<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type ActionCode = 0 | 1 | 2 | 3
type BucketKey = 'warning' | 'wait' | 'hold' | 'alert'
interface VariableItem { qcItemNo: string; qcItemName: string; chartType: number; issueState: number }
interface RuleBasis { ruleNo: string; ruleName: string; description: string; issueState: number; weType?: number }
interface ItemRule { qcItemNo: string; spcId: number; ruleNo: string; action: ActionCode; ruleName?: string }
interface Payload<T> { items?: T[]; rows?: T[]; data?: T[] }
interface MockState { mappings: ItemRule[] }
const props = withDefaults(defineProps<{ apiBaseUrl: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const itemApi = 'api/qc/variable-qc-items'
const mappingApi = 'api/qc/variable-item-rules'
const mockKey = 'ymes-dev-mock:qc-variable-item-rule-map:v1'
const actions: Array<{ code: ActionCode; key: BucketKey; label: string }> = [
  { code: 0, key: 'warning', label: 'Warning' }, { code: 1, key: 'wait', label: 'Wait' },
  { code: 2, key: 'hold', label: 'Hold' }, { code: 3, key: 'alert', label: 'Alert' },
]
const spcLabels: Record<number, string> = { 0: 'Trend Chart', 1: 'Xbar Chart', 2: 'R Chart', 3: 'S Chart', 4: 'Rm Chart' }
const demoItems: VariableItem[] = [
  { qcItemNo: 'DEV-QCV-001', qcItemName: '厚度量測（DEMO）', chartType: 0, issueState: 2 },
  { qcItemNo: 'DEV-QCV-002', qcItemName: '晶圓顆粒尺寸（DEMO）', chartType: 1, issueState: 2 },
  { qcItemNo: 'DEV-QCV-003', qcItemName: '外觀尺寸檢驗（DEMO）', chartType: 2, issueState: 2 },
]
const demoRules: RuleBasis[] = [
  { ruleNo: 'DEV-VAR-001', ruleName: 'Value 趨勢檢查', description: '連續樣本依設定值判定', issueState: 2, weType: 0 },
  { ruleNo: 'DEV-VAR-003', ruleName: '自訂趨勢函數', description: '使用自訂函數', issueState: 2, weType: 0 },
  { ruleNo: 'DEV-VAR-006', ruleName: '尺寸偏移告警', description: '連續點數超出上下界', issueState: 2, weType: 0 },
  { ruleNo: 'DEV-VAR-008', ruleName: '量測值穩定性', description: '超出控制界線時等待覆核', issueState: 2, weType: 0 },
]
const demoMappings: ItemRule[] = [
  { qcItemNo: 'DEV-QCV-001', spcId: 0, ruleNo: 'DEV-VAR-001', action: 0, ruleName: 'Value 趨勢檢查' },
  { qcItemNo: 'DEV-QCV-001', spcId: 1, ruleNo: 'DEV-VAR-003', action: 2, ruleName: '自訂趨勢函數' },
  { qcItemNo: 'DEV-QCV-002', spcId: 0, ruleNo: 'DEV-VAR-008', action: 1, ruleName: '量測值穩定性' },
  { qcItemNo: 'DEV-QCV-003', spcId: 4, ruleNo: 'DEV-VAR-006', action: 3, ruleName: '尺寸偏移告警' },
]
const items = ref<VariableItem[]>([])
const rules = ref<RuleBasis[]>([])
const mappings = ref<ItemRule[]>([])
const activeItemNo = ref('')
const lookupText = ref('')
const lookupOpen = ref(false)
const selectedLookupNo = ref('')
const editorOpen = ref(false)
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')
const waitEnabled = ref(true)
const currentSpcId = ref<number | null>(null)
const loadedSpcId = ref<number | null>(null)
const workingRules = ref<ItemRule[]>([])
const selectedAvailable = ref<string[]>([])
const selectedAssigned = ref<Record<BucketKey, string[]>>(emptySelection())
const availableFocus = ref('')
const ruleDescription = ref('')
const dirty = ref(false)
const activeItem = computed(() => items.value.find(row => row.qcItemNo === activeItemNo.value) ?? null)
const filteredItems = computed(() => items.value.filter(row => row.qcItemNo.toLowerCase().includes(lookupText.value.trim().toLowerCase()) || row.qcItemName.toLowerCase().includes(lookupText.value.trim().toLowerCase())).sort((a, b) => a.qcItemNo.localeCompare(b.qcItemNo)))
const selectedLookupItem = computed(() => filteredItems.value.find(row => row.qcItemNo === selectedLookupNo.value) ?? null)
const chartOptions = computed(() => {
  const type = activeItem.value?.chartType
  if (type === 0) return [0, 1, 2]
  if (type === 1) return [0, 1, 3]
  if (type === 2) return [0, 4]
  return []
})
const visibleMappings = computed(() => mappings.value.filter(row => row.qcItemNo === activeItemNo.value)
  .map(row => ({ ...row, ruleName: row.ruleName || rules.value.find(rule => rule.ruleNo === row.ruleNo)?.ruleName || '' }))
  .sort((a, b) => a.spcId - b.spcId || a.action - b.action || a.ruleNo.localeCompare(b.ruleNo)))
const assignedNos = computed(() => new Set(workingRules.value.map(row => row.ruleNo)))
const availableRules = computed(() => rules.value.filter(row => !assignedNos.value.has(row.ruleNo)).sort((a, b) => a.ruleNo.localeCompare(b.ruleNo)))
const activeAvailableRule = computed(() => rules.value.find(row => row.ruleNo === availableFocus.value) ?? null)
function emptySelection(): Record<BucketKey, string[]> { return { warning: [], wait: [], hold: [], alert: [] } }
function toList<T>(payload: T[] | Payload<T>): T[] { return Array.isArray(payload) ? payload : payload.items ?? payload.rows ?? payload.data ?? [] }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (!response.ok) throw new Error((await response.text()) || ('API 回應 ' + response.status))
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMock(): MockState {
  try {
    const raw = localStorage.getItem(mockKey)
    if (raw) { const value = JSON.parse(raw) as MockState; if (Array.isArray(value.mappings)) return value }
  } catch { /* reset isolated DEV preview */ }
  const value = { mappings: structuredClone(demoMappings) }
  localStorage.setItem(mockKey, JSON.stringify(value))
  return value
}
function writeMock(value: MockState): void { localStorage.setItem(mockKey, JSON.stringify(value)) }
async function loadLookups(): Promise<void> {
  loading.value = true; errorMessage.value = ''
  try {
    if (mock.value) { items.value = structuredClone(demoItems); rules.value = structuredClone(demoRules); waitEnabled.value = true; return }
    const [itemPayload, rulePayload, options] = await Promise.all([
      request<VariableItem[] | Payload<VariableItem>>(itemApi + '?issueState=2'),
      request<RuleBasis[] | Payload<RuleBasis>>('api/qc/rule-bases'),
      request<{ waitEnabled?: boolean }>(mappingApi + '/options'),
    ])
    items.value = toList(itemPayload).filter(row => row.issueState === 2)
    rules.value = toList(rulePayload).filter(row => row.issueState === 2)
    waitEnabled.value = options.waitEnabled ?? true
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : String(error) }
  finally { loading.value = false }
}
async function loadMappings(): Promise<void> {
  if (!activeItem.value) { mappings.value = []; return }
  loading.value = true; errorMessage.value = ''
  try {
    if (mock.value) mappings.value = structuredClone(readMock().mappings).filter(row => row.qcItemNo === activeItemNo.value)
    else mappings.value = toList(await request<ItemRule[] | Payload<ItemRule>>(mappingApi + '/' + encodeURIComponent(activeItemNo.value)))
    notice.value = ''
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : String(error) }
  finally { loading.value = false }
}
function openLookup(): void { lookupText.value = ''; selectedLookupNo.value = activeItemNo.value; lookupOpen.value = true }
async function chooseItem(item: VariableItem): Promise<void> { activeItemNo.value = item.qcItemNo; lookupOpen.value = false; await loadMappings() }
function chooseSelectedItem(): void { if (selectedLookupItem.value) void chooseItem(selectedLookupItem.value) }
function actionLabel(code: number): string { return actions.find(item => item.code === code)?.label ?? String(code) }
function ruleText(rule: RuleBasis | ItemRule): string { const name = rule.ruleName?.trim(); return name ? rule.ruleNo + ' (' + name + ')' : rule.ruleNo }
function rowsForBucket(action: ActionCode): ItemRule[] { return workingRules.value.filter(row => row.action === action).sort((a, b) => a.ruleNo.localeCompare(b.ruleNo)) }
function descForBucket(key: BucketKey): string {
  const selected = selectedAssigned.value[key]?.[0]
  return selected ? rules.value.find(row => row.ruleNo === selected)?.description ?? '' : ''
}
function selectAvailable(ruleNo: string, checked: boolean): void {
  selectedAvailable.value = checked ? [...new Set([...selectedAvailable.value, ruleNo])] : selectedAvailable.value.filter(value => value !== ruleNo)
  availableFocus.value = ruleNo
  ruleDescription.value = rules.value.find(row => row.ruleNo === ruleNo)?.description ?? ''
}
function selectAssigned(key: BucketKey, ruleNo: string, checked: boolean): void {
  selectedAssigned.value[key] = checked ? [...new Set([...selectedAssigned.value[key], ruleNo])] : selectedAssigned.value[key].filter(value => value !== ruleNo)
  ruleDescription.value = rules.value.find(row => row.ruleNo === ruleNo)?.description ?? ''
}
function moveToBucket(action: typeof actions[number]): void {
  const picked = selectedAvailable.value
  if (!picked.length || (action.key === 'wait' && !waitEnabled.value)) return
  for (const ruleNo of picked) workingRules.value.push({ qcItemNo: activeItemNo.value, spcId: loadedSpcId.value ?? 0, ruleNo, action: action.code, ruleName: rules.value.find(row => row.ruleNo === ruleNo)?.ruleName ?? '' })
  selectedAvailable.value = []; availableFocus.value = ''; ruleDescription.value = ''; dirty.value = true
}
function moveToAvailable(action: typeof actions[number]): void {
  const picked = selectedAssigned.value[action.key]
  if (!picked.length || (action.key === 'wait' && !waitEnabled.value)) return
  workingRules.value = workingRules.value.filter(row => !(row.action === action.code && picked.includes(row.ruleNo)))
  selectedAssigned.value[action.key] = []; dirty.value = true
}
async function selectChart(): Promise<void> {
  const next = currentSpcId.value
  if (next === null || next === loadedSpcId.value) return
  if (dirty.value && !window.confirm('目前 SPCID 有尚未儲存的變更，切換後將放棄這些變更。要繼續嗎？')) { currentSpcId.value = loadedSpcId.value; return }
  loadedSpcId.value = next
  workingRules.value = mappings.value.filter(row => row.qcItemNo === activeItemNo.value && row.spcId === next).map(row => ({ ...row }))
  selectedAvailable.value = []; selectedAssigned.value = emptySelection(); availableFocus.value = ''; ruleDescription.value = ''; dirty.value = false
}
function openAssignment(): void {
  if (!activeItem.value) { errorMessage.value = '請先查詢並選取一筆計量品管項目。'; return }
  if (!chartOptions.value.length) { errorMessage.value = '此 QC Item 沒有可用的 ChartType，無法設定 SPCID。'; return }
  errorMessage.value = ''; notice.value = ''
  loadedSpcId.value = chartOptions.value[0]; currentSpcId.value = chartOptions.value[0]
  workingRules.value = mappings.value.filter(row => row.qcItemNo === activeItemNo.value && row.spcId === loadedSpcId.value).map(row => ({ ...row }))
  selectedAvailable.value = []; selectedAssigned.value = emptySelection(); availableFocus.value = ''; ruleDescription.value = ''; dirty.value = false; editorOpen.value = true
}
async function saveAssignment(closeAfter: boolean): Promise<void> {
  if (!activeItem.value || loadedSpcId.value === null) return
  busy.value = true; errorMessage.value = ''; notice.value = ''
  const payload = { rules: workingRules.value.map(row => ({ ruleNo: row.ruleNo, action: row.action })) }
  try {
    if (mock.value) {
      const store = readMock()
      store.mappings = store.mappings.filter(row => !(row.qcItemNo === activeItemNo.value && row.spcId === loadedSpcId.value))
      store.mappings.push(...workingRules.value.map(row => ({ ...row })))
      writeMock(store)
    } else await request(mappingApi + '/' + encodeURIComponent(activeItemNo.value) + '/' + loadedSpcId.value, { method: 'PUT', body: JSON.stringify(payload) })
    mappings.value = [...mappings.value.filter(row => !(row.qcItemNo === activeItemNo.value && row.spcId === loadedSpcId.value)), ...workingRules.value.map(row => ({ ...row }))]
    dirty.value = false; selectedAvailable.value = []; selectedAssigned.value = emptySelection()
    notice.value = 'SPCID ' + loadedSpcId.value + ' 管制法則已儲存。'
    if (closeAfter) editorOpen.value = false
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : String(error) }
  finally { busy.value = false }
}
onMounted(() => { void loadLookups() })
</script>

<template>
  <section class="qcr-page">
    <header class="qcr-title"><h1>計量品管項目管制法則對應</h1><span v-if="mock" class="qcr-dev">DEV 假資料</span></header>
    <div class="qcr-query">
      <label for="qcr-item-no">QCItem</label><input id="qcr-item-no" class="qcr-input qcr-item-no" :value="activeItem?.qcItemNo ?? ''" readonly>
      <input class="qcr-input qcr-item-name" :value="activeItem?.qcItemName ?? ''" readonly aria-label="QCItemName">
      <button type="button" class="qcr-btn" :disabled="loading" @click="openLookup">...</button>
      <span class="qcr-query-spacer"></span><span v-if="loading" class="qcr-muted">讀取中…</span>
    </div>
    <div v-if="errorMessage" class="qcr-message qcr-error" role="alert">{{ errorMessage }}</div>
    <div v-else-if="notice" class="qcr-message" role="status">{{ notice }}</div>
    <div class="qcr-main">
      <div class="qcr-grid-box"><div class="qcr-grid-scroll"><table class="qcr-grid"><thead><tr><th>SPCID</th><th>RuleNo</th><th>Action</th><th>RuleName</th></tr></thead><tbody>
        <tr v-for="(row,index) in visibleMappings" :key="row.spcId + ':' + row.ruleNo + ':' + index"><td>{{ spcLabels[row.spcId] ?? row.spcId }}</td><td>{{ row.ruleNo }}</td><td>{{ actionLabel(row.action) }}</td><td>{{ row.ruleName }}</td></tr>
        <tr v-if="!visibleMappings.length"><td colspan="4" class="qcr-empty">{{ activeItem ? '此 QC Item 尚未設定管制法則' : '請先使用 ... 選取 QC Item' }}</td></tr>
      </tbody></table></div><footer class="qcr-grid-footer">資料總筆數 {{ visibleMappings.length }}</footer></div>
      <aside class="qcr-actions"><button type="button" class="qcr-btn qcr-primary" :disabled="loading || !activeItem" @click="openAssignment">SET (&amp;S)</button><button type="button" class="qcr-btn qcr-close" @click="emit('close')">Close (&amp;X)</button></aside>
    </div>

    <div v-if="lookupOpen" class="qcr-overlay" @click.self="lookupOpen=false"><section class="qcr-lookup" role="dialog" aria-modal="true">
      <header class="qcr-dialog-title"><h2>計量品管項目查詢</h2><button type="button" class="qcr-x" @click="lookupOpen=false">×</button></header>
      <div class="qcr-lookup-search"><label for="qcr-search">QCItemNo / QCItemName</label><input id="qcr-search" v-model="lookupText" class="qcr-input" @input="selectedLookupNo=''"> <button type="button" class="qcr-btn" @click="lookupText=lookupText.trim(); selectedLookupNo=''">查詢</button></div>
      <div class="qcr-grid-scroll"><table class="qcr-grid"><thead><tr><th>QCItemNo</th><th>QCItemName</th></tr></thead><tbody><tr v-for="row in filteredItems" :key="row.qcItemNo" class="qcr-pick-row" :class="{ selected: selectedLookupNo === row.qcItemNo }" @click="selectedLookupNo=row.qcItemNo" @dblclick="chooseItem(row)"><td>{{ row.qcItemNo }}</td><td>{{ row.qcItemName }}</td></tr><tr v-if="!filteredItems.length"><td colspan="2" class="qcr-empty">查無核准計量品管項目</td></tr></tbody></table></div>
      <footer class="qcr-dialog-actions"><button type="button" class="qcr-btn" @click="lookupOpen=false">Close</button><button type="button" class="qcr-btn qcr-primary" :disabled="!selectedLookupItem" @click="chooseSelectedItem">選取</button></footer>
    </section></div>

    <div v-if="editorOpen" class="qcr-overlay" @click.self="editorOpen=false"><section class="qcr-editor" role="dialog" aria-modal="true">
      <header class="qcr-dialog-title"><h2>QCItem Rule Define</h2><button type="button" class="qcr-x" :disabled="busy" @click="editorOpen=false">×</button></header>
      <div class="qcr-editor-head"><label>QCItem<input class="qcr-input" :value="activeItem?.qcItemNo" readonly></label><input class="qcr-input" :value="activeItem?.qcItemName" readonly aria-label="QCItemName"><label>SPCID<select v-model.number="currentSpcId" class="qcr-input" :disabled="busy" @change="selectChart"><option v-for="spcId in chartOptions" :key="spcId" :value="spcId">{{ spcLabels[spcId] }}</option></select></label></div>
      <div class="qcr-assignment-layout">
        <section class="qcr-available"><header>Unselected Rule</header><div class="qcr-rule-list"><label v-for="rule in availableRules" :key="rule.ruleNo" class="qcr-rule-option"><input type="checkbox" :checked="selectedAvailable.includes(rule.ruleNo)" @change="selectAvailable(rule.ruleNo,($event.target as HTMLInputElement).checked)"><span>{{ ruleText(rule) }}</span></label><p v-if="!availableRules.length" class="qcr-empty">沒有可加入的已核准法則</p></div><textarea class="qcr-description" :value="activeAvailableRule?.description ?? ruleDescription" readonly aria-label="Rule Description"></textarea></section>
        <template v-for="action in actions" :key="action.key">
          <div class="qcr-move" :style="{ gridRow: actions.findIndex(item => item.key === action.key) + 1 }"><button type="button" class="qcr-btn qcr-arrow" :disabled="busy || !selectedAvailable.length || (action.key==='wait' && !waitEnabled)" :aria-label="'加入 ' + action.label" @click="moveToBucket(action)">→</button><button type="button" class="qcr-btn qcr-arrow" :disabled="busy || !selectedAssigned[action.key].length || (action.key==='wait' && !waitEnabled)" :aria-label="'移出 ' + action.label" @click="moveToAvailable(action)">←</button></div>
          <section class="qcr-bucket" :style="{ gridRow: actions.findIndex(item => item.key === action.key) + 1 }"><header>{{ action.label }}<span v-if="action.key==='wait' && !waitEnabled">（此企業停用）</span></header><div class="qcr-rule-list"><label v-for="rule in rowsForBucket(action.code)" :key="rule.ruleNo" class="qcr-rule-option"><input type="checkbox" :checked="selectedAssigned[action.key].includes(rule.ruleNo)" @change="selectAssigned(action.key,rule.ruleNo,($event.target as HTMLInputElement).checked)"><span>{{ ruleText(rule) }}</span></label><p v-if="!rowsForBucket(action.code).length" class="qcr-empty">無</p></div><textarea class="qcr-description" :value="descForBucket(action.key)" readonly :aria-label="action.label + ' Rule Description'"></textarea></section>
        </template>
      </div>
      <p class="qcr-edit-hint">已核准的計量品管法則依 SPCID 分類；多選後使用箭頭移動。{{ dirty ? '有尚未儲存的變更。' : '' }}</p>
      <footer class="qcr-dialog-actions"><button type="button" class="qcr-btn qcr-primary" :disabled="busy" @click="saveAssignment(true)">Confirm</button><button type="button" class="qcr-btn" :disabled="busy" @click="editorOpen=false">Close</button><button type="button" class="qcr-btn" :disabled="busy || !dirty" @click="saveAssignment(false)">Apply</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.qcr-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#f7f6f1;color:#292929;font:13px "Segoe UI",Arial,sans-serif}.qcr-title{display:flex;align-items:center;gap:10px;padding:10px 16px;border-bottom:1px solid #d7d5cf;background:#faf9f6}.qcr-title h1{font-size:20px;font-weight:600;margin:0}.qcr-dev{font-size:11px;color:#765600;background:#fff0b3;border:1px solid #e8c85e;padding:3px 7px;border-radius:3px}
.qcr-query{display:flex;align-items:center;gap:6px;padding:7px 12px;min-height:42px;background:#fff;border-bottom:1px solid #d2d0ca}.qcr-query-spacer{flex:1}.qcr-item-no{width:150px}.qcr-item-name{width:min(340px,38vw)}.qcr-input{min-height:28px;box-sizing:border-box;padding:4px 7px;border:1px solid #999;background:#fff0ad;color:#292929;font:inherit}.qcr-input:focus{outline:1px solid #bd8a33}.qcr-input[readonly]{background:#ededeb}.qcr-btn{min-height:30px;padding:4px 10px;border:1px solid #bdbab4;border-radius:3px;background:linear-gradient(#fff,#f1f0ed);color:#252525;font:inherit;cursor:pointer;white-space:nowrap}.qcr-btn:hover:not(:disabled){background:#fff5d6;border-color:#c68a2b}.qcr-btn:disabled{opacity:.5;cursor:default}.qcr-primary{background:#ffae32;border-color:#e6991e;color:#fff;font-weight:600}.qcr-message{padding:6px 12px;background:#f0f6eb;border-bottom:1px solid #d2d0ca;color:#47633a}.qcr-error{background:#fff1ec;color:#9c2e19}.qcr-muted{color:#777}
.qcr-main{display:flex;gap:8px;padding:8px;flex:1;min-height:0}.qcr-grid-box{display:flex;flex-direction:column;flex:1;min-width:0;border:1px solid #c8c5be;background:#fff}.qcr-grid-scroll{overflow:auto;flex:1;min-height:0}.qcr-grid{width:100%;border-collapse:collapse;background:#fff}.qcr-grid th,.qcr-grid td{padding:5px 8px;border:1px solid #c8c8c8;text-align:left;white-space:nowrap;height:25px}.qcr-grid th{background:#e9e7e2;color:#303030;position:sticky;top:0;z-index:1}.qcr-grid tbody tr:hover{background:#fff8df}.qcr-grid-footer{padding:5px 8px;border-top:1px solid #d5d2ca;background:#faf9f6;color:#666}.qcr-empty{text-align:center!important;color:#888;padding:12px!important}.qcr-actions{width:110px;display:flex;flex-direction:column;gap:7px;flex:none}.qcr-actions .qcr-btn{height:34px}.qcr-close{margin-top:auto}
.qcr-overlay{position:fixed;inset:0;z-index:5000;background:#0006;display:flex;align-items:center;justify-content:center;padding:16px}.qcr-lookup,.qcr-editor{display:flex;flex-direction:column;background:#f7f6f1;border:1px solid #9d9a93;box-shadow:0 8px 28px #0004;max-height:94vh}.qcr-lookup{width:min(760px,96vw);height:min(560px,90vh)}.qcr-editor{width:min(900px,98vw);height:min(700px,94vh)}.qcr-dialog-title{display:flex;align-items:center;justify-content:space-between;padding:9px 14px;border-bottom:1px solid #cbc8c0;background:#fbfaf7;flex:none}.qcr-dialog-title h2{font-size:16px;margin:0;font-weight:600}.qcr-x{border:0;background:transparent;font-size:24px;color:#555;cursor:pointer}.qcr-lookup-search{display:flex;align-items:center;gap:8px;padding:9px 12px}.qcr-lookup-search .qcr-input{flex:1}.qcr-pick-row{cursor:pointer}.qcr-pick-row.selected{background:#ffe6a3}
.qcr-editor-head{display:grid;grid-template-columns:200px minmax(180px,1fr) 230px;align-items:center;gap:8px;padding:10px 14px}.qcr-editor-head label{display:flex;align-items:center;gap:7px;white-space:nowrap}.qcr-editor-head .qcr-input{min-width:0}.qcr-assignment-layout{display:grid;grid-template-columns:minmax(230px,1fr) 58px minmax(280px,1fr);grid-template-rows:repeat(4,minmax(110px,1fr));gap:7px;padding:4px 12px;flex:1;min-height:0}.qcr-available{grid-column:1;grid-row:1/5;display:flex;flex-direction:column;min-height:0;border:1px solid #c4c1ba;background:#fff}.qcr-available>header,.qcr-bucket>header{padding:4px 7px;background:#f0efec;border-bottom:1px solid #c8c5be;font-weight:600}.qcr-available .qcr-rule-list{flex:1}.qcr-rule-list{overflow:auto;min-height:0;background:#fff}.qcr-rule-option{display:flex;align-items:flex-start;gap:6px;padding:4px 6px;line-height:1.35;border-bottom:1px solid #eee;cursor:pointer}.qcr-rule-option:hover{background:#fff6d8}.qcr-rule-option input{accent-color:#e79320;margin-top:2px}.qcr-rule-option span{overflow-wrap:anywhere}.qcr-description{box-sizing:border-box;resize:none;width:100%;min-height:34px;border:1px solid #c8c8c8;background:#faf9f6;color:#555;padding:4px 6px;font:12px "Segoe UI",Arial,sans-serif}.qcr-move{grid-column:2;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px}.qcr-arrow{width:48px;height:30px;font-size:18px;padding:0}.qcr-bucket{grid-column:3;display:flex;flex-direction:column;min-height:0;border:1px solid #c4c1ba;background:#fff}.qcr-bucket .qcr-rule-list{flex:1}.qcr-bucket>header span{font-weight:400;color:#8a5d00}.qcr-edit-hint{padding:3px 14px;margin:0;color:#665d4b;min-height:20px}.qcr-dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:9px 12px;border-top:1px solid #cbc8c0;background:#faf9f6;flex:none}
@media(max-width:760px){.qcr-query{flex-wrap:wrap}.qcr-item-name{flex:1}.qcr-editor{height:96vh}.qcr-editor-head{grid-template-columns:1fr}.qcr-assignment-layout{grid-template-columns:minmax(160px,1fr) 48px minmax(180px,1fr);grid-template-rows:repeat(4,minmax(100px,1fr));padding:4px}.qcr-available>header,.qcr-bucket>header{font-size:12px}.qcr-rule-option{font-size:11px;padding:3px}.qcr-arrow{width:42px}}
</style>
