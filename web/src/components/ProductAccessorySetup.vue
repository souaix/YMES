<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type AccessoryRow = Record<string, any>
type OperationOption = { opNo: string; opName: string }
type TypeOption = { accessoryType: string; category: string; description: string }

const browserWindow = globalThis.window
const props = defineProps<{
  product: AccessoryRow
  processes: AccessoryRow[]
  accessories: AccessoryRow[] | null
  developmentPreview: boolean
}>()
const emit = defineEmits<{ save: [rows: AccessoryRow[]]; close: [] }>()

const working = ref<AccessoryRow[]>([])
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const definitionOpen = ref(false)
const editMode = ref(false)
const lookupMode = ref<'operation' | 'type' | null>(null)
const draftOpNo = ref('')
const draftOpName = ref('')
const draftType = ref('')
const allSelected = ref(false)
const selectedAccessoryNos = ref<string[]>([])
const rememberedNos = ref<string[]>([])
const dirty = ref(false)
const applied = ref(false)
const error = ref('')
const notice = ref('')
const readOnly = computed(() => !props.developmentPreview)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))

// DEV stand-ins for LoadAccessoryType, LoadAccessoryBasis and PD_ExpandProcess.
const devTypes: TypeOption[] = [
  { accessoryType: 'DEV-ACC-FIXTURE', category: '治具', description: 'DEV 治具類別' },
  { accessoryType: 'DEV-ACC-CONSUMABLE', category: '耗材', description: 'DEV 耗材類別' },
]
const devAccessories = [
  { accessoryType: 'DEV-ACC-FIXTURE', accessoryNo: 'DEV-JIG-001', description: 'DEV 定位治具 001' },
  { accessoryType: 'DEV-ACC-FIXTURE', accessoryNo: 'DEV-JIG-002', description: 'DEV 定位治具 002' },
  { accessoryType: 'DEV-ACC-FIXTURE', accessoryNo: 'DEV-JIG-003', description: 'DEV 定位治具 003' },
  { accessoryType: 'DEV-ACC-CONSUMABLE', accessoryNo: 'DEV-CONS-001', description: 'DEV 消耗配件 001' },
  { accessoryType: 'DEV-ACC-CONSUMABLE', accessoryNo: 'DEV-CONS-002', description: 'DEV 消耗配件 002' },
]
const devOperations: OperationOption[] = [
  { opNo: 'DEV-OP-10', opName: '點膠作業' },
  { opNo: 'DEV-OP-20', opName: '組裝作業' },
]

const summaryRows = computed(() => {
  const groups = new Map<string, AccessoryRow[]>()
  for (const row of working.value) {
    const key = summaryKey(row.opNo, row.accessoryType)
    groups.set(key, [...(groups.get(key) ?? []), row])
  }
  return [...groups.entries()].map(([key, rows]) => ({
    key,
    opNo: String(rows[0].opNo ?? ''),
    opName: String(rows[0].opName ?? ''),
    accessoryType: String(rows[0].accessoryType ?? ''),
    assign: rows.length === 1 && String(rows[0].accessoryNo) === '*' ? 'N' : 'Y',
  })).sort((a, b) => a.opNo.localeCompare(b.opNo) || a.accessoryType.localeCompare(b.accessoryType))
})
const selectedSummaryRows = computed(() => summaryRows.value.filter(row => selectedKeys.value.includes(row.key)))
const activeSummary = computed(() => summaryRows.value.find(row => row.key === activeKey.value) ?? null)
const detailRows = computed(() => activeSummary.value
  ? working.value.filter(row => summaryKey(row.opNo, row.accessoryType) === activeSummary.value?.key)
      .map(row => String(row.accessoryNo ?? '')).sort((a, b) => a.localeCompare(b))
  : [])
