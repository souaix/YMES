<script setup lang="ts">
import { computed, ref } from 'vue'

type Piece = { itemName: string; pieceSource: string; pieceData: string; direction: 0 | 1 | 2; startIndex: number; pieceLength: number; dataScript: string; memo: string }
type Label = { labelNo: string; labelType: string; labelSource: number; formatFileName: string; printerNo: string; description: string; pieces: Piece[] }
type LabelRow = Label & { creator: string; createDate: string; reviser?: string; reviseDate?: string; dataStamp: number }
type Mode = 'add' | 'edit' | 'copy' | ''
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd107-label-data.v1'
const labelTypes = ['SN', 'MATERIAL', 'SORT_PASS', 'SORT_NG', 'SORT_INV']
const sources = ['LS', 'LB', 'MB', 'PB', 'WK', 'UD', 'RAW', 'SORTBIN', 'SC', 'SQL', 'D2', 'M2', 'M3', 'Y4', 'Y2', 'Y1', 'W2']
const autoSources = ['WK', 'D2', 'M2', 'M3', 'Y4', 'Y2', 'Y1', 'W2']
const draft = ref<Label>(blank())
const labelMode = ref<Mode>('')
const previewMode = ref<'Test' | 'Preview' | ''>('')
const testKey = ref('DEV-LOT-001')
const testedKey = ref('DEV-LOT-001')
const rows = ref(load())
const selectedNo = ref('')
const keyword = ref('')
const query = ref('')
const typeQuery = ref('')
const error = ref('')
const notice = ref('')
const user = computed(() => props.currentUserNo || 'DEV001')
const visible = computed(() => rows.value.filter(row => row.labelNo.toUpperCase().includes(query.value) && (!typeQuery.value || row.labelType === typeQuery.value)))
const active = computed(() => visible.value.find(row => row.labelNo === selectedNo.value) ?? visible.value[0])

function blankPiece(): Piece { return { itemName: '', pieceSource: 'UD', pieceData: '', direction: 0, startIndex: 0, pieceLength: 1, dataScript: '', memo: '' } }
function blank(): Label { return { labelNo: '', labelType: 'SN', labelSource: 1, formatFileName: '', printerNo: '', description: '', pieces: [] } }

