<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const unavailableMessage = "正式環境尚未開放此功能。"
const emit = defineEmits<{ close: [] }>()

const defaultBasics = { no: '', op: dev.value ? 'WB' : '', source: 'op', product: dev.value ? 'DEV-IC/A' : '', start: '', end: '' }
const basics = reactive({ ...defaultBasics })
const operations = dev.value ? (['WB', 'FT']) : ([])
const products = dev.value ? (['DEV-IC/A', 'DEV-IC/B', 'DEV-PKG/A']) : ([])
const lots = dev.value ? ([
  { no: 'DEV-ECN001', customer: 'DEV-C1', mo: 'DEV-MO1', product: 'DEV-IC/A' },
  { no: 'DEV-ECN002', customer: 'DEV-C1', mo: 'DEV-MO2', product: 'DEV-IC/B' },
  { no: 'DEV-ECN003', customer: 'DEV-C2', mo: 'DEV-MO3', product: 'DEV-PKG/A' },
]) : ([])
const factorFields = [
  { key: 'customer', label: '客戶' }, { key: 'mo', label: '工單' }, { key: 'product', label: '產品／版本' },
] as const
const factors = reactive({ customer: '', mo: '', product: '' })
const enabled = reactive({ customer: false, mo: false, product: false })
const attrs = ref<{ no: string; initial: string; value: string; checked: boolean }[]>([])
const candidates = computed(() => dev.value ? (lots.filter(lot => factorFields.every(({ key }) => !enabled[key] || lot[key] === factors[key]))) : ([]))
const selectedLots = ref<string[]>([])
const step = ref(1)
const titles = ['歡迎', 'ECN 基本資料', '設定特性', '影響條件', '受影響生產批', '完成']
const error = ref('')
const confirmation = ref<string | null>(null)
const confirmAction = ref<'submit' | 'close'>('submit')
const recorded = new Set<string>()
const again = ref(false)

function loadAttrs(): void {
  if (!dev.value) return

  const numeric = basics.op === 'WB' ? 'BOND_FORCE' : 'TEST_LIMIT'
  const initial = basics.op === 'WB' ? '25' : '80'
  attrs.value = [
    { no: numeric, initial, value: initial, checked: false },
    { no: 'SETUP_PROGRAM', initial: basics.source === 'product' ? basics.product : 'STD-A', value: basics.source === 'product' ? basics.product : 'STD-A', checked: false },
  ]
}
watch(() => [basics.op, basics.source, basics.product], loadAttrs)
loadAttrs()

function next(): void {
  if (!dev.value) return

  error.value = ''
  if (step.value === 2) {
    if (!basics.no.trim() || !basics.op || (basics.source === 'product' && !basics.product)) {
      error.value = '請輸入 ECN 編號並完成工序／產品選擇。'
    } else if (basics.start && new Date(basics.start).getTime() < Date.now()) {
      error.value = '生效時間不得早於目前時間。'
    } else if (basics.start && basics.end && basics.end <= basics.start) {
      error.value = '失效時間必須晚於生效時間。'
    }
  }
  if (step.value === 3 && (!attrs.value.some(row => row.checked) || attrs.value.some(row => row.checked && !row.value.trim()))) {
    error.value = '請勾選至少一項特性並輸入變更值。'
  }
  if (step.value === 4) {
    if (!factorFields.some(({ key }) => enabled[key]) || factorFields.some(({ key }) => enabled[key] && !factors[key])) {
      error.value = '請勾選至少一項影響條件，並設定條件值。'
    } else selectedLots.value = candidates.value.map(lot => lot.no)
  }
  if (error.value) return
  if (step.value < 5) {
    step.value++
  } else {
    confirmAction.value = 'submit'
    confirmation.value = `確認建立 ECN ${basics.no.trim().toUpperCase()}，套用至 ${selectedLots.value.length} 個生產批？`
  }
}

function close(): void {
  if (!dev.value) return emit('close')

  if (step.value === 1 || step.value === 6) return emit('close')
  confirmAction.value = 'close'
  confirmation.value = '結束 ECN 設定並捨棄本次輸入？'
}

function resolve(accepted: boolean): void {
  if (!dev.value) return

  confirmation.value = null
  if (!accepted) return
  if (confirmAction.value === 'close') return emit('close')
  const number = basics.no.trim().toUpperCase()
  if (recorded.has(number)) {
    error.value = 'ECN 編號已存在。'
    return
  }
  basics.no = number
  recorded.add(number)
  step.value = 6
}