const operationOptions = computed<OperationOption[]>(() => {
  const rows = [...devOperations]
  for (const row of working.value) {
    const item = { opNo: String(row.opNo ?? ''), opName: String(row.opName ?? '') }
    if (item.opNo && !rows.some(option => option.opNo === item.opNo)) rows.push(item)
  }
  return rows.sort((a, b) => a.opNo.localeCompare(b.opNo))
})
const typeOptions = computed<TypeOption[]>(() => {
  const rows = [...devTypes]
  for (const row of working.value) {
    const type = String(row.accessoryType ?? '')
    if (type && !rows.some(option => option.accessoryType === type)) rows.push({ accessoryType: type, category: '', description: '' })
  }
  return rows.sort((a, b) => a.accessoryType.localeCompare(b.accessoryType))
})
const accessoryOptions = computed(() => {
  const rows = devAccessories.filter(item => item.accessoryType === draftType.value)
  for (const no of selectedAccessoryNos.value) {
    if (no !== '*' && !rows.some(item => item.accessoryNo === no)) rows.push({ accessoryType: draftType.value, accessoryNo: no, description: '' })
  }
  return rows
})
const canApply = computed(() => !readOnly.value && dirty.value && Boolean(draftOpNo.value && draftType.value))

watch(() => props.accessories, rows => {
  working.value = clone(Array.isArray(rows) ? rows : [])
  if (!summaryRows.value.some(row => row.key === activeKey.value)) activeKey.value = summaryRows.value[0]?.key ?? ''
  selectedKeys.value = selectedKeys.value.filter(key => summaryRows.value.some(row => row.key === key))
}, { immediate: true, deep: true })

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function summaryKey(opNo: unknown, accessoryType: unknown): string { return String(opNo ?? '') + '\u0000' + String(accessoryType ?? '') }
function persist(): void { emit('save', clone(working.value)); notice.value = 'DEV 產品配件假資料已儲存。' }
function selectSummary(row: AccessoryRow, event: MouseEvent): void {
  activeKey.value = row.key
  if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(row.key) ? selectedKeys.value.filter(key => key !== row.key) : [...selectedKeys.value, row.key]
  } else if (event.shiftKey && selectedKeys.value.length) {
    const keys = summaryRows.value.map(item => item.key)
    const from = keys.indexOf(selectedKeys.value[selectedKeys.value.length - 1])
    const to = keys.indexOf(row.key)
    selectedKeys.value = keys.slice(Math.min(from, to), Math.max(from, to) + 1)
  } else selectedKeys.value = [row.key]
}
function openDefinition(row?: AccessoryRow): void {
  if (readOnly.value) return
  error.value = ''; notice.value = ''; editMode.value = Boolean(row)
  draftOpNo.value = row ? String(row.opNo ?? '') : ''
  draftOpName.value = row ? String(row.opName ?? '') : ''
  draftType.value = row ? String(row.accessoryType ?? '') : ''
  const existing = row ? working.value.filter(item => summaryKey(item.opNo, item.accessoryType) === summaryKey(row.opNo, row.accessoryType)) : []
  allSelected.value = existing.length === 1 && String(existing[0].accessoryNo) === '*'
  selectedAccessoryNos.value = allSelected.value ? [] : existing.map(item => String(item.accessoryNo ?? ''))
  rememberedNos.value = [...selectedAccessoryNos.value]
  dirty.value = false; applied.value = false; lookupMode.value = null; definitionOpen.value = true
}
function chooseOperation(item: OperationOption): void {
  draftOpNo.value = item.opNo; draftOpName.value = item.opName; draftType.value = ''
  allSelected.value = false; selectedAccessoryNos.value = []; rememberedNos.value = []
  dirty.value = true; applied.value = false; error.value = ''; lookupMode.value = null
}
function chooseType(item: TypeOption): void {
  draftType.value = item.accessoryType
  const existing = working.value.filter(row => summaryKey(row.opNo, row.accessoryType) === summaryKey(draftOpNo.value, item.accessoryType))
  allSelected.value = existing.length === 1 && String(existing[0].accessoryNo) === '*'
  selectedAccessoryNos.value = allSelected.value ? [] : existing.map(row => String(row.accessoryNo ?? ''))
  rememberedNos.value = [...selectedAccessoryNos.value]
  dirty.value = true; applied.value = false; error.value = ''; lookupMode.value = null
}
function openOperationLookup(): void {
  if (readOnly.value || editMode.value) return
  if (!props.processes.length) { error.value = '此產品版本尚未設定可用流程，無法查找作業站。'; return }
  lookupMode.value = 'operation'
}
function setAll(value: boolean): void {
  if (value && !allSelected.value) rememberedNos.value = [...selectedAccessoryNos.value]
  if (!value && allSelected.value) selectedAccessoryNos.value = [...rememberedNos.value]
  allSelected.value = value; dirty.value = true; applied.value = false
}
function toggleAccessory(no: string): void {
  selectedAccessoryNos.value = selectedAccessoryNos.value.includes(no)
    ? selectedAccessoryNos.value.filter(item => item !== no) : [...selectedAccessoryNos.value, no]
  rememberedNos.value = [...selectedAccessoryNos.value]
  dirty.value = true; applied.value = false
}
function saveDefinition(closeAfter: boolean): boolean {
  if (readOnly.value) return false
  if (!draftOpNo.value || !draftOpName.value) { error.value = '請選擇作業站。'; return false }
  if (!draftType.value) { error.value = '請選擇配件類別。'; return false }
  if (!props.processes.length) { error.value = '此產品版本尚未設定可用流程，無法設定配件。'; return false }
  const retained = working.value.filter(row => summaryKey(row.opNo, row.accessoryType) !== summaryKey(draftOpNo.value, draftType.value))
  const numbers = allSelected.value ? ['*'] : [...new Set(selectedAccessoryNos.value)]
  const added = numbers.map(accessoryNo => ({
    productNo: productNo.value, productVersion: productVersion.value, opNo: draftOpNo.value,
    opName: draftOpName.value, accessoryType: draftType.value, accessoryNo,
  }))
  working.value = [...retained, ...added]
  activeKey.value = summaryKey(draftOpNo.value, draftType.value); selectedKeys.value = [activeKey.value]
  dirty.value = false; applied.value = true; error.value = ''; persist()
  if (closeAfter) definitionOpen.value = false
  return true
}
function confirmDefinition(): void {
  if (applied.value && !dirty.value) { definitionOpen.value = false; return }
  saveDefinition(true)
}
function closeDefinition(): void { definitionOpen.value = false; lookupMode.value = null; error.value = '' }
function deleteSelected(): void {
  if (readOnly.value) return
  const keys = selectedSummaryRows.value.length ? selectedSummaryRows.value.map(row => row.key) : activeKey.value ? [activeKey.value] : []
  if (!keys.length) { error.value = '請先選取作業站／配件類別資料。'; return }
  if (!browserWindow.confirm('確定刪除選取的 ' + keys.length + ' 組產品配件設定？')) return
  working.value = working.value.filter(row => !keys.includes(summaryKey(row.opNo, row.accessoryType)))
  selectedKeys.value = []; activeKey.value = summaryRows.value[0]?.key ?? ''; error.value = ''; persist()
}
</script>

