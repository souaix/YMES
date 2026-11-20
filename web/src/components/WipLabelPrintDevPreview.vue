<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const fixtures = dev.value ? new Map([
  ['DEV-LABEL001', { product: 'DEV-IC', version: 'A', no: 'DEV-IC-LOT', type: 'LOT', template: 'DEV-IC-LOT', qty: 120 }],
  ['DEV-LABEL002', { product: 'DEV-PKG', version: 'B', no: 'DEV-GENERIC', type: 'LOT', template: 'DEV-GENERIC', qty: 75 }],
]) : new Map<string, never>()
const batches = ref('')
const printer = ref('預覽')
const software = 'BarTender'
const rows = ref<{ lot: string; printer: string; label?: NonNullable<ReturnType<typeof fixtures.get>> }[]>([])
const selected = ref('')
const active = computed(() => rows.value.find(row => row.lot === selected.value))
const labelFields = computed(() => {
  const row = active.value
  return row?.label ? [
    { name: 'LOT_NO', value: row.lot },
    { name: 'PRODUCT_NO', value: row.label.product },
    { name: 'VERSION', value: row.label.version },
    { name: 'QTY', value: String(row.label.qty) },
  ] : []
})
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)
const preview = ref(false)

function clear(): void {
  rows.value = []
  selected.value = ''
  error.value = result.value = ''
  preview.value = false
}
function query(): void {
  clear()
  const lotNos = [...new Set(batches.value.toUpperCase().split(',').map(value => value.trim()).filter(Boolean))]
  if (!lotNos.length) {
    error.value = '請輸入批號，多筆請以逗號分隔。'
    return
  }
  rows.value = lotNos.map(lot => ({ lot, printer: printer.value, label: fixtures.get(lot) }))
  selected.value = rows.value.find(row => row.label)?.lot ?? lotNos[0] ?? ''
  const failures = rows.value.filter(row => !row.label).map(row => row.lot)
  if (failures.length) error.value = `無標籤對應批號 ${failures.join('、')}。`
}
function requestPrint(): void {
  const mapped = rows.value.filter(row => row.label)
  if (!mapped.length || !active.value?.label) return
  if (printer.value === '預覽') {
    preview.value = true
    return
  }
  confirmation.value = `模擬將 ${mapped.length} 筆標籤送至 ${printer.value}？`
}
function print(accepted: boolean): void {
  confirmation.value = null
  if (!accepted) return
  result.value = `DEV 已模擬 ${rows.value.filter(row => row.label).length} 筆列印，本頁未送印表機。`
}
</script>

<template>
  <section class="label-page" aria-label="標籤列印">
    <template v-if="dev">
      <form class="toolbar" @submit.prevent="query">
        <label>批號 <input v-model="batches" maxlength="512" placeholder="DEV-LABEL001,DEV-LABEL002" @input="clear"></label>
        <label>標籤來源 <input value="產品" readonly></label>
        <label>標籤軟體 <input :value="software" readonly></label>
        <label>印表機 <select v-model="printer" @change="clear"><option>預覽</option><option>DEV-標籤機</option></select></label>
        <button type="submit" class="query-button">查詢</button>
        <button type="button" :disabled="!active?.label" @click="requestPrint">{{ printer === '預覽' ? '預覽' : '模擬列印' }}</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div class="grid">
        <table aria-label="批次標籤對應">
          <thead><tr><th>選取</th><th>批號</th><th>來源</th><th>類型</th><th>標籤編號</th><th>印表機</th><th>軟體</th><th>結果</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="row.lot" :class="{ selected: selected === row.lot }">
              <td><input v-model="selected" type="radio" name="label-lot" :value="row.lot" :aria-label="'選取 ' + row.lot" @change="preview = false"></td>
              <td>{{ row.lot }}</td><td>{{ row.label ? '產品' : '—' }}</td><td>{{ row.label?.type }}</td><td>{{ row.label?.no }}</td>
              <td>{{ row.printer }}</td><td>{{ software }}</td><td>{{ row.label ? '已對應' : '無對應' }}</td>
            </tr>
            <tr v-if="!rows.length"><td colspan="8">請查詢生產批。</td></tr>
          </tbody>
        </table>
      </div>
      <div class="grid">
        <table aria-label="標籤欄位">
          <thead><tr><th>項目</th><th>值</th></tr></thead>
          <tbody>
            <tr v-for="field in labelFields" :key="field.name"><td>{{ field.name }}</td><td>{{ field.value }}</td></tr>
            <tr v-if="!labelFields.length"><td colspan="2">無可預覽的標籤欄位。</td></tr>
          </tbody>
        </table>
      </div>
      <article v-if="preview && active?.label" class="sample" aria-label="DEV 標籤預覽">
        <strong>{{ active.label.template }}</strong>
        <dl><template v-for="field in labelFields" :key="field.name"><dt>{{ field.name }}</dt><dd>{{ field.value }}</dd></template></dl>
        <button type="button" @click="preview = false">關閉預覽</button>
      </article>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="print" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.label-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
.sample { max-width: 350px; padding: 14px; border: 1px solid var(--line); background: white; }
dl { display: grid; grid-template-columns: auto 1fr; gap: 5px 12px; }
dd, p { margin: 0; }
button { width: max-content; min-height: 30px; }
[role='alert'] { color: #9d2727; }
</style>
