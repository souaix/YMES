<script setup lang="ts">
import { computed, ref } from 'vue'
import { loadDevelopmentRunCards, newCardField, runCardStorageKey, type CardField, type RunCard } from './runCardDevelopment'

type Mode = 'add' | 'edit' | 'copy' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const rows = ref(props.developmentPreview ? loadDevelopmentRunCards() : [])
const selected = ref('')
const keyword = ref('')
const query = ref('')
const visible = computed(() => rows.value.filter(row => row.no.includes(query.value)))
const active = computed(() => visible.value.find(row => row.no === selected.value) ?? visible.value[0])
const mode = ref<Mode>('')
const draft = ref<RunCard>(blank())
const error = ref('')
const notice = ref('')
const testing = ref(false)
const lotNo = ref('DEV-LOT-001')
const previewLot = ref('DEV-LOT-001')
const types = [{ key: 0, label: 'General' }, { key: 1, label: 'Detail' }]
const fieldTypes = ['String', 'Code128', 'QRCode', 'Picture', 'Sub RunCard']
const tables = ['TBLWIPLOTBASIS', 'TBLWIPLOTSTATE']
const sources = ['TB', 'UD', 'SC', 'SQL', 'SR']
const user = computed(() => props.currentUserNo || 'DEV001')

function blank(): RunCard { return { no: '', type: 0, main: true, fileName: '', printer: '', description: '', headerStart: '', headerEnd: '', fields: [], creator: '', createDate: '', dataStamp: 0 } }

