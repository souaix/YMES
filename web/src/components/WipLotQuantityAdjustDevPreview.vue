<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
type Mode = 'lot' | 'bin' | 'component'
type Count = number | string
interface Bucket { id: string; qty: Count; good: boolean }
interface Part { id: string; goodQty: Count; scrapQty: Count; state: 'normal' | 'bonus' | 'deducted'; pass: boolean }
interface Lot {
  no: string; area: string; segment: string; op: string; input: number; good: number; stamp: number;
  bins: Bucket[]; parts: Part[]; scrap: number; loss: number; errorQty: number; packaged?: boolean;
}
const lots = ref<Lot[]>(dev.value ? [
  { no: 'DEV-QTY001', area: 'A01', segment: 'ASSY', op: 'WB', input: 100, good: 98, stamp: 1, bins: [], parts: [], scrap: 1, loss: 1, errorQty: 1 },
  { no: 'DEV-QTY002', area: 'A02', segment: 'TEST', op: 'FT', input: 100, good: 95, stamp: 1, bins: [{ id: '01', qty: 95, good: true }, { id: '02', qty: 5, good: false }], parts: [], scrap: 0, loss: 0, errorQty: 0 },
  { no: 'DEV-QTY003', area: 'A01', segment: 'ASSY', op: 'WB', input: 2, good: 2, stamp: 1, bins: [], parts: [{ id: 'DEV-C01', goodQty: 1, scrapQty: 0, state: 'normal', pass: true }, { id: 'DEV-C02', goodQty: 1, scrapQty: 0, state: 'normal', pass: true }], scrap: 0, loss: 0, errorQty: 0 },
  { no: 'DEV-QTY004', area: 'A02', segment: 'TEST', op: 'FT', input: 70, good: 70, stamp: 1, bins: [], parts: [], scrap: 0, loss: 0, errorQty: 0, packaged: true },
] : [])
const filters = [{ key: 'area', label: '作業區' }, { key: 'segment', label: '生產區段' }, { key: 'op', label: '作業站' }, { key: 'no', label: '批號' }] as const
const blank = { area: '', segment: '', op: '', no: '' }
const filter = reactive({ ...blank })
const criteria = ref({ ...blank })
const queried = ref(false)
const visible = computed(() => queried.value ? lots.value.filter(lot => filters.every(({ key }) => !criteria.value[key] || lot[key] === criteria.value[key])) : [])
const selected = ref('')
const active = computed(() => visible.value.find(lot => lot.no === selected.value))
const modeNames = { lot: '整批', bin: '分 Bin', component: '元件' }
function mode(lot: Lot): Mode {
  if (lot.bins.length) return 'bin'
  return lot.parts.length ? 'component' : 'lot'
}

const draft = ref<Lot>()
const original = ref<Lot>()
const variant = computed(() => draft.value && mode(original.value ?? draft.value))
const reasons = ref<string[]>([])
const note = ref('')
const inventory = ref('')
const failInventory = ref('')
const addPart = ref('')
const bonus = ref<Count>(0)
const deduction = ref<Count>(0)
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)
const failedBins = computed(() => draft.value?.bins.filter(bin => !bin.good).reduce((sum, bin) => sum + Number(bin.qty), 0) ?? 0)

function query(): void {
  for (const { key } of filters) criteria.value[key] = filter[key].trim().toUpperCase()
  queried.value = true
  selected.value = ''
  error.value = result.value = ''
}
function clear(): void {
  Object.assign(filter, blank)
  query()
}
function edit(): void {
  const lot = active.value
  error.value = result.value = ''
  if (!lot || lot.packaged) {
    error.value = '已包裝生產批不可調整。'
    return
  }
  const snapshot = () => ({ ...lot, bins: lot.bins.map(row => ({ ...row })), parts: lot.parts.map(row => ({ ...row })) })
  draft.value = snapshot()
  original.value = snapshot()
  reasons.value = []
  note.value = inventory.value = failInventory.value = addPart.value = ''
  bonus.value = deduction.value = 0
}
function addComponent(): void {
  const lot = draft.value
  const id = addPart.value.trim().toUpperCase()
  if (!lot || !id || lot.parts.some(row => row.id === id)) {
    error.value = '請輸入未使用的元件編號。'
    return
  }
  lot.parts.push({ id, goodQty: 1, scrapQty: 0, state: 'bonus', pass: true })
  addPart.value = error.value = ''
}
function toggleDeduction(part: Part): void {
  if (part.state === 'bonus') {
    if (draft.value) draft.value.parts = draft.value.parts.filter(row => row.id !== part.id)
  } else part.state = part.state === 'deducted' ? 'normal' : 'deducted'
}
function closeDraft(): void {
  draft.value = undefined
  error.value = ''
}
function valid(value: Count, whole = false): boolean {
  const n = Number(value)
  return value !== '' && Number.isFinite(n) && n >= 0 && (!whole || Number.isInteger(n))
}

