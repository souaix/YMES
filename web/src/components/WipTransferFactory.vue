<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const factories = ['F1', 'F2']
const lots = ref(dev.value ? [
  { no: 'DEV-TF001', factory: 'F1', area: 'ASSY-1', op: 'WB', product: 'DEV-IC', version: 'A', box: 'DEV-BOX1', qty: 100, areaByFactory: { F1: 'ASSY-1', F2: 'ASSY-2' } },
  { no: 'DEV-TF002', factory: 'F1', area: 'ASSY-1', op: 'WB', product: 'DEV-IC', version: 'A', box: 'DEV-BOX1', qty: 80, areaByFactory: { F1: 'ASSY-1', F2: 'ASSY-2' } },
  { no: 'DEV-TF003', factory: 'F1', area: 'TEST-1', op: 'FT', product: 'DEV-PKG', version: 'B', box: '', qty: 70, areaByFactory: { F1: 'TEST-1' } },
  { no: 'DEV-TF004', factory: 'F2', area: 'TEST-2', op: 'FT', product: 'DEV-IC', version: 'A', box: '', qty: 120, areaByFactory: { F1: 'TEST-1', F2: 'TEST-2' } },
] : [])
const boxFactories = reactive<Record<string, string>>({ 'DEV-BOX1': 'F1' })
const fields = [{ key: 'factory', label: '原廠別' }, { key: 'area', label: '作業區' }, { key: 'op', label: '作業站' }] as const
const empty = { factory: '', area: '', op: '' }
const filter = reactive({ ...empty })
const criteria = ref({ ...empty })
const queried = ref(false)
const visible = computed(() => queried.value ? lots.value.filter(lot => fields.every(({ key }) => !criteria.value[key] || criteria.value[key] === lot[key])) : [])
const destination = ref('')
const selected = ref<string[]>([])
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)

function query(): void {
  criteria.value = { ...filter }
  selected.value = []
  queried.value = true
  error.value = result.value = ''
}
function clear(): void {
  Object.assign(filter, empty)
  destination.value = ''
  criteria.value = { ...empty }
  selected.value = []
  queried.value = false
  error.value = result.value = ''
}
function plan() {
  const chosen = lots.value.filter(lot => selected.value.includes(lot.no))
  error.value = ''
  if (!destination.value || !chosen.length) {
    error.value = '請選擇生產批和目的廠別。'
    return []
  }
  const changing = chosen.filter(lot => lot.factory !== destination.value)
  if (!changing.length) {
    error.value = '所選批次已在目的廠。'
    return []
  }
  for (const lot of changing) {
    const other = lot.box && lots.value.find(row => row.box === lot.box && !selected.value.includes(row.no))
    if (other) {
      error.value = `靜電箱 ${lot.box} 仍有未勾選的批號 ${other.no}，請整組換廠。`
      return []
    }
    if (!lot.areaByFactory[destination.value as 'F1' | 'F2']) {
      error.value = `${lot.no} 的產品／版本與作業站在目的廠沒有區域設定。`
      return []
    }
  }
  return changing
}
function requestTransfer(): void {
  result.value = ''
  const changing = plan()
  if (changing.length) confirmation.value = `將 ${changing.length} 個生產批轉至 ${destination.value}？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  if (!accepted) return
  const changing = plan()
  if (!changing.length) return
  for (const lot of changing) {
    const area = lot.areaByFactory[destination.value as 'F1' | 'F2']
    if (!area) return
    lot.factory = destination.value
    lot.area = area
    if (lot.box) boxFactories[lot.box] = destination.value
  }
  selected.value = []
  result.value = `DEV 已將 ${changing.length} 批轉至 ${destination.value}。`
}
</script>

<template>
  <section class="transfer-page" aria-label="換廠作業">
    <template v-if="dev">
      <form class="toolbar" @submit.prevent="query">
        <label v-for="field in fields" :key="field.key">{{ field.label }}
          <select v-model="filter[field.key]"><option value="">全部</option><option v-for="value in new Set(lots.map(lot => lot[field.key]))" :key="value">{{ value }}</option></select>
        </label>
        <button type="submit" class="query-button">查詢</button>
        <button type="button" @click="clear">清除</button>
      </form>
      <div class="toolbar">
        <label>目的廠別 <select v-model="destination"><option value="">請選擇</option><option v-for="factory in factories" :key="factory">{{ factory }}</option></select></label>
        <button type="button" :disabled="!visible.length" @click="selected = visible.map(row => row.no)">全選</button>
        <button type="button" :disabled="!selected.length" @click="selected = []">取消全選</button>
        <button type="button" class="primary" :disabled="!selected.length" @click="requestTransfer">確認換廠</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </div>
      <div class="grid">
        <table aria-label="可換廠的生產批">
          <thead><tr><th>選取</th><th>批號</th><th>廠別</th><th>作業區</th><th>站別</th><th>產品／版本</th><th>數量</th><th>靜電箱</th></tr></thead>
          <tbody>
            <tr v-for="lot in visible" :key="lot.no" :class="{ selected: selected.includes(lot.no) }">
              <td><input v-model="selected" type="checkbox" :value="lot.no" :aria-label="'選取 ' + lot.no"></td>
              <td>{{ lot.no }}</td><td>{{ lot.factory }}</td><td>{{ lot.area }}</td><td>{{ lot.op }}</td><td>{{ lot.product }}／{{ lot.version }}</td><td>{{ lot.qty }}</td><td>{{ lot.box ? lot.box + '（' + boxFactories[lot.box] + '）' : '—' }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="8">{{ queried ? '查無資料。' : '請先查詢。' }}</td></tr>
          </tbody>
        </table>
      </div>
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
.transfer-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
p { margin: 0; }
button { width: max-content; min-height: 30px; }
[role='alert'] { color: #9d2727; }
</style>