function saveRows(next: RunCard[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(runCardStorageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 流程卡。'; return false }
  rows.value = next; error.value = ''; return true
}

function open(next: Mode): void {
  const current = next === 'add' ? undefined : active.value
  draft.value = current ? { ...current, fields: current.fields.map(field => ({ ...field })) } : blank()
  error.value = ''; mode.value = next
}

function validate(card: RunCard): string {
  if (!card.no || !card.fileName) return '流程卡編號與格式檔名必填。'
  if (card.type === 1 && (card.headerStart || card.headerEnd)) {
    if (![card.headerStart, card.headerEnd].every(text => /^\d{1,5}$/.test(text)) || Number(card.headerStart) > Number(card.headerEnd)) return '表頭起訖需一起填寫，且起始不可大於結束。'
  }
  const names = new Set<string>()
  for (const field of card.fields) {
    if (!field.fileField || names.has(field.fileField)) return '格式欄位名稱必填且不可重複。'
    names.add(field.fileField)
    if (field.valueType === 4 && (field.source !== 'SR' || !rows.value.some(row => row.no === field.value && row.no !== card.no))) return `${field.fileField} 請選擇另一張流程卡。`
    if (field.valueType !== 4 && field.source === 'SR') return `${field.fileField} 的 SR 來源須搭配子流程卡欄位。`
    if (field.source === 'TB' && (!tables.includes(field.sourceName) || !field.value)) return `${field.fileField} 缺少來源表或欄位。`
    if ((field.source === 'SC' || field.source === 'SQL') && !field.script.trim()) return `${field.fileField} 缺少腳本。`
    if (field.source === 'UD' && !field.value) return `${field.fileField} 缺少自訂內容。`
    if (field.direction && (!/^\d{1,2}$/.test(field.start) || !/^[1-9]\d?$/.test(field.length))) return `${field.fileField} 缺少有效的起始位置或擷取長度。`
  }
  return ''
}

function save(close: boolean): void {
  const form = draft.value
  const card = { ...form, no: form.no.trim().toUpperCase(), fileName: form.fileName.trim(),
    fields: form.fields.map(field => ({ ...field, fileField: field.fileField.trim(), value: field.value.trim() })) }
  error.value = validate(card)
  if (error.value) return
  const old = rows.value.find(row => row.no === card.no)
  if (mode.value !== 'edit' && old) { error.value = '流程卡編號已存在。'; return }
  if (mode.value === 'edit' && !old) return
  const now = new Date().toISOString()
  const saved = old ? { ...card, reviser: user.value, reviseDate: now, dataStamp: old.dataStamp + 1 }
    : { ...card, creator: user.value, createDate: now, dataStamp: 0, reviser: undefined, reviseDate: undefined }
  if (!saveRows(old ? rows.value.map(row => row.no === card.no ? saved : row) : [...rows.value, saved])) return
  selected.value = card.no
  query.value = ''; keyword.value = ''
  draft.value = { ...saved, fields: saved.fields.map(field => ({ ...field })) }
  mode.value = close ? '' : 'edit'
  notice.value = '流程卡已儲存。'
}

function remove(): void {
  const card = active.value
  if (!card) return
  if (rows.value.some(row => row.fields.some(field => field.source === 'SR' && field.value === card.no))) { error.value = '此流程卡目前被另一張流程卡引用。'; return }
  if (window.confirm(`確定刪除 ${card.no} 與欄位明細？`) && saveRows(rows.value.filter(row => row.no !== card.no))) notice.value = '流程卡已刪除。'
}

function preview(field: CardField): string {
  if (field.source === 'SC' || field.source === 'SQL') return field.defaultValue || '等待正式產值'
  if (field.source === 'SR') return `子流程卡 ${field.value}`
  const value = field.source === 'UD' ? field.value : field.value.toUpperCase() === 'LOTNO' ? previewLot.value : field.defaultValue || `DEV ${field.value}`
  const start = Number(field.start)
  const length = Number(field.length)
  if (field.direction === 1) return value.slice(start, start + length)
  if (field.direction === 2) return value.slice(Math.max(0, value.length - start - length), Math.max(0, value.length - start))
  return value
}
</script>

<template>
  <main class="card-page">
    <header><h1>自定義流程卡</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">自定義流程卡等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase()"><label>RunCardNo <input v-model="keyword" type="search" /></label><button>查詢</button></form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!active" @click="open('edit')">EDIT</button><button :disabled="!active" @click="remove">DEL</button><button :disabled="!active" @click="open('copy')">Copy</button><button :disabled="!active" @click="testing = true">Test</button></div>
      <div class="grid"><table><thead><tr><th>RunCardNo</th><th>RunCardType</th><th>MainRunCard</th><th>FormatFileName</th><th>PrinterNo</th><th>HeaderStart</th><th>HeaderEnd</th><th>Description</th><th>Creator</th><th>Reviser</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="row.no" :class="{ selected: active?.no === row.no }" @click="selected = row.no"><td><button @click="selected = row.no">{{ row.no }}</button></td><td>{{ types.find(type => type.key === row.type)?.label }}</td><td>{{ row.main ? 'Yes' : 'No' }}</td><td>{{ row.fileName }}</td><td>{{ row.printer }}</td><td>{{ row.headerStart }}</td><td>{{ row.headerEnd }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.reviser }}</td></tr><tr v-if="!visible.length"><td colspan="10">沒有符合的流程卡。</td></tr>
      </tbody></table></div>
      <div v-if="active" class="grid"><table><thead><tr><th>FileField</th><th>FieldValueType</th><th>DataSource</th><th>DataSourceName</th><th>FieldData</th><th>Memo</th></tr></thead><tbody><tr v-for="field in active.fields" :key="field.fileField"><td>{{ field.fileField }}</td><td>{{ fieldTypes[field.valueType] }}</td><td>{{ field.source }}</td><td>{{ field.sourceName }}</td><td>{{ field.value }}</td><td>{{ field.memo }}</td></tr></tbody></table></div>
      <p v-if="error && !mode" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay"><form class="editor" role="dialog" aria-modal="true" aria-label="流程卡定義" @submit.prevent="save(true)"><h2>Run Card {{ mode.toUpperCase() }}</h2>
      <div class="fields"><label>RunCardNo<input v-model="draft.no" :readonly="mode === 'edit'" required maxlength="50" /></label><label>RunCardType<select v-model.number="draft.type" @change="draft.headerStart = ''; draft.headerEnd = ''"><option v-for="type in types" :key="type.key" :value="type.key">{{ type.label }}</option></select></label><label class="check"><input v-model="draft.main" type="checkbox" />MainRunCard</label><label>FormatFileName<input v-model="draft.fileName" maxlength="100" required /></label><label>PrinterNo<input v-model="draft.printer" maxlength="100" /></label><label>Description<input v-model="draft.description" maxlength="255" /></label><template v-if="draft.type === 1"><label>DetailHeaderStart<input v-model="draft.headerStart" inputmode="numeric" maxlength="5" /></label><label>DetailHeaderEnd<input v-model="draft.headerEnd" inputmode="numeric" maxlength="5" /></label></template></div>
      <header><h3>欄位明細</h3><button type="button" @click="draft.fields.push(newCardField())">ADD</button></header>
      <fieldset v-for="(field, index) in draft.fields" :key="index" class="fields"><legend>FileField {{ index + 1 }}</legend>
        <label>FileField<input v-model="field.fileField" maxlength="50" required /></label><label>FieldValueType<select v-model.number="field.valueType" @change="field.source = field.valueType === 4 ? 'SR' : 'UD'; field.value = ''"><option v-for="(type, number) in fieldTypes" :key="type" :value="number">{{ type }}</option></select></label>
        <label>DataSource<select v-model="field.source" :disabled="field.valueType === 4"><option v-for="source in sources.filter(source => field.valueType === 4 || source !== 'SR')" :key="source">{{ source }}</option></select></label>
        <label v-if="field.source === 'TB'">DataSourceName<select v-model="field.sourceName"><option value="">請選擇</option><option v-for="table in tables" :key="table">{{ table }}</option></select></label>
        <label v-if="field.source === 'SC' || field.source === 'SQL'">DataScript<textarea v-model="field.script" /></label>
        <label v-else-if="field.source === 'SR'">Sub RunCard<select v-model="field.value"><option value="">請選擇</option><option v-for="card in rows.filter(row => row.no !== draft.no)" :key="card.no">{{ card.no }}</option></select></label>
        <label v-else>FieldData<input v-model="field.value" maxlength="50" /></label>
        <label>Direction<select v-model.number="field.direction"><option :value="0">All</option><option :value="1">Left</option><option :value="2">Right</option></select></label><template v-if="field.direction"><label>StartIndex<input v-model="field.start" inputmode="numeric" maxlength="2" /></label><label>DataLength<input v-model="field.length" inputmode="numeric" maxlength="2" /></label></template><label>DefaultValue<input v-model="field.defaultValue" maxlength="100" /></label><label>Memo<input v-model="field.memo" maxlength="255" /></label><button type="button" @click="draft.fields.splice(index, 1)">DEL</button>
      </fieldset>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="save(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="testing && active" class="overlay"><section class="editor" role="dialog" aria-modal="true" aria-label="流程卡測試"><header><h2>Test {{ active.no }}</h2><button @click="testing = false">關閉</button></header><p>DEV 測試欄位值。</p><form class="toolbar" @submit.prevent="previewLot = lotNo.trim()"><label>測試批號 <input v-model="lotNo" required /></label><button>查詢</button></form><div class="grid"><table><thead><tr><th>欄位</th><th>值</th></tr></thead><tbody><tr v-for="field in active.fields" :key="field.fileField"><td>{{ field.fileField }}</td><td>{{ preview(field) }}</td></tr></tbody></table></div></section></div>
  </main>
</template>

<style scoped>
.card-page{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,h3,p{margin:0}h1{font-size:17px}h2,h3{font-size:14px}.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input,select,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:10px;width:min(900px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(145px,1fr));gap:8px}.fields label{display:grid;gap:3px}.fields .check{display:flex;align-items:center}.fields input:not([type=checkbox]),.fields textarea,.fields select{min-width:0;padding:5px;border:1px solid #aaa797;background:#fff5d6}footer{justify-content:flex-end}
</style>
