<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, seedRows, type ProductRow } from './productBasisDevelopment'
import { loadDevelopmentRunCardMappings, loadDevelopmentRunCards, runCardMapStorageKey, type RunCardMapping } from './runCardDevelopment'

const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const mappings = ref(props.developmentPreview ? loadDevelopmentRunCardMappings() : [])
const products = new Map<string, ProductRow>()
const mainCards = props.developmentPreview ? loadDevelopmentRunCards().filter(card => card.main) : []

if (props.developmentPreview) {
  let source = seedRows
  try {
    const stored = JSON.parse(localStorage.getItem(rowsKey) ?? 'null')
    if (Array.isArray(stored)) source = stored
  } catch { /* Use seeded products when DEV storage is unavailable. */ }
  for (const product of source) {
    if (Number(product.issueState) !== 2) continue
    if (!products.has(product.productNo) || Number(product.curVersion) === 1) products.set(product.productNo, product)
  }
}

const mode = ref<'' | 'add' | 'edit' | 'copy'>('')
const selectedProduct = ref('')
const productNo = ref('')
const runCardNo = ref('')
const search = ref('')
const filter = ref('')
const message = ref('')
const error = ref('')
const visible = computed(() => mappings.value.filter(row => row.productNo.includes(filter.value) || row.runCardNo.includes(filter.value)))
const active = computed(() => visible.value.find(row => row.productNo === selectedProduct.value) ?? visible.value[0])
const editorTitle = computed(() => ({ add: '新增', edit: '修改', copy: '複製', '': '' })[mode.value])

function open(next: 'add' | 'edit' | 'copy'): void {
  if (next !== 'add' && !active.value) return
  productNo.value = next === 'edit' ? active.value?.productNo ?? '' : ''
  runCardNo.value = next === 'add' ? '' : active.value?.runCardNo ?? ''
  error.value = ''
  message.value = ''
  mode.value = next
}

function persist(next: RunCardMapping[]): boolean {
  if (!props.developmentPreview) return false
  try {
    localStorage.setItem(runCardMapStorageKey, JSON.stringify(next))
  } catch {
    error.value = '無法儲存本機 DEV 對應。'
    return false
  }
  mappings.value = next
  error.value = ''
  return true
}

function save(close: boolean): void {
  const previous = mappings.value.find(row => row.productNo === productNo.value)
  if (!productNo.value || (mode.value !== 'edit' && productNo.value !== '*' && !products.has(productNo.value))) {
    error.value = '請選擇已核准產品或 *。'
    return
  }
  if (!mainCards.some(card => card.no === runCardNo.value)) {
    error.value = '請選擇主流程卡。'
    return
  }
  if (mode.value !== 'edit' && previous) {
    error.value = '這個產品已有流程卡對應。'
    return
  }
  if (mode.value === 'edit' && !previous) {
    error.value = '找不到要修改的產品對應。'
    return
  }
  const user = props.currentUserNo || 'DEV001'
  const date = new Date().toISOString()
  const saved = previous
    ? { ...previous, runCardNo: runCardNo.value, reviser: user, reviseDate: date, dataStamp: previous.dataStamp + 1 }
    : { productNo: productNo.value, runCardNo: runCardNo.value, creator: user, createDate: date, dataStamp: 0 }
  const next = previous ? mappings.value.map(row => row.productNo === saved.productNo ? saved : row) : [...mappings.value, saved]
  if (!persist(next)) return
  selectedProduct.value = saved.productNo
  search.value = filter.value = ''
  message.value = '已儲存產品流程卡對應。'
  mode.value = close ? '' : 'edit'
}

function remove(): void {
  const row = active.value
  if (row && window.confirm(`確定刪除 ${row.productNo} 的流程卡對應？`) && persist(mappings.value.filter(item => item.productNo !== row.productNo))) {
    message.value = '已刪除產品流程卡對應。'
  }
}
</script>

<template>
  <main class="run-card-map">
    <header><h1>產品流程卡對應設定</h1><button @click="emit('close')">關閉</button></header>
    <form class="toolbar" @submit.prevent="filter = search.trim().toUpperCase()">
      <label>產品或流程卡 <input v-model="search" /></label>
      <button>查詢</button>
      <button type="button" @click="search = filter = ''">清除</button>
      <button type="button" :disabled="!developmentPreview" @click="open('add')">新增</button>
      <button type="button" :disabled="!active" @click="open('edit')">修改</button>
      <button type="button" :disabled="!active" @click="open('copy')">複製</button>
      <button type="button" :disabled="!active" @click="remove">刪除</button>
    </form>
    <p v-if="message" role="status">{{ message }}</p>
    <p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid">
      <table>
        <thead><tr><th scope="col">產品編號</th><th scope="col">產品名稱</th><th scope="col">流程卡編號</th><th scope="col">建立人員</th><th scope="col">修改人員</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="row.productNo" :class="{ selected: active?.productNo === row.productNo }" @dblclick="selectedProduct = row.productNo; open('edit')">
            <td><button :aria-pressed="active?.productNo === row.productNo" @click="selectedProduct = row.productNo">{{ row.productNo }}</button></td>
            <td>{{ products.get(row.productNo)?.productName }}</td><td>{{ row.runCardNo }}</td><td>{{ row.creator }}</td><td>{{ row.reviser }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="5">無對應資料</td></tr>
        </tbody>
      </table>
    </div>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" :aria-label="editorTitle" @submit.prevent="save(true)">
        <h2>{{ editorTitle }}產品流程卡對應</h2>
        <label>產品編號
          <input v-if="mode === 'edit'" :value="productNo" readonly />
          <select v-else v-model="productNo" required>
            <option value="">請選擇</option><option value="*">*</option>
            <option v-for="[key, product] in products" :key="key" :value="key">{{ key }} {{ product.productName }}</option>
          </select>
        </label>
        <label>主流程卡
          <select v-model="runCardNo" required>
            <option value="">請選擇</option>
            <option v-if="runCardNo && !mainCards.some(card => card.no === runCardNo)" :value="runCardNo" disabled>{{ runCardNo }}（已停用或不存在）</option>
            <option v-for="card in mainCards" :key="card.no" :value="card.no">{{ card.no }}</option>
          </select>
        </label>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer><button>確定</button><button type="button" @click="save(false)">套用</button><button type="button" @click="mode = ''; error = ''">關閉</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.run-card-map { display: grid; align-content: start; gap: 8px; padding: 10px; min-width: 0; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
h2 { font-size: 14px; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(420px, 95vw); padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor label { display: grid; gap: 4px; }
.editor input, .editor select { min-width: 0; padding: 5px; border: 1px solid #aaa797; background: #fff5d6; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
