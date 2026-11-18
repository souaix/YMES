<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Row = Record<string, any>
type OperationOption = { opNo: string; opName: string }

const props = defineProps<{
  product: Row
  subOps: Row[] | null
  operationOptions: Row[]
  developmentPreview: boolean
  currentUserNo?: string
}>()
const emit = defineEmits<{ save: [rows: Row[]]; close: [] }>()
const browserWindow = globalThis.window

const working = ref<Row[]>([])
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const mode = ref<'add' | 'insert' | 'edit' | ''>('')
const draft = ref<Row>(emptyDraft())
const lookupOpen = ref(false)
const lookupQuery = ref('')
const error = ref('')
const notice = ref('')
const readOnly = computed(() => !props.developmentPreview)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))

function emptyDraft(): Row {
  return { opNo: '', subOpSequence: '', subOpNo: '', subOpName: '', creator: '', createDate: '', reviser: '', reviseDate: '', dataStamp: 0 }
}
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function keyOf(row: Row): string { return String(row.opNo ?? '') + '|' + String(row.subOpSequence ?? '') }
function normalize(row: Row): Row {
  return {
    ...row,
    productNo: String(row.productNo ?? row.ProductNo ?? productNo.value),
    productVersion: String(row.productVersion ?? row.ProductVersion ?? productVersion.value),
    opNo: String(row.opNo ?? row.OPNo ?? ''),
    subOpSequence: Number(row.subOpSequence ?? row.SubOPSequence ?? 0),
    subOpNo: String(row.subOpNo ?? row.SubOPNo ?? ''),
    subOpName: String(row.subOpName ?? row.SubOPName ?? ''),
    creator: String(row.creator ?? row.Creator ?? ''),
    createDate: row.createDate ?? row.CreateDate ?? '',
    dataStamp: row.dataStamp ?? row.DataStamp ?? 0,
    reviser: String(row.reviser ?? row.Reviser ?? ''),
    reviseDate: row.reviseDate ?? row.ReviseDate ?? '',
  }
}
const rows = computed(() => [...working.value].sort((a, b) =>
  String(a.opNo).localeCompare(String(b.opNo)) || Number(a.subOpSequence) - Number(b.subOpSequence)))
const activeRow = computed(() => working.value.find(row => keyOf(row) === activeKey.value) ?? null)
const canConfirm = computed(() => Boolean(!readOnly.value && mode.value && draft.value.opNo && draft.value.subOpNo?.trim() && draft.value.subOpName?.trim()))
const operations = computed<OperationOption[]>(() => {
  const map = new Map<string, OperationOption>()
  for (const row of props.operationOptions ?? []) {
    const opNo = String(row.opNo ?? row.OPNo ?? '')
    if (opNo) map.set(opNo, { opNo, opName: String(row.opName ?? row.OPName ?? '') })
  }
  for (const row of working.value) {
    if (row.opNo && !map.has(String(row.opNo))) map.set(String(row.opNo), { opNo: String(row.opNo), opName: String(row.opName ?? '') })
  }
  return [...map.values()].sort((a, b) => a.opNo.localeCompare(b.opNo))
})
const matchingOperations = computed(() => {
  const query = lookupQuery.value.trim().toLocaleLowerCase()
  return operations.value.filter(row => !query || row.opNo.toLocaleLowerCase().includes(query) || row.opName.toLocaleLowerCase().includes(query))
})

watch(() => props.subOps, source => {
  working.value = (Array.isArray(source) ? source : []).map(normalize)
  if (!working.value.some(row => keyOf(row) === activeKey.value)) activeKey.value = rows.value[0] ? keyOf(rows.value[0]) : ''
  selectedKeys.value = selectedKeys.value.filter(key => working.value.some(row => keyOf(row) === key))
  if (!mode.value) loadDraft(activeRow.value)
}, { immediate: true, deep: true })

