<script setup lang="ts">
import { computed, ref } from 'vue'
import { rowsKey, seedRows } from './productBasisDevelopment'

type Source = 'LAST FULL TEST' | 'USER DEFINE'
type Rule = {
  customer: string; product: string; sequence: number; status: 'Actived' | 'NotUsed'
  map: Source; detail: Source; mapStations: string[]; detailStations: string[]
  mapAfterCo: boolean; mapMatch: boolean; goodQtyMatch: boolean; creator: string; date: string
}
type Snapshot = { rules: Rule[]; log: Rule[] }
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdp01-wafer-ship-report.v1'
const products = props.developmentPreview ? read(rowsKey, seedRows) : []
const customers = [...new Set(['DEV-CUSTOMER', 'DEV-CUSTOMER-02', ...products.map(row => String(row.customerNo || '')).filter(Boolean)])]
const stationCatalog = ['DEV-CP-10', 'DEV-CP-20', 'DEV-CP-30']
const sources: Array<{ code: Source; label: string }> = [{ code: 'LAST FULL TEST', label: '最後一次完整測試' }, { code: 'USER DEFINE', label: '自訂站點' }]
const snapshot = ref<Snapshot>(props.developmentPreview ? read(storageKey, {
  rules: [{ ...blank(), customer: 'DEV-CUSTOMER', product: '*', sequence: 1, creator: 'DEV001' }], log: [],
}) : { rules: [], log: [] })
const query = ref({ customer: '', product: '', showUnused: false })
const filters = ref({ ...query.value })
const selection = ref('')
const mode = ref<'' | 'add' | 'edit'>('')
const draft = ref<Rule>(blank())
const error = ref('')
const notice = ref('')
const visible = computed(() => snapshot.value.rules.filter(row =>
  (filters.value.showUnused || row.status === 'Actived')
  && (!filters.value.customer || row.customer === filters.value.customer)
  && (!filters.value.product || row.product === filters.value.product),
))
const active = computed(() => visible.value.find(row => key(row) === selection.value) ?? visible.value[0])
const productChoices = computed(() => draft.value.customer === '*' ? [] : [...new Set(products
  .filter(row => row.customerNo === draft.value.customer)
  .map(row => String(row.productNo)))])

function key(row: Rule): string { return JSON.stringify([row.customer, row.product]) }

function read<T>(name: string, fallback: T): T {
  try { return JSON.parse(localStorage.getItem(name) ?? 'null') ?? fallback }
  catch { return fallback }
}

function blank(): Rule {
  return { customer: '', product: '*', sequence: 0, status: 'Actived', map: 'LAST FULL TEST', detail: 'LAST FULL TEST',
    mapStations: [], detailStations: [], mapAfterCo: false, mapMatch: false, goodQtyMatch: false, creator: '', date: '' }
}

function open(next: 'add' | 'edit'): void {
  const row = active.value
  if (next === 'edit' && !row) return
  draft.value = next === 'edit' && row ? { ...row, mapStations: [...row.mapStations], detailStations: [...row.detailStations] } : blank()
  error.value = ''
  mode.value = next
}

function persist(saved: Rule): boolean {
  if (!props.developmentPreview) return false
  const next = { rules: [...snapshot.value.rules.filter(row => key(row) !== key(saved)), saved], log: [...snapshot.value.log, saved] }
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 出貨報告設定。'; return false }
  snapshot.value = next
  selection.value = key(saved)
  error.value = ''
  return true
}

function save(): void {
  const row = draft.value
  const existing = snapshot.value.rules.find(item => key(item) === key(row))
  if (mode.value === 'add' && ![...customers, '*'].includes(row.customer)) { error.value = '請選客戶。'; return }
  if (mode.value === 'add' && row.product !== '*' && !productChoices.value.includes(row.product)) { error.value = '請選此客戶的產品，或使用 *。'; return }
  if (mode.value === 'add' && existing) { error.value = '此客戶與產品已有設定。請查詢既有設定。'; return }
  if (mode.value === 'edit' && !existing) return
  if (row.map === 'USER DEFINE' && !row.mapStations.length) { error.value = '請選至少一個出貨 MAP 站點。'; return }
  if (row.detail === 'USER DEFINE' && !row.detailStations.length) { error.value = '請選至少一個出貨明細站點。'; return }
  const saved: Rule = { ...row, status: 'Actived', sequence: (existing?.sequence ?? 0) + 1, creator: props.currentUserNo || 'DEV001', date: new Date().toISOString(),
    mapStations: row.map === 'USER DEFINE' ? [...row.mapStations] : [], detailStations: row.detail === 'USER DEFINE' ? [...row.detailStations] : [] }
  if (persist(saved)) {
    query.value = { customer: '', product: '', showUnused: false }
    filters.value = { ...query.value }
    mode.value = ''
    notice.value = '已儲存出貨報告設定。'
  }
}

