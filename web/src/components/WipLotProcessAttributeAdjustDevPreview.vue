<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

const props = defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const records = dev.value ? [
  { group: 'DEV-ATTR-G1', lot: 'DEV-ATTR001', op: 'WB' },
  { group: 'DEV-ATTR-G2', lot: 'DEV-ATTR001', op: 'FT' },
] : []
interface Attribute { id: string; group: string; no: string; component: string; source: number; value: string }
const attrs = ref<Attribute[]>(dev.value ? [
  { id: '1', group: 'DEV-ATTR-G1', no: 'BOND_FORCE', component: '', source: 0, value: '25' },
  { id: '2', group: 'DEV-ATTR-G1', no: 'GOODQTY', component: '', source: 0, value: '100' },
  { id: '3', group: 'DEV-ATTR-G1', no: 'RECIPE', component: '', source: 3, value: 'A1' },
  { id: '4', group: 'DEV-ATTR-G2', no: 'VOLTAGE', component: 'DEV-C001', source: 0, value: '3.2' },
  { id: '5', group: 'DEV-ATTR-G2', no: 'VOLTAGE', component: 'DEV-C002', source: 0, value: '3.4' },
  { id: '6', group: 'DEV-ATTR-G2', no: 'CPASS', component: 'DEV-C001', source: 0, value: 'Y' },
] : [])
const restricted = new Set(['GOODQTY', 'SCRAPQTY', 'CGOODQTY', 'CSCRAPQTY', 'CPASS'])
const lot = ref('')
const reports = ref<typeof records>([])
const report = ref('')
const detail = ref('')
const tab = ref<'lot' | 'component'>('lot')
const rows = computed(() => attrs.value.filter(row => row.group === detail.value && Boolean(row.component) === (tab.value === 'component')))
const selected = ref('')
const editor = ref<Attribute[]>([])
const description = ref('')
const component = ref('')
const error = ref('')
const result = ref('')
const confirmation = ref<string | null>(null)
const changes = computed(() => editor.value.filter(row => attrs.value.find(orig => orig.id === row.id)?.value !== row.value))

function query(): void {
  const input = lot.value.trim().toUpperCase()
  reports.value = records.filter(row => row.lot === input)
  report.value = detail.value = selected.value = result.value = ''
  error.value = reports.value.length ? '' : '查無該批報工歷程。'
}
function showDetail(): void {
  detail.value = report.value
  showTab('lot')
}
function showTab(value: typeof tab.value): void {
  tab.value = value
  selected.value = error.value = result.value = ''
}
function openEditor(): void {
  const row = rows.value.find(item => item.id === selected.value)
  if (!row) return
  error.value = result.value = ''
  if (row.source !== 0 || restricted.has(row.no)) {
    error.value = '此來源或數量／判定特性不可調整。'
    return
  }
  editor.value = (row.component ? rows.value.filter(item => item.no === row.no && item.source === 0) : [row]).map(item => ({ ...item }))
  description.value = component.value = ''
}
function findComponent(): void {
  const row = editor.value.find(item => item.component === component.value.trim().toUpperCase())
  error.value = row ? '' : '查無這個元件。'
  if (row) document.getElementById('attr-value-' + row.id)?.focus()
}
function requestSave(): void {
  error.value = changes.value.length ? '' : '特性值沒有變更。'
  if (!error.value) confirmation.value = `保存 ${changes.value.length} 項製程特性調整？`
}
function resolve(accepted: boolean): void {
  confirmation.value = null
  if (!accepted) return
  const adjusted = changes.value
  attrs.value = attrs.value.map(row => adjusted.find(change => change.id === row.id) ?? row)
  result.value = `DEV 已調整 ${adjusted.length} 項特性。`
  editor.value = []
}
</script>

<template>
  <section class="attr-page" aria-label="生產批作業站製程特性值調整">
    <template v-if="dev">
      <template v-if="!editor.length">
        <form class="toolbar" @submit.prevent="query">
          <label>批號 <input v-model="lot" list="dev-attr-lots" maxlength="50"></label><datalist id="dev-attr-lots"><option>DEV-ATTR001</option></datalist>
          <button type="submit" class="query-button">查詢</button><button type="button" :disabled="!report" @click="showDetail">明細</button><button type="button" @click="$emit('close')">關閉</button>
        </form>
        <div class="grid"><table aria-label="報工歷程">
          <thead><tr><th>選取</th><th>報工序號</th><th>批號</th><th>作業站</th></tr></thead>
          <tbody>
            <tr v-for="row in reports" :key="row.group" :class="{ selected: row.group === report }"><td><input v-model="report" type="radio" name="attr-reports" :value="row.group" :aria-label="'選取 ' + row.group"></td><td>{{ row.group }}</td><td>{{ row.lot }}</td><td>{{ row.op }}</td></tr>
            <tr v-if="!reports.length"><td colspan="4">沒有報工資料。</td></tr>
          </tbody>
        </table></div>
        <template v-if="detail">
          <div class="toolbar"><button type="button" :aria-pressed="tab === 'lot'" @click="showTab('lot')">批特性</button><button type="button" :aria-pressed="tab === 'component'" @click="showTab('component')">元件特性</button><button type="button" class="primary" :disabled="!selected" @click="openEditor">調整特性值</button></div>
          <div class="grid"><table aria-label="報工特性">
            <thead><tr><th>選取</th><th>特性</th><th v-if="tab === 'component'">元件</th><th>來源</th><th>目前值</th></tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="row.id" :class="{ selected: selected === row.id }"><td><input v-model="selected" type="radio" name="attr-detail" :value="row.id" :aria-label="'選取 ' + row.no + ' ' + row.component"></td><td>{{ row.no }}</td><td v-if="tab === 'component'">{{ row.component }}</td><td>{{ row.source }}</td><td>{{ row.value }}</td></tr>
              <tr v-if="!rows.length"><td :colspan="tab === 'component' ? 5 : 4">沒有特性記錄。</td></tr>
            </tbody>
          </table></div>
        </template>
      </template>
      <form v-else class="editor" @submit.prevent="requestSave">
        <h2>調整 {{ editor[0]?.no }}</h2>
        <div v-if="tab === 'component'" class="toolbar"><label>找元件 <input v-model="component" maxlength="50" @keydown.enter.prevent="findComponent"></label><button type="button" @click="findComponent">尋找</button></div>
        <div v-for="row in editor" :key="row.id" class="toolbar"><label>{{ row.component || row.no }} <input :id="'attr-value-' + row.id" v-model="row.value" maxlength="50"></label></div>
        <label>調整說明 <input v-model="description" maxlength="255"></label>
        <div class="toolbar"><button type="submit" class="primary">確定</button><button type="button" @click="editor = []; error = ''">取消</button></div>
      </form>
      <p v-if="error" role="alert">{{ error }}</p><p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p role="status">正式環境尚未開放此功能。</p><button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.attr-page, .editor { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
.editor { background: white; border: 1px solid var(--line); }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
h2, p { margin: 0; }
[role='alert'] { color: #9d2727; }
</style>
