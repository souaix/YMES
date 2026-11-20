<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const lots = ref(dev.value ? [
  { no: 'DEV-CLOSE001', product: 'DEV-P1', mo: 'DEV-MO01', op: 'WB', area: 'ASSY', qty: 100, state: 0 },
  { no: 'DEV-CLOSE002', product: 'DEV-P1', mo: 'DEV-MO01', op: 'WB', area: 'ASSY', qty: 80, state: 0, packaged: true },
  { no: 'DEV-CLOSE003', product: 'DEV-P2', mo: 'DEV-MO02', op: 'FT', area: 'TEST', qty: 75, state: 0 },
] : [])
const fields = [{ key: 'product', label: '產品' }, { key: 'mo', label: '工單' }, { key: 'op', label: '作業站' }] as const
const filter = reactive({ product: '', mo: '', op: '' })
const enabled = reactive({ product: false, mo: false, op: false })
const criteria = ref<{ product: string; mo: string; op: string }>()
const visible = computed(() => {
  const query = criteria.value
  return query ? lots.value.filter(lot => lot.state === 0 && fields.every(({ key }) => !query[key] || query[key] === lot[key])) : []
})
const selected = ref('')
const active = computed(() => visible.value.find(lot => lot.no === selected.value))
const confirmation = ref<string | null>(null)
const result = ref('')
const error = ref('')
function search(): void {
  result.value = error.value = selected.value = ''
  criteria.value = undefined
  if (!fields.some(({ key }) => enabled[key]) || fields.some(({ key }) => enabled[key] && !filter[key].trim())) {
    error.value = '請啟用至少一項查詢條件，並填寫已啟用欄位。'
    return
  }
  criteria.value = { product: enabled.product ? filter.product.trim().toUpperCase() : '', mo: enabled.mo ? filter.mo.trim().toUpperCase() : '', op: enabled.op ? filter.op.trim().toUpperCase() : '' }
}
function requestClose(): void {
  if (!active.value) return
  error.value = result.value = ''
  confirmation.value = `確定強制結批 ${active.value.no}？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  const lot = active.value
  if (!accepted || !lot) return
  if (lot.packaged) {
    error.value = `${lot.no} 已包裝，無法強制結批。`
    return
  }
  lot.state = 301
  selected.value = ''
  result.value = `DEV 生產批 ${lot.no} 已強制結批（301）。`
}
</script>

<template>
  <section class="force-close-page" aria-label="生產批強制結批作業">
    <template v-if="dev">
      <form class="toolbar" @submit.prevent="search">
        <div v-for="field in fields" :key="field.key" class="toolbar">
          <label><input v-model="enabled[field.key]" type="checkbox"> {{ field.label }}</label>
          <input v-model="filter[field.key]" :disabled="!enabled[field.key]" :aria-label="field.label" :list="'close-' + field.key" maxlength="50">
          <datalist :id="'close-' + field.key"><option v-for="item in new Set(lots.map(lot => lot[field.key]))" :key="item" :value="item" /></datalist>
        </div>
        <button type="submit" class="query-button">查詢</button>
        <button type="button" class="primary" :disabled="!active" @click="requestClose">強制結批</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div class="grid"><table aria-label="排隊生產批">
        <thead><tr><th>選取</th><th>批號</th><th>產品</th><th>工單</th><th>作業站</th><th>作業區</th><th>數量</th><th>狀態</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="row.no" :class="{ selected: selected === row.no }">
            <td><input v-model="selected" type="radio" name="close-lot" :value="row.no" :aria-label="'選取 ' + row.no"></td>
            <td>{{ row.no }}</td><td>{{ row.product }}</td><td>{{ row.mo }}</td><td>{{ row.op }}</td><td>{{ row.area }}</td><td>{{ row.qty }}</td><td>排隊</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="8">{{ criteria ? '查無排隊生產批。' : '請先查詢。' }}</td></tr>
        </tbody>
      </table></div>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p><button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>
<style scoped>
.force-close-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
p { margin: 0; }
[role='alert'] { color: #9d2727; }
</style>
