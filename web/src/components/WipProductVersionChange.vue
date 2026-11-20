<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()

const products = [
  { no: 'DEV-IC1', version: 'C', previous: ['A', 'B'] },
  { no: 'DEV-IC2', version: 'B', previous: ['A'] },
]
const lots = ref([
  { no: 'DEV-VER001', product: 'DEV-IC1', version: 'A', mo: 'DEV-MO1', ro: 'DEV-RO1', item: '1', customer: 'DEV-C01' },
  { no: 'DEV-VER002', product: 'DEV-IC1', version: 'B', mo: 'DEV-MO1', ro: 'DEV-RO1', item: '2', customer: 'DEV-C01', packaged: true },
  { no: 'DEV-VER003', product: 'DEV-IC1', version: 'C', mo: 'DEV-MO2', ro: 'DEV-RO2', item: '1', customer: 'DEV-C02' },
  { no: 'DEV-VER004', product: 'DEV-IC2', version: 'A', mo: 'DEV-MO3', ro: 'DEV-RO3', item: '1', customer: 'DEV-C02' },
])
const productNo = ref('')
const oldVersion = ref('')
const product = computed(() => products.find(row => row.no === productNo.value))
const query = ref<{ no: string; version: string }>()
const visible = computed(() => lots.value.filter(row => row.product === query.value?.no &&
  (!query.value.version || row.version === query.value.version)))
const selected = ref<string[]>([])
const confirmation = ref<string | null>(null)
const error = ref('')
const result = ref('')

function resetQuery(): void {
  query.value = undefined
  selected.value = []
  error.value = result.value = ''
}

function search(): void {
  resetQuery()
  if (!product.value) {
    error.value = '請選擇產品。'
    return
  }
  query.value = { no: product.value.no, version: oldVersion.value }
}

function requestChange(): void {
  const target = product.value
  if (!target || !selected.value.length) return
  error.value = result.value = ''
  confirmation.value = `將選取的 ${selected.value.length} 個基礎批變更至 ${target.no} 目前版本 ${target.version}？`
}

function resolve(accepted: boolean): void {
  confirmation.value = null
  const target = product.value
  if (!accepted || !target) return
  const selectedLots = visible.value.filter(row => selected.value.includes(row.no))
  const packaged = selectedLots.find(row => row.packaged)
  if (packaged) {
    error.value = `基礎批 ${packaged.no} 已包裝，本次未變更。`
    return
  }
  const changes = selectedLots.filter(row => row.version !== target.version)
  for (const row of changes) row.version = target.version
  selected.value = []
  query.value = { no: target.no, version: '' }
  oldVersion.value = ''
  result.value = `DEV 已變更 ${changes.length} 個基礎批，${selectedLots.length - changes.length} 個已是目前版本。`
}
</script>

<template>
  <section class="version-change" aria-labelledby="version-change-title">
    <h2 id="version-change-title">產品版本變更</h2>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="search">
        <label>產品
          <select v-model="productNo" @change="oldVersion = ''; resetQuery()">
            <option value="">請選擇</option><option v-for="row in products" :key="row.no">{{ row.no }}</option>
          </select>
        </label>
        <label>目前版本 <input :value="product?.version ?? ''" readonly></label>
        <label>舊版本
          <select v-model="oldVersion" :disabled="!product" @change="resetQuery">
            <option value="">全部</option><option v-for="version in product?.previous" :key="version">{{ version }}</option>
          </select>
        </label>
        <button type="submit">查詢</button>
      </form>
      <div class="toolbar">
        <button type="button" :disabled="!visible.length" @click="selected = visible.map(row => row.no)">全選</button>
        <button type="button" :disabled="!selected.length" @click="selected = []">取消全選</button>
        <button type="button" :disabled="!selected.length" @click="requestChange">確認變更</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </div>
      <div class="grid">
        <table aria-label="產品基礎批">
          <thead><tr><th scope="col">選取</th><th scope="col">基礎批號</th><th scope="col">產品</th><th scope="col">版本</th><th scope="col">訂單</th><th scope="col">項次</th><th scope="col">工單</th><th scope="col">客戶</th></tr></thead>
          <tbody>
            <tr v-for="row in visible" :key="row.no" :class="{ selected: selected.includes(row.no) }">
              <td><input v-model="selected" type="checkbox" :value="row.no" :aria-label="'選取 ' + row.no"></td>
              <td>{{ row.no }}</td><td>{{ row.product }}</td><td>{{ row.version }}</td><td>{{ row.ro }}</td><td>{{ row.item }}</td><td>{{ row.mo }}</td><td>{{ row.customer }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="8">{{ query ? '查無資料。' : '請先查詢產品基礎批。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p>產品版本變更 API 待接續。</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.version-change { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p { margin: 0; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
.selected td { background: #fff6c5; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