function currentUser(): string { return props.currentUserNo || 'DEV001' }
function now(): string { return new Date().toISOString() }
function dateText(value: unknown): string {
  if (!value) return ''
  const date = new Date(String(value))
  if (Number.isNaN(date.getTime())) return String(value).replace('T', ' ').replaceAll('-', '/').slice(0, 19)
  const pad = (v: number) => String(v).padStart(2, '0')
  return date.getFullYear() + '/' + pad(date.getMonth() + 1) + '/' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds())
}
function loadDraft(row: Row | null): void { draft.value = row ? clone(row) : emptyDraft() }
function selectRow(row: Row, event: MouseEvent): void {
  const key = keyOf(row)
  activeKey.value = key
  selectedKeys.value = event.ctrlKey || event.metaKey
    ? selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
    : [key]
  if (!mode.value) loadDraft(row)
  error.value = ''
}
function nextSequence(opNo: string): number {
  return working.value.filter(row => String(row.opNo) === opNo)
    .reduce((max, row) => Math.max(max, Number(row.subOpSequence) || 0), 0) + 1
}
function beginAdd(): void {
  if (readOnly.value) return
  const opNo = String(activeRow.value?.opNo ?? '')
  mode.value = 'add'
  error.value = ''
  notice.value = ''
  draft.value = { ...emptyDraft(), opNo, subOpSequence: opNo ? nextSequence(opNo) : '', creator: currentUser(), createDate: now() }
}
function beginInsert(): void {
  if (readOnly.value || !activeRow.value) return
  mode.value = 'insert'
  error.value = ''
  notice.value = ''
  draft.value = { ...emptyDraft(), opNo: activeRow.value.opNo, subOpSequence: Number(activeRow.value.subOpSequence), creator: currentUser(), createDate: now() }
}
function beginEdit(): void {
  if (readOnly.value || !activeRow.value) return
  mode.value = 'edit'
  error.value = ''
  notice.value = ''
  draft.value = { ...clone(activeRow.value), reviser: currentUser(), reviseDate: now() }
}
function cancelEdit(): void {
  mode.value = ''
  error.value = ''
  loadDraft(activeRow.value)
}
function openLookup(): void {
  if (mode.value !== 'add' || readOnly.value) return
  lookupQuery.value = ''
  lookupOpen.value = true
}
function chooseOperation(row: OperationOption): void {
  draft.value.opNo = row.opNo
  draft.value.opName = row.opName
  draft.value.subOpSequence = nextSequence(row.opNo)
  lookupOpen.value = false
}
function searchEnter(): void {
  const exact = matchingOperations.value.find(row => row.opNo.toLowerCase() === lookupQuery.value.trim().toLowerCase())
  if (exact) chooseOperation(exact)
  else if (matchingOperations.value.length === 1) chooseOperation(matchingOperations.value[0])
}
function deleteSelected(): void {
  if (readOnly.value || !activeRow.value) return
  const targets = selectedKeys.value.length ? selectedKeys.value : [activeKey.value]
  if (!targets.length || !browserWindow.confirm('確定刪除已選取的子工序資料？')) return
  const removed = new Set(targets)
  const kept = working.value.filter(row => !removed.has(keyOf(row)))
  const groups = new Map<string, Row[]>()
  for (const row of kept) groups.set(String(row.opNo), [...(groups.get(String(row.opNo)) ?? []), row])
  for (const group of groups.values()) {
    group.sort((a, b) => Number(a.subOpSequence) - Number(b.subOpSequence))
    group.forEach((row, index) => { row.subOpSequence = index + 1 })
  }
  working.value = kept
  activeKey.value = rows.value[0] ? keyOf(rows.value[0]) : ''
  selectedKeys.value = activeKey.value ? [activeKey.value] : []
  mode.value = ''
  loadDraft(activeRow.value)
  emit('save', clone(working.value))
  notice.value = '已刪除選取資料並依作業站重排序號。'
  error.value = ''
}
function confirmEdit(): void {
  if (!mode.value || readOnly.value) return
  const opNo = String(draft.value.opNo ?? '').trim()
  const subOpNo = String(draft.value.subOpNo ?? '').trim()
  const subOpName = String(draft.value.subOpName ?? '').trim()
  const sequence = Number(draft.value.subOpSequence)
  if (!opNo) { error.value = '請選擇 OP No。'; return }
  if (!subOpNo) { error.value = '請輸入 Sub OP No。'; return }
  if (!subOpName) { error.value = '請輸入 Sub OP Name。'; return }
  if (!Number.isInteger(sequence) || sequence < 1 || sequence > 9999) { error.value = 'Sub OP Sequence 必須介於 1 到 9999。'; return }
  if (mode.value !== 'edit' && working.value.some(row => String(row.opNo) === opNo && Number(row.subOpSequence) === sequence)) {
    error.value = '此作業站的 Sub OP Sequence 已存在。'
    return
  }
  if (mode.value === 'edit') {
    const original = working.value.find(row => keyOf(row) === activeKey.value)
    if (!original) { error.value = '找不到要修改的子工序資料。'; return }
    original.subOpNo = subOpNo
    original.subOpName = subOpName
    original.reviser = currentUser()
    original.reviseDate = now()
    if (original.dataStamp !== null && original.dataStamp !== undefined && original.dataStamp !== '') original.dataStamp = Number(original.dataStamp) + 1
  } else {
    if (mode.value === 'insert') {
      working.value = working.value.map(row => String(row.opNo) === opNo && Number(row.subOpSequence) >= sequence
        ? { ...row, subOpSequence: Number(row.subOpSequence) + 1 }
        : row)
    }
    working.value.push({
      productNo: productNo.value, productVersion: productVersion.value, opNo, subOpSequence: sequence,
      subOpNo, subOpName, creator: currentUser(), createDate: draft.value.createDate || now(),
      reviser: '', reviseDate: '', dataStamp: 0,
    })
  }
  const savedKey = opNo + '|' + sequence
  activeKey.value = savedKey
  selectedKeys.value = [savedKey]
  mode.value = ''
  loadDraft(working.value.find(row => keyOf(row) === savedKey) ?? null)
  emit('save', clone(working.value))
  notice.value = '子工序資料已保存至 DEV 假資料。'
  error.value = ''
}
</script>