function requestSave(): void {
  const lot = draft.value
  if (!lot || !original.value) return
  let mismatch = false
  error.value = ''
  if (!reasons.value.length) error.value = '請至少選一項調整原因。'
  else if (lot.stamp !== active.value?.stamp) error.value = '資料版本已變更，請重新查詢。'
  else if (!valid(lot.scrap) || !valid(lot.loss)) error.value = '報廢及損失數量須為非負數。'
  else if (variant.value === 'lot') {
    if (!valid(lot.good) || lot.good <= 0) error.value = '良品量須大於零。'
    else if (!valid(lot.errorQty) || Number(lot.errorQty) !== Number(lot.scrap)) error.value = '不良原因數量必須等於本站報廢量。'
    else if (lot.good === original.value.good && lot.scrap === original.value.scrap && lot.loss === original.value.loss) error.value = '數量沒有變更。'
    else mismatch = Math.abs(Number(lot.good) + Number(lot.scrap) + Number(lot.loss) - lot.input) > 0.0000001
  } else if (variant.value === 'bin') {
    const values = [bonus.value, deduction.value, lot.scrap, lot.loss, ...lot.bins.map(bin => bin.qty)]
    if (!values.every(value => valid(value, true))) error.value = 'Bin 數量、增扣與報廢損失都須為非負整數。'
    else if (Number(bonus.value) > 0 && Number(deduction.value) > 0) error.value = '增量及扣量至少一項為零。'
    else if (lot.bins.reduce((sum, bin) => sum + Number(bin.qty), 0) + Number(lot.scrap) + Number(lot.loss) !== lot.input + Number(bonus.value) - Number(deduction.value)) error.value = 'Bin、報廢及損失合計須等於投入量加增量減扣量。'
    else if (!lot.bins.some(bin => bin.good && Number(bin.qty) > 0)) error.value = '良品 Bin 合計須大於零。'
    else if (failedBins.value > 0 && !failInventory.value) error.value = '請選擇不良 Bin 庫房。'
  } else {
    const parts = lot.parts.filter(part => part.state !== 'deducted')
    if (!parts.length || !parts.some(part => part.pass)) error.value = '須保留至少一件合格元件。'
    else if (parts.some(part => !valid(part.goodQty, true) || !valid(part.scrapQty, true))) error.value = '元件數量須為非負整數。'
    else if (parts.some(part => !part.pass) && !inventory.value) error.value = '請選擇報廢庫房。'
  }
  if (!error.value && variant.value !== 'component' && (Number(lot.scrap) > 0 || Number(lot.loss) > 0) && !inventory.value) error.value = '請選擇報廢庫房。'
  if (!error.value) confirmation.value = mismatch ? '良品、報廢與損失合計不同於投入量，仍要保存 DEV 調整？' : `確認保存 ${lot.no} 的 DEV 數量調整？`
}

function resolve(accepted: boolean): void {
  confirmation.value = null
  const lot = draft.value
  const current = active.value
  if (!accepted || !lot || !current || lot.stamp !== current.stamp) return
  let good = Number(lot.good)
  if (variant.value === 'bin') good = lot.bins.reduce((sum, bin) => sum + (bin.good ? Number(bin.qty) : 0), 0)
  if (variant.value === 'component') {
    good = lot.parts.filter(part => part.state !== 'deducted' && part.pass).length
    lot.parts = lot.parts.filter(part => part.state !== 'deducted' && part.pass).map(part => ({ ...part, state: 'normal' }))
  }
  Object.assign(current, { ...lot, good, scrap: Number(lot.scrap), loss: Number(lot.loss), errorQty: Number(lot.errorQty), stamp: current.stamp + 1 })
  result.value = `DEV 已調整 ${lot.no}，目前良品量 ${good}。`
  closeDraft()
}
</script>