function finish(): void {
  if (!dev.value) return

  if (!again.value) return emit('close')
  Object.assign(basics, defaultBasics)
  for (const { key } of factorFields) {
    factors[key] = ''
    enabled[key] = false
  }
  loadAttrs()
  selectedLots.value = []
  again.value = false
  step.value = 1
}
</script>

<template>
  <section class="ecn-wizard" aria-labelledby="ecn-title">
    <h2 id="ecn-title">ECN 設定精靈</h2>
    <template v-if="dev">
      <ol class="toolbar">
        <li v-for="(title, i) in titles" :key="title" :aria-current="step === i + 1 ? 'step' : undefined">{{ title }}</li>
      </ol>
      <p v-if="step === 1">設定 ECN 的作業站、變更特性與受影響生產批。</p>
      <div v-else-if="step === 2" class="toolbar">
        <label>ECN 編號 <input v-model="basics.no" maxlength="50"></label>
        <label>特性來源 <select v-model="basics.source"><option value="op">按作業站</option><option value="product">按產品</option></select></label>
        <label>作業站 <select v-model="basics.op"><option v-for="op in operations" :key="op">{{ op }}</option></select></label>
        <label v-if="basics.source === 'product'">產品／版本 <select v-model="basics.product"><option v-for="row in products" :key="row">{{ row }}</option></select></label>
        <label>生效時間 <input v-model="basics.start" type="datetime-local"></label>
        <label>失效時間 <input v-model="basics.end" type="datetime-local"></label>
      </div>
      <div v-else-if="step === 3" class="grid">
        <table aria-label="設定特性">
          <thead><tr><th scope="col">選取</th><th scope="col">特性</th><th scope="col">目前值</th><th scope="col">變更值</th></tr></thead>
          <tbody>
            <tr v-for="row in attrs" :key="row.no">
              <td><input v-model="row.checked" type="checkbox" :aria-label="'選取 ' + row.no"></td><td>{{ row.no }}</td><td>{{ row.initial }}</td>
              <td><input v-model="row.value" :aria-label="'變更值 ' + row.no" :disabled="!row.checked"></td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-else-if="step === 4" class="toolbar">
        <div v-for="field in factorFields" :key="field.key" class="toolbar">
          <label><input v-model="enabled[field.key]" type="checkbox"> {{ field.label }}</label>
          <select v-model="factors[field.key]" :aria-label="field.label" :disabled="!enabled[field.key]">
            <option value="">請選擇</option><option v-for="value in new Set(lots.map(row => row[field.key]))" :key="value">{{ value }}</option>
          </select>
        </div>
      </div>
      <template v-else-if="step === 5">
        <div class="toolbar">
          <button type="button" @click="selectedLots = candidates.map(row => row.no)">全選</button>
          <button type="button" @click="selectedLots = []">取消全選</button>
        </div>
        <div class="grid">
          <table aria-label="受影響生產批">
            <thead><tr><th scope="col">選取</th><th scope="col">生產批號</th><th scope="col">客戶</th><th scope="col">工單</th><th scope="col">產品／版本</th></tr></thead>
            <tbody>
              <tr v-for="row in candidates" :key="row.no">
                <td><input v-model="selectedLots" type="checkbox" :value="row.no" :aria-label="'選取 ' + row.no"></td>
                <td>{{ row.no }}</td><td>{{ row.customer }}</td><td>{{ row.mo }}</td><td>{{ row.product }}</td>
              </tr>
              <tr v-if="!candidates.length"><td colspan="5">無符合條件的生產批。</td></tr>
            </tbody>
          </table>
        </div>
      </template>
      <template v-else>
        <p role="status">已建立 DEV ECN {{ basics.no }}，選取 {{ selectedLots.length }} 個生產批。</p>
        <label><input v-model="again" type="checkbox"> 再執行一次</label>
      </template>
      <p v-if="error" role="alert">{{ error }}</p>
      <footer class="toolbar">
        <button v-if="step > 1 && step < 6" type="button" @click="step--; error = ''">上一步</button>
        <button v-if="step < 6" type="button" @click="next">{{ step === 5 ? '確認 ECN' : '下一步' }}</button>
        <button v-else type="button" @click="finish">完成</button>
        <button type="button" @click="close">關閉</button>
      </footer>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p role="status">{{ unavailableMessage }}</p>
      <button type="button" @click="emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.ecn-wizard { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p, ol { margin: 0; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
ol { padding: 0; list-style: none; }
li { padding: 4px 8px; border: 1px solid var(--line); }
[aria-current="step"] { background: #fff6c5; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