<template>
  <section class="prd-subop-window" role="dialog" aria-modal="true" aria-label="PRD OP Sub OP">
    <header class="prd-subop-title">
      <span>PRD OP Sub OP</span><span v-if="developmentPreview" class="dev-badge">DEV</span>
      <button type="button" aria-label="關閉視窗" @click="emit('close')">×</button>
    </header>
    <main class="prd-subop-body">
      <div class="prd-subop-context">
        <label for="subop-product">Product No</label><input id="subop-product" :value="productNo" readonly>
        <label for="subop-version">Product Version</label><input id="subop-version" :value="productVersion" readonly>
        <span v-if="readOnly" class="readonly-note">Production API 尚未接線；正式資料唯讀。</span>
      </div>
      <div class="prd-subop-grid-wrap">
        <table class="prd-subop-grid">
          <thead><tr><th>OP No</th><th>Sub OP Sequence</th><th>Sub OP No</th><th>Sub OP Name</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="keyOf(row)" :class="{ active: activeKey === keyOf(row), selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow(row, $event)">
              <td>{{ row.opNo }}</td><td class="number">{{ row.subOpSequence }}</td><td>{{ row.subOpNo }}</td><td>{{ row.subOpName }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!rows.length"><td colspan="8" class="empty">此產品版本尚未設定子工序。</td></tr>
          </tbody>
        </table>
      </div>
      <aside class="prd-subop-actions">
        <button class="button primary" type="button" :disabled="readOnly" @click="beginAdd">ADD (&amp;A)</button>
        <button class="button" type="button" :disabled="readOnly || !activeRow" @click="beginInsert">INSERT (&amp;I)</button>
        <button class="button" type="button" :disabled="readOnly || !activeRow" @click="beginEdit">EDIT (&amp;E)</button>
        <button class="button" type="button" :disabled="readOnly || !activeRow" @click="deleteSelected">DEL (&amp;D)</button>
      </aside>

      <fieldset class="prd-subop-detail">
        <legend>Operation Sub OP</legend>
        <div class="fields">
          <label for="subop-opno">OP No</label><input id="subop-opno" class="readonly" :value="draft.opNo" readonly>
          <button class="lookup" type="button" :disabled="readOnly || mode !== 'add'" @click="openLookup">...</button>
          <label for="subop-seq">Sub OP Sequence</label><input id="subop-seq" class="readonly" :value="draft.subOpSequence" readonly><span></span>
          <label for="subop-no">Sub OP No</label><input id="subop-no" v-model="draft.subOpNo" class="kd-input" :readonly="!mode || readOnly" maxlength="20"><span></span>
          <label for="subop-name">Sub OP Name</label><input id="subop-name" v-model="draft.subOpName" class="kd-input" :readonly="!mode || readOnly" maxlength="100"><button class="button cancel" type="button" @click="cancelEdit">Cancel</button>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
      </fieldset>

      <fieldset class="prd-subop-audit">
        <legend>Basis Data</legend>
        <div><label>Creator</label><input :value="draft.creator" readonly></div><div><label>Create Date</label><input :value="dateText(draft.createDate)" readonly></div>
        <div><label>Reviser</label><input :value="draft.reviser" readonly></div><div><label>Revise Date</label><input :value="dateText(draft.reviseDate)" readonly></div>
      </fieldset>

      <footer class="prd-subop-footer">
        <span class="count">資料總筆數 {{ rows.length }}<span v-if="selectedKeys.length">　已選取 {{ selectedKeys.length }} 筆</span></span>
        <span v-if="notice" class="notice">{{ notice }}</span>
        <button class="button primary" type="button" :disabled="!canConfirm" @click="confirmEdit">Confirm</button>
        <button class="button" type="button" @click="emit('close')">Close</button>
      </footer>

      <div v-if="lookupOpen" class="lookup-backdrop" @click.self="lookupOpen = false">
        <section class="lookup-dialog" role="dialog" aria-modal="true" aria-label="選擇產品作業站">
          <header><strong>選擇 Product OP</strong><button type="button" aria-label="關閉查找" @click="lookupOpen = false">×</button></header>
          <label class="search">OP No / OP Name<input v-model="lookupQuery" autofocus @keydown.enter.prevent="searchEnter"></label>
          <div class="lookup-grid"><table class="prd-subop-grid">
            <thead><tr><th>OP No</th><th>OP Name</th></tr></thead>
            <tbody>
              <tr v-for="row in matchingOperations" :key="row.opNo" @click="draft.opNo = row.opNo; draft.opName = row.opName" @dblclick="chooseOperation(row)"><td>{{ row.opNo }}</td><td>{{ row.opName }}</td></tr>
              <tr v-if="!matchingOperations.length"><td colspan="2" class="empty">此產品版本沒有可選作業站。</td></tr>
            </tbody>
          </table></div>
          <p v-if="developmentPreview" class="lookup-note">候選資料為 DEV 假資料；正式候選須依登入者 PS 權限取得。</p>
          <footer><button class="button primary" type="button" :disabled="!draft.opNo" @click="chooseOperation({ opNo: String(draft.opNo), opName: String(draft.opName ?? '') })">選取</button><button class="button" type="button" @click="lookupOpen = false">取消</button></footer>
        </section>
      </div>
    </main>
  </section>
</template>

<style scoped>
.prd-subop-window{box-sizing:border-box;width:min(1008px,98vw);height:min(621px,96vh);min-height:591px;background:#fff;border:1px solid #aeb4ba;box-shadow:0 8px 28px #18253635;display:flex;flex-direction:column;color:#20252a;font:13px Arial,"Microsoft JhengHei",sans-serif;overflow:auto}
.prd-subop-title{height:30px;min-height:30px;display:flex;align-items:center;padding:0 9px 0 14px;border-bottom:1px solid #d4d8dc;background:#f6f6f6;font-size:14px;font-weight:600}
.prd-subop-title button,.lookup-dialog header button{margin-left:auto;border:0;background:transparent;font-size:21px;line-height:1;color:#4f555b;cursor:pointer}
.dev-badge{margin-left:10px;padding:2px 7px;border-radius:10px;background:#fff0cf;color:#8c5a00;font-size:10px}
.prd-subop-body{position:relative;flex:1;min-height:591px;padding:0 11px}
.prd-subop-context{position:absolute;left:12px;right:12px;top:7px;height:24px;display:flex;align-items:center;gap:8px}
.prd-subop-context label{width:85px;text-align:right}
.prd-subop-context input{height:22px;width:184px;border:1px solid #aaa;background:#eee;padding:2px 5px}
.prd-subop-context input:last-of-type{width:64px}
.readonly-note{margin-left:12px;color:#735112;font-size:12px}
.prd-subop-grid-wrap{position:absolute;left:14px;top:37px;width:calc(100% - 112px);height:253px;overflow:auto;border:1px solid #b9bec3}
.prd-subop-grid{width:100%;border-collapse:collapse;table-layout:auto;white-space:nowrap}
.prd-subop-grid th{position:sticky;top:0;z-index:1;height:27px;padding:3px 7px;background:#4b9ce3;color:#fff;border:1px solid #d3d8dc;text-align:left;font-weight:500}
.prd-subop-grid td{height:24px;padding:2px 7px;border:1px solid #d3d7da;background:#fff}
.prd-subop-grid tbody tr{cursor:pointer}
.prd-subop-grid tbody tr:hover td{background:#fff8df}
.prd-subop-grid tbody tr.selected td{background:#fff0bd}
.prd-subop-grid tbody tr.active td{box-shadow:inset 0 1px #f4a126,inset 0 -1px #f4a126}
.prd-subop-grid td.number{text-align:right}
.empty{height:50px!important;text-align:center;color:#737b82}
.prd-subop-actions{position:absolute;top:37px;right:11px;width:80px;display:flex;flex-direction:column;gap:6px}
.button{min-width:80px;height:32px;padding:4px 9px;border:1px solid #c5c9cd;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#292d31;cursor:pointer;font:13px Arial,"Microsoft JhengHei",sans-serif}
.button:hover:not(:disabled){border-color:#eea145;background:#fff7e8}
.button:disabled{opacity:.5;cursor:not-allowed}
.button.primary{border-color:#ec9b20;background:#ffa51f;color:#fff;font-weight:600}
.prd-subop-detail{position:absolute;top:296px;left:14px;right:14px;height:163px;margin:0;padding:16px 12px 10px;border:1px solid #b9bec3}
.prd-subop-detail legend,.prd-subop-audit legend{padding:0 5px;color:#555}
.fields{position:relative;display:grid;grid-template-columns:100px 160px 28px;grid-auto-rows:27px;align-items:center;gap:10px 8px}
.fields label{text-align:right}
.fields input{box-sizing:border-box;height:23px;padding:2px 5px;border:1px solid #8d9297;font:13px Arial,"Microsoft JhengHei",sans-serif}
.fields input.readonly{background:#eee}
.fields .kd-input,.fields .kd-input:read-only{background:#fff0b8}
.lookup{width:26px;height:25px;border:1px solid #c5c9cd;border-radius:3px;background:#f8f8f8;cursor:pointer}
.lookup:disabled{opacity:.4;cursor:not-allowed}
.fields .cancel{position:absolute;right:0;bottom:-71px}
.error{position:absolute;left:16px;bottom:3px;margin:0;color:#a32323;font-size:12px}
.prd-subop-audit{position:absolute;left:12px;right:12px;top:467px;height:70px;margin:0;padding:5px 10px;border:1px solid #c1c6ca;display:grid;grid-template-columns:1fr 1fr;grid-template-rows:27px 27px;align-content:center}
.prd-subop-audit div{display:flex;align-items:center;justify-content:flex-end;gap:8px}
.prd-subop-audit label{width:75px;text-align:right}
.prd-subop-audit input{box-sizing:border-box;width:140px;height:22px;padding:2px 5px;border:1px solid #c6c9cc;background:#eee;color:#555}
.prd-subop-footer{position:absolute;left:14px;right:12px;bottom:10px;min-height:32px;display:flex;align-items:center;gap:8px}
.count{margin-right:auto;color:#777}
.notice{max-width:42%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#51634a}
.lookup-backdrop{position:absolute;inset:0;z-index:5;display:grid;place-items:center;background:#151b2280}
.lookup-dialog{box-sizing:border-box;width:min(560px,90%);height:min(430px,80%);padding:12px;background:#fff;border:1px solid #aab0b5;box-shadow:0 5px 22px #0003;display:flex;flex-direction:column;gap:10px}
.lookup-dialog header{display:flex;align-items:center;border-bottom:1px solid #ddd;padding-bottom:7px}
.search{display:flex;align-items:center;gap:10px}
.search input{flex:1;height:27px;padding:3px 6px;border:1px solid #999}
.lookup-grid{flex:1;overflow:auto;border:1px solid #b9bec3}
.lookup-note{margin:0;color:#7a602c;font-size:12px}
.lookup-dialog footer{display:flex;justify-content:flex-end;gap:8px}
@media(max-width:760px){.prd-subop-window{width:98vw}.prd-subop-body{overflow:auto}.prd-subop-grid-wrap{width:calc(100% - 108px)}.prd-subop-detail,.prd-subop-audit{left:8px;right:8px}.prd-subop-audit{grid-template-columns:1fr}.prd-subop-audit div:nth-child(n+3){display:none}.prd-subop-footer{left:8px}.notice{display:none}}
</style>