<template>
  <section class="quantity-page" aria-label="生產批數量調整">
    <template v-if="dev">
      <form v-if="!draft" class="toolbar" @submit.prevent="query">
        <label v-for="field in filters" :key="field.key">{{ field.label }}
          <input v-model="filter[field.key]" :list="'qty-' + field.key" maxlength="50">
          <datalist :id="'qty-' + field.key"><option v-for="value in new Set(lots.map(lot => lot[field.key]))" :key="value" :value="value" /></datalist>
        </label>
        <button type="submit" class="query-button">查詢</button>
        <button type="button" @click="clear">清除</button>
        <button type="button" :disabled="!active" @click="edit">調整</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div v-if="!draft" class="grid">
        <table aria-label="可調整生產批">
          <thead><tr><th>選取</th><th>批號</th><th>區域</th><th>生產區段</th><th>作業站</th><th>良品量</th><th>調整類型</th></tr></thead>
          <tbody>
            <tr v-for="lot in visible" :key="lot.no" :class="{ selected: selected === lot.no }">
              <td><input v-model="selected" type="radio" name="qty-lot" :value="lot.no" :aria-label="'選取 ' + lot.no"></td>
              <td>{{ lot.no }}</td><td>{{ lot.area }}</td><td>{{ lot.segment }}</td><td>{{ lot.op }}</td><td>{{ lot.good }}</td><td>{{ modeNames[mode(lot)] }}</td>
            </tr>
            <tr v-if="!visible.length"><td colspan="7">{{ queried ? '查無資料。' : '請先查詢。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <form v-else class="editor" @submit.prevent="requestSave">
        <h2>{{ draft.no }} · {{ variant && modeNames[variant] }}調整</h2>
        <p>投入量 {{ draft.input }} · 目前良品量 {{ original?.good }} · 作業站 {{ draft.op }}</p>
        <div v-if="variant !== 'component'" class="toolbar">
          <label v-if="variant === 'lot'">調整後良品量 <input v-model.number="draft.good" type="number" min="0" step="0.0001"></label>
          <label v-if="variant === 'bin'">增量 <input v-model.number="bonus" type="number" min="0"></label>
          <label v-if="variant === 'bin'">扣量 <input v-model.number="deduction" type="number" min="0"></label>
          <label>報廢量 <input v-model.number="draft.scrap" type="number" min="0" :step="variant === 'bin' ? 1 : 0.0001"></label>
          <label>損失量 <input v-model.number="draft.loss" type="number" min="0" :step="variant === 'bin' ? 1 : 0.0001"></label>
          <label v-if="variant === 'lot'">不良原因數量 <input v-model.number="draft.errorQty" type="number" min="0" step="0.0001"></label>
        </div>
        <div v-if="variant === 'bin'" class="grid">
          <table aria-label="Bin 分佈">
            <thead><tr><th>Bin</th><th>良品</th><th>數量</th></tr></thead>
            <tbody><tr v-for="bin in draft.bins" :key="bin.id"><td>{{ bin.id }}</td><td><input v-model="bin.good" type="checkbox" :aria-label="'良品 Bin ' + bin.id"></td><td><input v-model.number="bin.qty" type="number" min="0" :aria-label="'Bin 數量 ' + bin.id"></td></tr></tbody>
          </table>
        </div>
        <template v-if="variant === 'component'">
          <div class="toolbar"><label>元件編號 <input v-model="addPart" maxlength="50"></label><button type="button" @click="addComponent">增加元件</button></div>
          <div class="grid">
            <table aria-label="批內元件">
              <thead><tr><th>元件</th><th>合格</th><th>良品量</th><th>報廢量</th><th>動作</th></tr></thead>
              <tbody><tr v-for="part in draft.parts" :key="part.id">
                <td>{{ part.id }}</td><td><input v-model="part.pass" :disabled="part.state === 'deducted'" type="checkbox" :aria-label="'合格 ' + part.id"></td>
                <td><input v-model.number="part.goodQty" :disabled="part.state === 'deducted'" type="number" min="0" :aria-label="'良品量 ' + part.id"></td>
                <td><input v-model.number="part.scrapQty" :disabled="part.state === 'deducted'" type="number" min="0" :aria-label="'報廢量 ' + part.id"></td>
                <td><button type="button" @click="toggleDeduction(part)">{{ part.state === 'deducted' ? '復原扣除' : '扣除' }}</button></td>
              </tr></tbody>
            </table>
          </div>
        </template>
        <div class="toolbar">
          <label>報廢庫房 <select v-model="inventory"><option value="">請選擇</option><option>DEV-SCRAP</option></select></label>
          <label v-if="variant === 'bin'">不良 Bin 庫房 <select v-model="failInventory"><option value="">請選擇</option><option>DEV-FAIL</option></select></label>
        </div>
        <fieldset class="toolbar"><legend>調整原因</legend><label v-for="reason in ['盤點差異', '報工修正', '測試重分類']" :key="reason"><input v-model="reasons" type="checkbox" :value="reason"> {{ reason }}</label></fieldset>
        <label>說明 <input v-model="note" maxlength="255"></label>
        <div class="toolbar"><button type="submit" class="primary">確認調整</button><button type="button" @click="closeDraft">取消</button></div>
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
.quantity-page, .editor { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.editor { padding: 14px; border: 1px solid var(--line); background: white; }
h2, p { margin: 0; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
input[type='number'] { width: 90px; }
button { width: max-content; min-height: 30px; }
[role='alert'] { color: #9d2727; }
</style>
