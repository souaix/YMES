<script setup lang="ts">
import { computed, ref } from 'vue'

type PropertyRow = Record<string, any>
type ProductContext = Record<string, any>
type EditorMode = 'add' | 'edit' | 'copy'
type PropertyBasis = { elementNo: string; elementName: string; defaultValue: string; elementSequence: number; controlled: number; allowedValues: string[] }

const props = withDefaults(defineProps<{
  product: ProductContext
  properties: PropertyRow[]
  developmentPreview?: boolean
  canEdit?: boolean
}>(), { developmentPreview: false, canEdit: false })
const emit = defineEmits<{ save: [properties: PropertyRow[]]; close: [] }>()

const statusOptions = [
  { value: 0, label: 'UNFROZEN' },
  { value: 1, label: 'PENDING' },
  { value: 2, label: 'ACTIVE' },
  { value: -1, label: 'UNUSED' },
]
const propertyBasis: PropertyBasis[] = [
  { elementNo: 'DEV-MODEL', elementName: '產品型號屬性', defaultValue: 'CTRL-A', elementSequence: 1, controlled: 1, allowedValues: ['CTRL-A', 'CTRL-B', 'CTRL-C'] },
  { elementNo: 'DEV-COLOR', elementName: '產品顏色', defaultValue: '', elementSequence: 2, controlled: 2, allowedValues: ['BLUE', 'GREEN', 'RED'] },
  { elementNo: 'DEV-CUSTOMER', elementName: '客戶代碼', defaultValue: '', elementSequence: 3, controlled: 0, allowedValues: [] },
  { elementNo: 'DEV-REVISION', elementName: '客戶版次', defaultValue: 'A', elementSequence: 4, controlled: 0, allowedValues: ['A', 'B', 'C'] },
]
const rows = ref<PropertyRow[]>(JSON.parse(JSON.stringify(props.properties ?? [])) as PropertyRow[])
const issueState = ref(0)
const activePropertyNo = ref('')
const selectedPropertyNos = ref<string[]>([])
const notice = ref('')
const error = ref('')
const editor = ref<{ mode: EditorMode; propertyNo: string; defaultValue: string; sequence: number; description: string; issueState: number; dataStamp: number; creator: string; createDate: string; reviser: string; reviseDate: string; controlled: number; saved?: PropertyRow } | null>(null)
const chooser = ref<'property' | 'value' | ''>('')
const readOnly = computed(() => !props.developmentPreview || !props.canEdit)
const shownRows = computed(() => rows.value.filter(row => Number(row.issueState ?? 0) === issueState.value))
const selectedRows = computed(() => rows.value.filter(row => selectedPropertyNos.value.includes(String(row.propertyNo))))
const actionTargets = computed(() => selectedRows.value.length ? selectedRows.value : rows.value.filter(row => String(row.propertyNo) === activePropertyNo.value))
const currentBasis = computed(() => propertyBasis.find(item => item.elementNo === editor.value?.propertyNo) ?? null)
const productNo = computed(() => String(props.product.productNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? ''))
const titleSuffix = computed(() => editor.value?.mode === 'add' ? '(ADD)' : editor.value?.mode === 'edit' ? '(EDIT)' : '(Copy)')
const allowedValues = computed(() => currentBasis.value?.allowedValues ?? [])

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function text(value: unknown): string { return value === null || value === undefined ? '' : String(value) }
function formatDate(value: unknown): string {
  if (!value) return ''
  const date = new Date(String(value))
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat('zh-TW', { calendar: 'gregory', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(date).replaceAll('-', '/')
}
function stateName(value: unknown): string { return statusOptions.find(item => item.value === Number(value))?.label ?? text(value) }
function stateCount(value: number): number { return rows.value.filter(row => Number(row.issueState ?? 0) === value).length }
function currentUser(): string { return 'DEV001' }
function persist(): void { emit('save', clone(rows.value)) }
function clearMessage(): void { notice.value = ''; error.value = '' }
function selectRow(row: PropertyRow, event: MouseEvent | KeyboardEvent): void {
  const key = text(row.propertyNo)
  activePropertyNo.value = key
  if (event.ctrlKey || event.metaKey) {
    selectedPropertyNos.value = selectedPropertyNos.value.includes(key)
      ? selectedPropertyNos.value.filter(item => item !== key)
      : [...selectedPropertyNos.value, key]
  } else {
    selectedPropertyNos.value = [key]
  }
}
function openEditor(mode: EditorMode, row?: PropertyRow): void {
  clearMessage()
  const basis = propertyBasis.find(item => item.elementNo === text(row?.propertyNo))
  editor.value = {
    mode,
    propertyNo: text(row?.propertyNo),
    defaultValue: text(row?.defaultValue ?? row?.value),
    sequence: Number(row?.propertySequence ?? row?.sequence ?? basis?.elementSequence ?? 0),
    description: text(row?.description),
    issueState: Number(row?.issueState ?? 0),
    dataStamp: Number(row?.dataStamp ?? 0),
    creator: mode === 'edit' ? text(row?.creator) : currentUser(),
    createDate: mode === 'edit' ? text(row?.createDate) : new Date().toISOString(),
    reviser: mode === 'edit' ? currentUser() : '',
    reviseDate: mode === 'edit' ? new Date().toISOString() : '',
    controlled: Number(row?.controlled ?? basis?.controlled ?? 0),
    ...(mode === 'edit' && row ? { saved: clone(row) } : {}),
  }
}
function openPropertyChooser(): void {
  if (editor.value?.mode === 'edit') return
  chooser.value = 'property'
}
function chooseProperty(basis: PropertyBasis): void {
  if (!editor.value) return
  editor.value.propertyNo = basis.elementNo
  editor.value.defaultValue = basis.defaultValue
  editor.value.sequence = basis.elementSequence
  editor.value.controlled = basis.controlled
  chooser.value = ''
}
function openValueChooser(): void {
  if (!editor.value?.propertyNo || !allowedValues.value.length) {
    error.value = '此 Property No 沒有可供選取的 MES Parameter 值。'
    return
  }
  error.value = ''
  chooser.value = 'value'
}
function chooseValue(value: string): void {
  if (editor.value) editor.value.defaultValue = value
  chooser.value = ''
}
function saveEditor(closeAfter: boolean): void {
  if (!editor.value) return
  const draft = editor.value
  clearMessage()
  if (!productNo.value) { error.value = 'ProductNo 不可空白。'; return }
  if (!editor.value.propertyNo) { error.value = 'PropertyNo 不可空白。'; return }
  if (!Number.isInteger(editor.value.sequence) || editor.value.sequence < 0 || editor.value.sequence > 99) { error.value = 'Sequence 必須介於 0 到 99。'; return }
  const valueBasis = propertyBasis.find(item => item.elementNo === draft.propertyNo)
  if (draft.defaultValue && valueBasis?.allowedValues.length && !valueBasis.allowedValues.includes(draft.defaultValue)) {
    error.value = 'Property Value 不符合此 DEV MES Parameter 的有效值。'
    return
  }

  const updateKey = draft.mode === 'edit'
    ? text(draft.saved?.propertyNo)
    : draft.saved && text(draft.saved.propertyNo) === draft.propertyNo ? text(draft.saved.propertyNo) : ''
  const duplicate = rows.value.some(row => text(row.propertyNo) === draft.propertyNo && text(row.propertyNo) !== updateKey)
  if (duplicate) { error.value = '同一產品版次不可重複設定相同的 Property No。'; return }
  const now = new Date().toISOString()
  const next: PropertyRow = {
    productNo: productNo.value,
    productVersion: productVersion.value || '-1',
    propertyNo: draft.propertyNo,
    defaultValue: draft.defaultValue,
    propertySequence: draft.sequence,
    description: draft.description,
    issueState: 0,
    dataStamp: draft.mode === 'edit' ? draft.dataStamp + 1 : 0,
    creator: draft.mode === 'edit' ? draft.creator : currentUser(),
    createDate: draft.mode === 'edit' ? draft.createDate : now,
    reviser: draft.mode === 'edit' ? currentUser() : '',
    reviseDate: draft.mode === 'edit' ? now : '',
    controlled: draft.controlled,
  }
  if (updateKey) rows.value = rows.value.map(row => text(row.propertyNo) === updateKey ? next : row)
  else rows.value = [...rows.value, next]
  draft.dataStamp = next.dataStamp
  draft.saved = clone(next)
  persist()
  activePropertyNo.value = next.propertyNo
  selectedPropertyNos.value = [next.propertyNo]
  notice.value = closeAfter ? '產品屬性假資料已儲存。' : '已套用產品屬性假資料；視窗保持開啟。'
  if (closeAfter) editor.value = null
}
function deleteSelected(): void {
  clearMessage()
  const targets = actionTargets.value
  if (!targets.length) { error.value = '請先選取產品屬性資料列。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆產品屬性？')) return
  let deleted = 0
  for (const target of targets) {
    const state = Number(target.issueState ?? 0)
    if (![0, -1, 2].includes(state)) {
      error.value = 'PENDING 狀態不可刪除；WinForms 會在此筆停止後續逐筆刪除。'
      break
    }
    if (state === 2) {
      rows.value = rows.value.map(row => text(row.propertyNo) === text(target.propertyNo) ? { ...row, issueState: -1, dataStamp: Number(row.dataStamp ?? 0) + 1, reviser: currentUser(), reviseDate: new Date().toISOString() } : row)
    } else {
      rows.value = rows.value.filter(row => text(row.propertyNo) !== text(target.propertyNo))
    }
    deleted += 1
  }
  if (deleted) {
    selectedPropertyNos.value = []
    activePropertyNo.value = ''
    persist()
    if (!error.value) notice.value = stateName(targets[0].issueState) === 'ACTIVE' ? 'ACTIVE 資料已標記為 UNUSED。' : '產品屬性假資料已刪除。'
  }
}
function approveSelected(): void {
  clearMessage()
  const targets = actionTargets.value
  if (!targets.length) { error.value = '請先選取產品屬性資料列。'; return }
  if (targets.some(row => Number(row.issueState ?? 0) !== 0)) { error.value = 'Approve 僅接受 UNFROZEN 狀態資料。'; return }
  if (!window.confirm('確定核准選取的 ' + targets.length + ' 筆產品屬性？')) return
  const now = new Date().toISOString()
  rows.value = rows.value.map(row => targets.some(target => text(target.propertyNo) === text(row.propertyNo))
    ? { ...row, issueState: 2, dataStamp: Number(row.dataStamp ?? 0) + 1, reviser: currentUser(), reviseDate: now }
    : row)
  persist()
  issueState.value = 2
  notice.value = 'DEV 假資料：核准流程直接完成為 ACTIVE；正式簽核 Job 設定由 API 處理。'
}
function changeStatus(value: number): void {
  issueState.value = value
  selectedPropertyNos.value = []
  activePropertyNo.value = ''
  clearMessage()
}
function closeMaster(): void { emit('close') }
</script>

<template>
  <section class="property-window" role="dialog" aria-modal="true" aria-label="Product Property">
    <header class="property-title"><span>Product Property</span><button type="button" aria-label="Close" @click="closeMaster">×</button></header>
    <div class="property-status" role="radiogroup" aria-label="Issue state">
      <label v-for="item in statusOptions" :key="item.value"><input type="radio" :checked="issueState === item.value" @change="changeStatus(item.value)" />{{ item.label }}<small>({{ stateCount(item.value) }})</small></label>
    </div>
    <div class="property-body">
      <div class="property-grid-scroll">
        <table class="property-grid">
          <thead><tr><th>Product No</th><th>Product Version</th><th>Property No</th><th>Default Value</th><th>Property Sequence</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in shownRows" :key="row.propertyNo" :class="{ selected: selectedPropertyNos.includes(String(row.propertyNo)) }" tabindex="0" @click="selectRow(row, $event)" @keydown.enter.prevent="selectRow(row, $event)">
              <td>{{ row.productNo || productNo }}</td><td>{{ row.productVersion === '-1' ? '' : row.productVersion || productVersion }}</td><td>{{ row.propertyNo }}</td><td>{{ row.defaultValue ?? row.value }}</td><td>{{ row.propertySequence ?? row.sequence }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!shownRows.length"><td colspan="10" class="empty-row">No Product Property data.</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="property-actions">
        <button class="prd-button confirm" type="button" :disabled="readOnly" @click="openEditor('add')">ADD (&amp;A)</button>
        <button class="prd-button" type="button" :disabled="readOnly || !rows.some(row => row.propertyNo === activePropertyNo && Number(row.issueState) === 0)" @click="openEditor('edit', rows.find(row => row.propertyNo === activePropertyNo))">EDIT (&amp;E)</button>
        <button class="prd-button" type="button" :disabled="readOnly || !actionTargets.length" @click="deleteSelected">DEL (&amp;D)</button>
        <button class="prd-button" type="button" :disabled="readOnly || !activePropertyNo" @click="openEditor('copy', rows.find(row => row.propertyNo === activePropertyNo))">Copy (&amp;C)</button>
        <button class="prd-button" type="button" :disabled="readOnly || !actionTargets.length || actionTargets.some(row => Number(row.issueState) !== 0)" @click="approveSelected">Approve (&amp;Y)</button>
      </aside>
    </div>
    <p v-if="readOnly" class="property-readonly">正式 API 尚未提供產品屬性 CRUD，本畫面唯讀；Development 模式使用獨立假資料。</p>
    <p v-if="error" class="property-message error">{{ error }}</p>
    <p v-else-if="notice" class="property-message">{{ notice }}</p>
    <footer class="property-footer"><span>Product {{ productNo }} / Version {{ productVersion || 'ProductType' }}</span><button class="prd-button" type="button" @click="closeMaster">Close (&amp;X)</button></footer>

    <div v-if="editor" class="property-editor-overlay" @click.self="editor = null">
      <form class="property-editor" @submit.prevent="saveEditor(true)">
        <header class="property-editor-title"><span>Product Property Data Define {{ titleSuffix }}</span><button type="button" aria-label="Close" @click="editor = null">×</button></header>
        <fieldset class="property-editor-fields">
          <legend>Product Property Data</legend>
          <label class="property-product-label"><span>ProductType ProductNo</span><input readonly :value="productNo" /></label>
          <label class="property-version-label"><span>Version</span><input readonly :value="productVersion" /></label>
          <label class="property-no-label"><span>Property No</span><input readonly :value="editor.propertyNo" /><button class="prd-button" type="button" :disabled="editor.mode === 'edit'" @click="openPropertyChooser">...</button></label>
          <label class="property-value-label"><span>Property Value</span><input v-model="editor.defaultValue" @input="clearMessage" /><button class="prd-button" type="button" @click="openValueChooser">...</button></label>
          <label class="property-sequence-label"><span>Sequence</span><input v-model.number="editor.sequence" type="number" min="0" max="99" @input="clearMessage" /></label>
          <label class="property-description-label"><span>Description</span><textarea v-model="editor.description" rows="2" @input="clearMessage"></textarea></label>
        </fieldset>
        <fieldset class="property-editor-audit">
          <legend>Basis Data</legend>
          <label>Creator<input readonly :value="editor.creator" /></label><label>CreateDate<input readonly :value="formatDate(editor.createDate)" /></label>
          <label>Reviser<input readonly :value="editor.reviser" /></label><label>ReviseDate<input readonly :value="formatDate(editor.reviseDate)" /></label>
        </fieldset>
        <p v-if="error" class="property-message error">{{ error }}</p>
        <footer class="property-editor-footer"><button class="prd-button confirm" type="submit">Confirm</button><button class="prd-button" type="button" @click="editor = null">Close</button><button class="prd-button" type="button" @click="saveEditor(false)">Apply</button></footer>
      </form>
    </div>

    <div v-if="chooser" class="property-chooser-overlay" @click.self="chooser = ''">
      <section class="property-chooser">
        <header><h2>{{ chooser === 'property' ? 'Select Property No' : 'Select Property Value' }}</h2><button type="button" aria-label="Close" @click="chooser = ''">×</button></header>
        <div v-if="chooser === 'property'" class="property-chooser-grid"><table><thead><tr><th>Element No</th><th>Element Name</th><th>Default Value</th><th>Sequence</th></tr></thead><tbody><tr v-for="item in propertyBasis" :key="item.elementNo" tabindex="0" @click="chooseProperty(item)" @keydown.enter.prevent="chooseProperty(item)"><td>{{ item.elementNo }}</td><td>{{ item.elementName }}</td><td>{{ item.defaultValue }}</td><td>{{ item.elementSequence }}</td></tr></tbody></table></div>
        <div v-else class="property-value-options"><button v-for="value in allowedValues" :key="value" class="prd-button" type="button" @click="chooseValue(value)">{{ value }}</button></div>
        <p class="property-dev-note">Development 假資料查找；正式選項需由 SYS API 載入。</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.property-window{position:relative;width:min(684px,calc(100vw - 20px));height:min(486px,calc(100vh - 20px));display:flex;flex-direction:column;gap:5px;padding:4px 8px 7px;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 8px 30px #0005;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;overflow:hidden}
.property-title{height:23px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 3px;font-size:13px}.property-title button,.property-editor-title button,.property-chooser header button{border:0;background:transparent;font-size:20px;line-height:18px;cursor:pointer}
.property-status{height:28px;flex:none;display:flex;align-items:center;gap:12px;padding:0 7px;border:1px solid #c9c7bd;background:#faf9f5}.property-status label{display:flex;align-items:center;gap:3px;white-space:nowrap}.property-status input{accent-color:#b4831d}.property-status small{color:#777}
.property-body{display:grid;grid-template-columns:minmax(0,1fr) 80px;gap:15px;flex:1;min-height:0}.property-grid-scroll{overflow:auto;border:1px solid #aaa79f}.property-grid{border-collapse:collapse;white-space:nowrap;font-size:12px}.property-grid th{position:sticky;top:0;z-index:1;height:25px;padding:3px 7px;border:1px solid #c9c5b7;background:#f0b023;color:#27251e;font-weight:600;text-align:left}.property-grid td{height:25px;padding:3px 7px;border:1px solid #d4d1c8;background:#fff}.property-grid tbody tr:nth-child(even) td{background:#faf9f5}.property-grid tbody tr{cursor:pointer}.property-grid tbody tr.selected td{background:#ffeaa1}.property-grid tbody tr:hover td{background:#fff1bd}.property-grid .description-cell{white-space:normal;min-width:150px;max-width:250px}.property-grid .empty-row{text-align:center;color:#777;padding:16px}
.property-actions{display:flex;flex-direction:column;align-items:stretch;gap:6px;padding-top:0}.prd-button{min-height:29px;padding:4px 7px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f3f1eb);color:#25241e;font:inherit;white-space:nowrap;cursor:pointer}.prd-button:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.prd-button:disabled{opacity:.5;cursor:not-allowed}.prd-button.confirm{background:#f7a92c;border-color:#e1971f;color:#fff;font-weight:600}
.property-readonly,.property-dev-note{margin:0;padding:4px 7px;border:1px solid #e4d7a3;background:#fff8dc;color:#5f5427}.property-message{margin:0;padding:4px 7px;border:1px solid #d6c793;background:#fffbeb}.property-message.error{border-color:#d29a8e;background:#fff1ed;color:#7c281d}.property-footer{height:32px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 3px;color:#5f5b50}.property-footer .prd-button{width:80px;height:30px}
.property-editor-overlay,.property-chooser-overlay{position:fixed;inset:0;z-index:111;display:grid;place-items:center;background:#0005;padding:10px}.property-editor{width:min(496px,calc(100vw - 20px));height:min(276px,calc(100vh - 20px));position:relative;display:flex;flex-direction:column;gap:4px;padding:0 7px 5px;background:#fdfcf9;border:1px solid #99968d;box-shadow:0 8px 30px #0006;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;overflow:auto}.property-editor-title{height:23px;flex:none;display:flex;align-items:center;justify-content:space-between;font-size:13px}.property-editor-fields{position:relative;flex:1;min-height:145px;margin:0;padding:15px 9px 7px;border:1px solid #b7b4ab}.property-editor-fields legend,.property-editor-audit legend{padding:0 5px;color:#514d40}.property-editor-fields label{position:absolute;display:flex;align-items:center;gap:5px;white-space:nowrap}.property-editor-fields label>span{width:115px;text-align:right}.property-editor-fields input,.property-editor-fields textarea{height:23px;min-width:0;border:1px solid #99968d;background:#fff2bd;padding:3px 5px;color:#292820;font:inherit}.property-editor-fields input:read-only{background:#efeee9}.property-editor-fields textarea{height:34px;resize:none}.property-editor-fields .property-product-label{left:7px;top:21px}.property-product-label input{width:160px}.property-editor-fields .property-version-label{right:7px;top:21px}.property-version-label>span{width:45px!important}.property-version-label input{width:50px}.property-editor-fields .property-no-label{left:7px;top:49px}.property-no-label input{width:160px}.property-no-label .prd-button,.property-value-label .prd-button{min-width:25px;min-height:24px;padding:1px 3px}.property-editor-fields .property-value-label{left:7px;top:77px}.property-value-label input{width:160px}.property-editor-fields .property-sequence-label{right:7px;top:77px}.property-sequence-label>span{width:75px!important}.property-sequence-label input{width:50px}.property-editor-fields .property-description-label{left:7px;top:107px;align-items:flex-start}.property-description-label>span{padding-top:3px}.property-description-label textarea{width:326px}.property-editor-audit{height:51px;flex:none;margin:0;padding:5px 7px;border:1px solid #b7b4ab;display:grid;grid-template-columns:1fr 1fr;gap:3px 6px}.property-editor-audit label{display:flex;align-items:center;gap:5px;min-width:0}.property-editor-audit input{min-width:0;flex:1;width:70px;height:19px;padding:2px 4px;border:1px solid #aaa;background:#eee;color:#555;font:inherit}.property-editor-footer{height:30px;flex:none;display:flex;justify-content:flex-end;align-items:center;gap:6px}.property-editor-footer .prd-button{width:80px;height:29px}
.property-chooser-overlay{z-index:120}.property-chooser{width:min(530px,calc(100vw - 20px));max-height:70vh;display:flex;flex-direction:column;background:#fdfcf9;border:1px solid #a9a69d;box-shadow:0 8px 30px #0005}.property-chooser header{display:flex;align-items:center;justify-content:space-between;padding:7px 10px;border-bottom:1px solid #d7d3c7;background:#f7f5ef}.property-chooser h2{margin:0;font-size:15px}.property-chooser-grid{overflow:auto}.property-chooser-grid table{width:100%;border-collapse:collapse;white-space:nowrap}.property-chooser-grid th,.property-chooser-grid td{padding:5px 7px;border:1px solid #d4d1c8;text-align:left}.property-chooser-grid th{background:#f0b023}.property-chooser-grid tbody tr{cursor:pointer}.property-chooser-grid tbody tr:hover{background:#fff1bd}.property-value-options{display:flex;flex-wrap:wrap;gap:6px;padding:12px}.property-dev-note{font-size:11px}
@media(max-width:560px){.property-status{gap:4px;overflow:auto}.property-body{grid-template-columns:minmax(0,1fr) 72px;gap:5px}.property-actions .prd-button{white-space:normal;padding:3px}.property-editor-fields .property-version-label{top:49px;left:7px;right:auto}.property-editor-fields .property-no-label{top:77px}.property-editor-fields .property-value-label{top:105px}.property-editor-fields .property-sequence-label{top:105px}.property-editor-fields .property-description-label{top:135px}.property-editor-fields{min-height:175px}}
</style>
