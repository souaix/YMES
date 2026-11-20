<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const lots = ref(dev.value ? [
  { no: 'DEV-CHG001', mo: 'DEV-MO01', segment: 'ASSY', op: 'WB', product: 'DEV-P1', version: 'A', unit: 'PCS', status: 0 },
  { no: 'DEV-CHG002', mo: 'DEV-MO01', segment: 'ASSY', op: 'PK', product: 'DEV-P1', version: 'A', unit: 'PCS', status: 0 },
  { no: 'DEV-CHG003', mo: 'DEV-MO02', segment: 'TEST', op: 'FT', product: 'DEV-P2', version: 'A', unit: 'PCS', status: 0, packaged: true },
  { no: 'DEV-CHG004', mo: 'DEV-MO02', segment: 'TEST', op: 'FT', product: 'DEV-P2', version: 'A', unit: 'PCS', status: 4 },
] : [])
const products = [
  { no: 'DEV-P1', version: 'A', unit: 'PCS', compatibleOps: ['WB', 'PK'] },
  { no: 'DEV-P2', version: 'A', unit: 'PCS', compatibleOps: ['FT'] },
  { no: 'DEV-P3', version: 'B', unit: 'PCS', compatibleOps: ['WB', 'FT'] },
]
const filters = [{ key: 'mo', name: '工單' }, { key: 'no', name: '生產批號' }, { key: 'segment', name: '生產區段' }, { key: 'product', name: '來源產品' }] as const
const filter = reactive({ mo: '', no: '', segment: '', product: '' })
const criteria = ref({ ...filter })
const queried = ref(false)
const detailMode = ref(true)
const selected = ref<string[]>([])
const target = ref('')
const destination = computed(() => products.find(item => item.no === target.value))
const visible = computed(() => queried.value ? lots.value.filter(lot => (!detailMode.value || lot.status === 0) && filters.every(({ key }) => !criteria.value[key] || lot[key] === criteria.value[key])) : [])
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)
function search(): void {
  for (const { key } of filters) criteria.value[key] = filter[key].trim().toUpperCase()
  queried.value = true
  selected.value = []
  error.value = result.value = ''
}
function request(): void {
  error.value = result.value = ''
  const chosen = visible.value.filter(lot => selected.value.includes(lot.no))
  if (!destination.value || !chosen.length) {
    error.value = '請勾選生產批並選擇聯產品。'
    return
  }
  const packaged = chosen.find(lot => lot.packaged)
  if (packaged) {
    error.value = `${packaged.no} 已包裝，本次未執行變更。`
    return
  }
  confirmation.value = `將選取的 ${chosen.length} 批變更至 ${destination.value.no}／${destination.value.version}？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  const product = destination.value
  if (!accepted || !product) return
  const chosen = visible.value.filter(lot => selected.value.includes(lot.no))
  let changed = 0
  for (const [i, lot] of chosen.entries()) {
    if (lot.product === product.no) continue
    if (detailMode.value && !product.compatibleOps.includes(lot.op)) {
      error.value = `${lot.no} 的作業站 ${lot.op} 不在新產品流程。後續 ${chosen.length - i - 1} 批未執行。`
      break
    }
    lot.product = product.no
    lot.version = product.version
    lot.unit = product.unit
    changed++
  }
  selected.value = []
  result.value = `DEV 已變更 ${changed} 批。`
}
</script>

<template>
  <section class="product-change-page" aria-label="生產批產品變更作業">
    <template v-if="dev">
      <form class="toolbar" @submit.prevent="search">
        <label v-for="field in filters" :key="field.key">{{ field.name }}
          <input v-model="filter[field.key]" :list="'product-change-' + field.key" maxlength="50">
          <datalist :id="'product-change-' + field.key"><option v-for="item in new Set(lots.map(lot => lot[field.key]))" :key="item" :value="item" /></datalist>
        </label>
        <label><input v-model="detailMode" type="checkbox" @change="search"> DEV 明細模式</label>
        <button type="submit" class="query-button">查詢</button>
      </form>
      <div class="toolbar">
        <label>聯產品 <select v-model="target"><option value="">請選擇</option><option v-for="item in products" :key="item.no">{{ item.no }}</option></select></label>
        <label>版本 <input :value="destination?.version ?? ''" readonly></label>
        <label>單位 <input :value="destination?.unit ?? ''" readonly></label>
        <button type="button" class="primary" :disabled="!selected.length" @click="request">確認變更</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </div>
      <div class="grid"><table aria-label="可變更產品的生產批">
        <thead><tr><th>選取</th><th>批號</th><th>工單</th><th>區段</th><th>站別</th><th>產品／版本</th><th>單位</th><th>狀態</th></tr></thead>
        <tbody>
          <tr v-for="lot in visible" :key="lot.no" :class="{ selected: selected.includes(lot.no) }">
            <td><input v-model="selected" type="checkbox" :value="lot.no" :aria-label="'選取 ' + lot.no"></td>
            <td>{{ lot.no }}</td><td>{{ lot.mo }}</td><td>{{ lot.segment }}</td><td>{{ lot.op }}</td><td>{{ lot.product }}／{{ lot.version }}</td><td>{{ lot.unit }}</td><td>{{ lot.status === 0 ? '排隊' : '完成' }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="8">{{ queried ? '查無資料。' : '請先查詢。' }}</td></tr>
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
.product-change-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
p { margin: 0; }
[role='alert'] { color: #9d2727; }
</style>