function load(): LabelRow[] {
  if (!props.developmentPreview) return []
  try {
    const stored = localStorage.getItem(storageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  return [{ ...blank(), labelNo: 'DEV-PRD-LABEL-01', formatFileName: 'dev-product.btw', description: '產品批號範例', creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0,
    pieces: [{ ...blankPiece(), itemName: 'LotNo', pieceSource: 'LB', pieceData: 'LotNo' }, { ...blankPiece(), itemName: 'Title', pieceData: 'DEV 生產標籤' }] }]
}

function saveRows(next: LabelRow[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 標籤。'; return false }
  rows.value = next; error.value = ''; return true
}

function open(mode: Mode): void {
  const source = mode === 'add' ? undefined : active.value
  draft.value = source ? { ...source, pieces: source.pieces.map(piece => ({ ...piece })) } : blank()
  labelMode.value = mode; error.value = ''
}

function move(index: number, step: number): void {
  const next = index + step
  if (next < 0 || next >= draft.value.pieces.length) return
  const [piece] = draft.value.pieces.splice(index, 1)
  draft.value.pieces.splice(next, 0, piece)
}

function validate(): string {
  const row = draft.value
  if (!row.labelNo.trim() || !row.labelType || !row.formatFileName.trim() || !Number.isInteger(row.labelSource) || row.labelSource < 0 || row.labelSource > 9) return '請輸入標籤編號、類型、來源代碼及格式檔名。'
  if (!row.pieces.length || row.pieces.length > 99) return '標籤至少要有一筆明細，最多 99 筆。'
  const names = new Set<string>()
  for (const [index, piece] of row.pieces.entries()) {
    const name = piece.itemName.trim()
    const item = `第 ${index + 1} 筆明細`
    if (!name || names.has(name)) return `${item}的 ItemName 為空白或重複。`
    names.add(name)
    if (piece.pieceSource === 'RAW' && row.labelType !== 'MATERIAL') return `${item}使用 RAW，LabelType 須為 MATERIAL。`
    if (piece.pieceSource === 'SORTBIN' && !['SORT_PASS', 'SORT_NG', 'SORT_INV'].includes(row.labelType)) return `${item}使用 SORTBIN，請選擇 SORT 標籤類型。`
    if (['SC', 'SQL'].includes(piece.pieceSource)) {
      if (!piece.dataScript.trim()) return `${item}缺少 DataScript。`
    } else if (!autoSources.includes(piece.pieceSource) && !piece.pieceData.trim()) return `${item}缺少 ItemData。`
    if (piece.direction !== 0 && (!Number.isInteger(piece.startIndex) || piece.startIndex < 0 || piece.startIndex > 99
      || !Number.isInteger(piece.pieceLength) || piece.pieceLength < 1 || piece.pieceLength > 99)) return `${item}的起始位置或擷取長度超出範圍。`
  }
  return ''
}

function save(close: boolean): void {
  error.value = validate()
  if (error.value) return
  const labelNo = draft.value.labelNo.trim().toUpperCase()
  const existing = rows.value.find(row => row.labelNo === labelNo)
  if (labelMode.value !== 'edit' && existing) { error.value = '標籤編號已存在。'; return }
  if (labelMode.value === 'edit' && !existing) return
  const now = new Date().toISOString()
  const record: Label = { labelNo, labelType: draft.value.labelType, labelSource: draft.value.labelSource, formatFileName: draft.value.formatFileName.trim(), printerNo: draft.value.printerNo.trim(), description: draft.value.description.trim(),
    pieces: draft.value.pieces.map(piece => ({ ...piece, itemName: piece.itemName.trim(), pieceData: piece.pieceData.trim() })) }
  const saved: LabelRow = existing ? { ...existing, ...record, dataStamp: existing.dataStamp + 1, reviser: user.value, reviseDate: now }
    : { ...record, dataStamp: 0, creator: user.value, createDate: now }
  if (!saveRows(existing ? rows.value.map(row => row.labelNo === labelNo ? saved : row) : [...rows.value, saved])) return
  selectedNo.value = labelNo; query.value = ''; keyword.value = ''; typeQuery.value = ''
  notice.value = '標籤及明細已儲存。'; labelMode.value = close ? '' : 'edit'; draft.value.labelNo = labelNo
}

function remove(): void {
  const row = active.value
  if (row && window.confirm(`確定刪除 ${row.labelNo} 及其明細？`) && saveRows(rows.value.filter(item => item.labelNo !== row.labelNo))) notice.value = '標籤已刪除。'
}

function preview(piece: Piece): string {
  if (piece.pieceSource === 'SC' || piece.pieceSource === 'SQL') return '等待正式產值'
  const supplied: Record<string, string> = { WK: 'DEV-WEEK', D2: '19', M2: '11', M3: 'NOV', Y4: '2026', Y2: '26', Y1: '6', W2: '47' }
  const value = piece.pieceSource === 'UD' ? piece.pieceData : supplied[piece.pieceSource]
    ?? (piece.pieceData.toUpperCase() === 'LOTNO' ? testedKey.value : `DEV ${piece.pieceData}`)
  if (piece.direction === 1) return value.slice(piece.startIndex, piece.startIndex + piece.pieceLength)
  if (piece.direction === 2) return value.slice(Math.max(0, value.length - piece.startIndex - piece.pieceLength), Math.max(0, value.length - piece.startIndex))
  return value
}

const format = (value?: string) => value?.replace('T', ' ').slice(0, 19) ?? ''
</script>

<template>
  <main class="label-master">
    <header><h1>標籤設定</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">標籤設定等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase()"><input v-model="keyword" aria-label="標籤編號" placeholder="標籤編號" /><select v-model="typeQuery" aria-label="標籤類型"><option value="">所有類型</option><option v-for="type in labelTypes" :key="type">{{ type }}</option></select><button>查詢</button></form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!active" @click="open('edit')">EDIT</button><button :disabled="!active" @click="remove">DEL</button><button :disabled="!active" @click="open('copy')">Copy</button><button :disabled="!active" @click="previewMode = 'Test'">Test</button><button :disabled="!active" @click="testedKey = 'DEV-LOT-001'; previewMode = 'Preview'">Preview</button></div>
      <div class="grid"><table><thead><tr><th>LabelNo</th><th>LabelType</th><th>LabelSource</th><th>FormatFileName</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="row.labelNo" :class="{ selected: active?.labelNo === row.labelNo }" @click="selectedNo = row.labelNo"><td><button @click="selectedNo = row.labelNo">{{ row.labelNo }}</button></td><td>{{ row.labelType }}</td><td>{{ row.labelSource }}</td><td>{{ row.formatFileName }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ format(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ format(row.reviseDate) }}</td></tr>
        <tr v-if="!visible.length"><td colspan="9">沒有符合的標籤。</td></tr></tbody></table></div>
      <div v-if="active" class="grid"><table><thead><tr><th>Position</th><th>ItemName</th><th>ItemSource</th><th>ItemData</th><th>Direction</th><th>StartIndex</th><th>CaptureLength</th><th>Memo</th></tr></thead><tbody>
        <tr v-for="(piece, index) in active.pieces" :key="index"><td>{{ index + 1 }}</td><td>{{ piece.itemName }}</td><td>{{ piece.pieceSource }}</td><td>{{ piece.pieceData }}</td><td>{{ ['All', 'Left', 'Right'][piece.direction] }}</td><td>{{ piece.direction ? piece.startIndex : '' }}</td><td>{{ piece.direction ? piece.pieceLength : '' }}</td><td>{{ piece.memo }}</td></tr>
      </tbody></table></div>
      <p v-if="error && !labelMode" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="labelMode" class="overlay"><form class="editor" role="dialog" aria-modal="true" aria-label="標籤明細設定" @submit.prevent="save(true)">
      <h2>標籤 {{ labelMode.toUpperCase() }}</h2>
      <div class="fields"><label>LabelNo<input v-model="draft.labelNo" :readonly="labelMode === 'edit'" maxlength="50" required /></label><label>LabelType<select v-model="draft.labelType"><option v-for="type in labelTypes" :key="type">{{ type }}</option></select></label>
        <label>LabelSource<input v-model.number="draft.labelSource" type="number" min="0" max="9" step="1" required /></label><label>FormatFileName<input v-model="draft.formatFileName" maxlength="100" required /></label><label>PrinterNo<input v-model="draft.printerNo" maxlength="100" /></label><label>Description<input v-model="draft.description" maxlength="255" /></label></div>
      <header><h3>標籤明細</h3><button type="button" :disabled="draft.pieces.length >= 99" @click="draft.pieces.push(blankPiece())">ADD</button></header>
      <fieldset v-for="(piece, index) in draft.pieces" :key="index" class="fields"><legend>Position {{ index + 1 }}</legend>
        <label>ItemName<input v-model="piece.itemName" maxlength="50" required /></label><label>ItemSource<select v-model="piece.pieceSource"><option v-for="source in sources" :key="source">{{ source }}</option></select></label>
        <label v-if="!autoSources.includes(piece.pieceSource)">{{ ['SC', 'SQL'].includes(piece.pieceSource) ? 'DataScript' : 'ItemData' }}<textarea v-if="['SC', 'SQL'].includes(piece.pieceSource)" v-model="piece.dataScript" /><input v-else v-model="piece.pieceData" maxlength="50" /></label>
        <label>Direction<select v-model.number="piece.direction"><option :value="0">All</option><option :value="1">Left</option><option :value="2">Right</option></select></label>
        <label v-if="piece.direction">StartIndex<input v-model.number="piece.startIndex" type="number" min="0" max="99" /></label><label v-if="piece.direction">CaptureLength<input v-model.number="piece.pieceLength" type="number" min="1" max="99" /></label>
        <label>Memo<input v-model="piece.memo" maxlength="255" /></label><div class="moves"><button type="button" :disabled="index === 0" @click="move(index, -1)">上移</button><button type="button" :disabled="index === draft.pieces.length - 1" @click="move(index, 1)">下移</button><button type="button" @click="draft.pieces.splice(index, 1)">DEL</button></div>
      </fieldset>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="save(false)">Apply</button><button type="button" @click="labelMode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="previewMode && active" class="overlay"><section class="editor preview" role="dialog" aria-modal="true" aria-label="DEV 標籤欄位預覽"><header><h2>{{ previewMode }} {{ active.labelNo }}</h2><button @click="previewMode = ''">關閉</button></header>
      <p>DEV 欄位值預覽。正式格式檔與列印待接入。</p>
      <form v-if="previewMode === 'Test'" class="toolbar" @submit.prevent="testedKey = testKey.trim()"><label>測試批號 <input v-model="testKey" required /></label><button>產生欄位值</button></form>
      <div class="grid"><table><thead><tr><th>ItemName</th><th>Value</th></tr></thead><tbody><tr v-for="(piece, index) in active.pieces" :key="index"><td>{{ piece.itemName }}</td><td>{{ preview(piece) }}</td></tr></tbody></table></div>
    </section></div>
  </main>
</template>

<style scoped>
.label-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer,.moves{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,h3,p{margin:0}h1{font-size:17px}h2,h3{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input,select,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:10px;width:min(880px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.preview{width:min(580px,95vw)}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:8px}.fields label{display:grid;gap:3px}.fields input,.fields select,.fields textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.moves,footer{justify-content:flex-end}
</style>