<template>
  <section class="prd-accessory-window" role="dialog" aria-modal="true" aria-label="產品配件設定">
    <header class="prd-accessory-title"><span>產品配件設定</span><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <p v-if="readOnly" class="prd-accessory-message">Production API 尚未接線；正式資料唯讀。</p>
    <p v-if="notice" class="prd-accessory-message">{{ notice }}</p>
    <p v-if="error && !definitionOpen" class="prd-accessory-message is-error">{{ error }}</p>
    <main class="prd-accessory-main">
      <div class="prd-accessory-grids">
        <section class="prd-accessory-grid-block">
          <h3>作業站／配件類別</h3><div class="prd-accessory-grid-wrap"><table class="prd-accessory-grid">
            <thead><tr><th>作業站編號</th><th>作業站名稱</th><th>配件類別</th><th>指定</th></tr></thead>
            <tbody>
              <tr v-for="row in summaryRows" :key="row.key" :class="{ selected: selectedKeys.includes(row.key), active: activeKey === row.key }" @click="selectSummary(row, $event)">
                <td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.accessoryType }}</td><td>{{ row.assign }}</td>
              </tr>
              <tr v-if="!summaryRows.length"><td colspan="4" class="prd-accessory-empty">此產品版本尚未設定配件。</td></tr>
            </tbody>
          </table></div>
        </section>
        <section class="prd-accessory-grid-block">
          <h3>配件明細 <span v-if="activeSummary">{{ activeSummary.opNo }} / {{ activeSummary.accessoryType }}</span></h3>
          <div class="prd-accessory-grid-wrap"><table class="prd-accessory-grid"><thead><tr><th>配件編號</th></tr></thead><tbody>
            <tr v-for="no in detailRows" :key="no"><td>{{ no }}</td></tr>
            <tr v-if="activeSummary && !detailRows.length"><td class="prd-accessory-empty">沒有明細。</td></tr>
            <tr v-if="!activeSummary"><td class="prd-accessory-empty">請先選取上方資料列。</td></tr>
          </tbody></table></div>
        </section>
      </div>
      <aside class="prd-accessory-actions">
        <button class="prd-accessory-button confirm" type="button" :disabled="readOnly" @click="openDefinition()">ADD (&amp;A)</button>
        <button class="prd-accessory-button" type="button" :disabled="readOnly || !activeSummary" @click="openDefinition(activeSummary ?? undefined)">EDIT (&amp;E)</button>
        <button class="prd-accessory-button" type="button" :disabled="readOnly || (!selectedKeys.length && !activeKey)" @click="deleteSelected">DEL (&amp;D)</button>
        <span class="prd-accessory-spacer"></span><button class="prd-accessory-button" type="button" @click="emit('close')">Close (&amp;X)</button>
      </aside>
    </main>
    <div v-if="definitionOpen" class="prd-accessory-form-backdrop">
      <section class="prd-accessory-definition" role="dialog" aria-modal="true" aria-label="產品配件定義">
        <header class="prd-accessory-title"><span>{{ editMode ? '產品配件修改' : '產品配件新增' }}</span><button type="button" aria-label="關閉" @click="closeDefinition">×</button></header>
        <div class="prd-accessory-fields">
          <label for="prd-accessory-op">作業站</label><input id="prd-accessory-op" class="prd-accessory-yellow" :value="draftOpNo" readonly>
          <button class="prd-accessory-lookup" type="button" :disabled="editMode" @click="openOperationLookup">...</button><span class="prd-accessory-op-name">{{ draftOpName }}</span>
          <label for="prd-accessory-type">配件類別</label><input id="prd-accessory-type" class="prd-accessory-yellow" :value="draftType" readonly>
          <button class="prd-accessory-lookup" type="button" :disabled="editMode || !draftOpNo" @click="lookupMode = 'type'">...</button>
        </div>
        <div class="prd-accessory-selection">
          <div class="prd-accessory-radio-row">
            <label><input type="radio" name="prd-accessory-mode" :checked="allSelected" @change="setAll(true)"> 全部配件 (*)</label>
            <label><input type="radio" name="prd-accessory-mode" :checked="!allSelected" @change="setAll(false)"> 指定配件</label>
          </div>
          <div class="prd-accessory-options">
            <label v-for="item in accessoryOptions" :key="item.accessoryNo" class="prd-accessory-option">
              <input type="checkbox" :disabled="allSelected || !draftType" :checked="selectedAccessoryNos.includes(item.accessoryNo)" @change="toggleAccessory(item.accessoryNo)">
              <span>{{ item.accessoryNo }}</span><small>{{ item.description }}</small>
            </label>
            <p v-if="draftType && !accessoryOptions.length" class="prd-accessory-empty">此類別目前沒有可選的配件編號。</p>
            <p v-if="!draftType" class="prd-accessory-empty">請先選擇配件類別。</p>
          </div>
        </div>
        <p v-if="error" class="prd-accessory-message is-error">{{ error }}</p>
        <footer class="prd-accessory-form-actions">
          <button class="prd-accessory-button confirm" type="button" @click="confirmDefinition">Confirm</button>
          <button class="prd-accessory-button" type="button" @click="closeDefinition">Close</button>
          <button class="prd-accessory-button" type="button" :disabled="!canApply" @click="saveDefinition(false)">Apply</button>
        </footer>
        <div v-if="lookupMode" class="prd-accessory-lookup-backdrop" @click.self="lookupMode = null">
          <section class="prd-accessory-lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupMode === 'operation' ? '選擇作業站' : '選擇配件類別'">
            <header><strong>{{ lookupMode === 'operation' ? '選擇產品流程作業站' : '選擇配件類別' }}</strong><button type="button" aria-label="關閉查找" @click="lookupMode = null">×</button></header>
            <table v-if="lookupMode === 'operation'"><thead><tr><th>作業站編號</th><th>作業站名稱</th></tr></thead><tbody>
              <tr v-for="item in operationOptions" :key="item.opNo" @click="chooseOperation(item)"><td>{{ item.opNo }}</td><td>{{ item.opName }}</td></tr>
              <tr v-if="!operationOptions.length"><td colspan="2">沒有可選作業站。</td></tr>
            </tbody></table>
            <table v-else><thead><tr><th>配件類別</th><th>分類</th><th>說明</th></tr></thead><tbody>
              <tr v-for="item in typeOptions" :key="item.accessoryType" @click="chooseType(item)"><td>{{ item.accessoryType }}</td><td>{{ item.category }}</td><td>{{ item.description }}</td></tr>
              <tr v-if="!typeOptions.length"><td colspan="3">沒有可選配件類別。</td></tr>
            </tbody></table>
          </section>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.prd-accessory-window{position:relative;width:min(1067px,98vw);height:min(568px,calc(100vh - 24px));min-height:360px;background:#fff;border:1px solid #aaa79c;box-shadow:0 14px 44px #0004;color:#28271f;font:12px "Segoe UI","Microsoft JhengHei",sans-serif;display:flex;flex-direction:column;overflow:hidden}
.prd-accessory-title{height:30px;min-height:30px;display:flex;align-items:center;justify-content:space-between;padding:0 8px 0 12px;background:#f5f4f0;border-bottom:1px solid #d4d1c8;color:#333}.prd-accessory-title button{border:0;background:transparent;font-size:20px;color:#555;cursor:pointer;width:26px;height:26px}
.prd-accessory-message{margin:5px 12px 0;padding:4px 7px;background:#fff8df;border:1px solid #e4d7a3;color:#605326}.prd-accessory-message.is-error{background:#fff0ed;border-color:#d5a59b;color:#7a261c}
.prd-accessory-main{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:12px;padding:12px;flex:1;min-height:0}.prd-accessory-grids{display:grid;grid-template-rows:1fr 1fr;gap:8px;min-height:0}.prd-accessory-grid-block{display:flex;flex-direction:column;min-height:0}.prd-accessory-grid-block h3{margin:0;padding:4px 7px;background:#f5f4f0;border:1px solid #d4d1c8;font-size:12px;font-weight:600}.prd-accessory-grid-block h3 span{margin-left:10px;color:#777;font-weight:400}.prd-accessory-grid-wrap{min-height:0;overflow:auto;border:1px solid #aaa79c;border-top:0}.prd-accessory-grid{width:100%;border-collapse:collapse;table-layout:fixed}.prd-accessory-grid th{position:sticky;top:0;background:#f1d982;border:1px solid #c6b983;padding:5px 6px;text-align:left;font-weight:600}.prd-accessory-grid td{border:1px solid #d3d0c7;padding:4px 6px;white-space:nowrap}.prd-accessory-grid tbody tr{cursor:pointer}.prd-accessory-grid tbody tr.selected{background:#fff0b0}.prd-accessory-grid tbody tr.active{outline:1px solid #bf982e;outline-offset:-1px}.prd-accessory-empty{text-align:center;color:#777;padding:12px!important}.prd-accessory-actions{display:flex;flex-direction:column;gap:7px}.prd-accessory-spacer{flex:1}.prd-accessory-button{min-height:30px;padding:4px 7px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f1efe9);color:#292820;font:inherit;white-space:nowrap;cursor:pointer}.prd-accessory-button:hover:not(:disabled),.prd-accessory-lookup:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}.prd-accessory-button.confirm{background:#f5a62a;border-color:#dc941d;color:#fff;font-weight:600}.prd-accessory-button:disabled{opacity:.5;cursor:not-allowed}
.prd-accessory-form-backdrop,.prd-accessory-lookup-backdrop{position:absolute;inset:0;background:#0005;display:grid;place-items:center;z-index:2}.prd-accessory-definition{position:relative;width:min(637px,96vw);height:min(453px,calc(100vh - 40px));min-height:340px;background:#fff;border:1px solid #aaa79c;box-shadow:0 12px 36px #0005;display:flex;flex-direction:column}.prd-accessory-fields{display:grid;grid-template-columns:92px minmax(100px,200px) 30px 1fr;align-items:center;gap:7px 5px;padding:14px 12px 8px}.prd-accessory-fields label{text-align:right}.prd-accessory-yellow{height:24px;padding:3px 6px;border:1px solid #888;background:#fff0b0;font:inherit}.prd-accessory-lookup{height:25px;border:1px solid #aaa79c;background:#f8f7f3;cursor:pointer}.prd-accessory-lookup:disabled{opacity:.5;cursor:not-allowed}.prd-accessory-op-name{color:#666;padding-left:5px}
.prd-accessory-selection{margin:2px 25px 0;border:1px solid #aaa79c;display:flex;flex-direction:column;flex:1;min-height:0}.prd-accessory-radio-row{display:flex;gap:20px;align-items:center;padding:7px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8}.prd-accessory-radio-row label{display:flex;align-items:center;gap:5px}.prd-accessory-options{overflow:auto;padding:6px 10px}.prd-accessory-option{display:grid;grid-template-columns:20px 170px 1fr;gap:8px;align-items:center;padding:4px 5px;border-bottom:1px solid #eceae4}.prd-accessory-option small{color:#777}.prd-accessory-form-actions{display:flex;justify-content:flex-end;gap:6px;margin:10px 14px 12px}.prd-accessory-form-actions .prd-accessory-button{height:32px}
.prd-accessory-lookup-backdrop{inset:30px 0 0;z-index:3}.prd-accessory-lookup-dialog{width:min(540px,94vw);max-height:75%;overflow:auto;border:1px solid #aaa79c;background:#fff;box-shadow:0 12px 35px #0005}.prd-accessory-lookup-dialog header{display:flex;justify-content:space-between;align-items:center;padding:7px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8}.prd-accessory-lookup-dialog header button{border:0;background:none;font-size:18px;cursor:pointer}.prd-accessory-lookup-dialog table{width:100%;border-collapse:collapse}.prd-accessory-lookup-dialog th,.prd-accessory-lookup-dialog td{border:1px solid #d3d0c7;padding:6px;text-align:left}.prd-accessory-lookup-dialog th{position:sticky;top:0;background:#f1d982}.prd-accessory-lookup-dialog tbody tr{cursor:pointer}.prd-accessory-lookup-dialog tbody tr:hover{background:#fff0b0}
@media(max-width:700px){.prd-accessory-main{grid-template-columns:minmax(0,1fr);grid-template-rows:minmax(250px,1fr) auto}.prd-accessory-actions{flex-direction:row;flex-wrap:wrap}.prd-accessory-spacer{display:none}.prd-accessory-definition{overflow:auto}}
</style>