function deactivate(): void {
  const row = active.value
  if (!row || row.status !== 'Actived' || !window.confirm('確定停用選取的出貨報告設定？')) return
  if (persist({ ...row, status: 'NotUsed', sequence: row.sequence + 1, creator: props.currentUserNo || 'DEV001', date: new Date().toISOString() })) {
    notice.value = '已停用出貨報告設定。'
  }
}
</script>

<template>
  <main class="cp-shipping">
    <header><h1>晶圓良率與出貨報告維護</h1><button @click="emit('close')">關閉</button></header>
    <form class="toolbar" @submit.prevent="filters = { customer: query.customer.trim().toUpperCase(), product: query.product.trim().toUpperCase(), showUnused: query.showUnused }">
      <label>客戶 <input v-model="query.customer" /></label>
      <label>產品 <input v-model="query.product" /></label>
      <label><input v-model="query.showUnused" type="checkbox" />含停用</label>
      <button>查詢</button>
      <button type="button" :disabled="!developmentPreview" @click="open('add')">新增</button>
      <button type="button" :disabled="!active" @click="open('edit')">修改</button>
      <button type="button" :disabled="active?.status !== 'Actived'" @click="deactivate">停用</button>
    </form>
    <p v-if="notice" role="status">{{ notice }}</p>
    <p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid">
      <table>
        <thead><tr><th>客戶</th><th>產品</th><th>出貨 MAP</th><th>出貨明細</th><th>序號</th><th>狀態</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="key(row)" :class="{ selected: row === active }">
            <td><button :aria-pressed="row === active" @click="selection = key(row)">{{ row.customer }}</button></td>
            <td>{{ row.product }}</td><td>{{ row.map }}</td><td>{{ row.detail }}</td><td>{{ row.sequence }}</td><td>{{ row.status === 'Actived' ? '啟用' : '停用' }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="6">無出貨報告設定</td></tr>
        </tbody>
      </table>
    </div>
    <dl v-if="active" class="summary">
      <dt>MAP 來源站點</dt><dd>{{ active.mapStations.join('、') || '最後一次完整測試' }}</dd>
      <dt>明細來源站點</dt><dd>{{ active.detailStations.join('、') || '最後一次完整測試' }}</dd>
      <dt>出貨檢查</dt><dd>{{ [active.mapAfterCo && '出貨後 MAP', active.mapMatch && 'MAP 一致', active.goodQtyMatch && '良品數一致'].filter(Boolean).join('、') || '無' }}</dd>
    </dl>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-label="出貨報告條件" @submit.prevent="save">
        <header><h2>出貨報告條件</h2><button type="button" @click="mode = ''; error = ''">關閉</button></header>
        <label>客戶
          <input v-if="mode === 'edit'" :value="draft.customer" readonly />
          <select v-else v-model="draft.customer" required @change="draft.product = '*'"><option value="">請選擇</option><option value="*">*</option><option v-for="item in customers" :key="item">{{ item }}</option></select>
        </label>
        <label>產品
          <input v-if="mode === 'edit'" :value="draft.product" readonly />
          <select v-else v-model="draft.product" required><option value="*">*</option><option v-for="item in productChoices" :key="item">{{ item }}</option></select>
        </label>
        <fieldset>
          <legend>出貨 MAP</legend>
          <select v-model="draft.map" aria-label="出貨 MAP 來源"><option v-for="source in sources" :key="source.code" :value="source.code">{{ source.label }}</option></select>
          <div v-if="draft.map === 'USER DEFINE'" class="checks"><label v-for="station in stationCatalog" :key="station"><input v-model="draft.mapStations" type="checkbox" :value="station" />{{ station }}</label></div>
        </fieldset>
        <fieldset>
          <legend>出貨明細</legend>
          <select v-model="draft.detail" aria-label="出貨明細來源"><option v-for="source in sources" :key="source.code" :value="source.code">{{ source.label }}</option></select>
          <div v-if="draft.detail === 'USER DEFINE'" class="checks"><label v-for="station in stationCatalog" :key="station"><input v-model="draft.detailStations" type="checkbox" :value="station" />{{ station }}</label></div>
        </fieldset>
        <fieldset>
          <legend>出貨檢查</legend>
          <div class="checks"><label><input v-model="draft.mapAfterCo" type="checkbox" />出貨後 MAP</label><label><input v-model="draft.mapMatch" type="checkbox" />MAP 一致</label><label><input v-model="draft.goodQtyMatch" type="checkbox" />良品數一致</label></div>
        </fieldset>
        <p v-if="error" role="alert">{{ error }}</p>
        <footer><button>確定</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.cp-shipping { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer, .checks { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.summary { display: grid; grid-template-columns: max-content 1fr; gap: 6px 14px; margin: 6px 0; }
dd { margin: 0; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 12px; width: min(560px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor > label { display: flex; align-items: center; gap: 8px; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
