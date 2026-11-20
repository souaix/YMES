<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Component = { componentNo: string; goodQty: number; scrapQty: number; unitNo: string; carrierNo: string }
type Lot = { lotNo: string; lotStamp: number; status: number; lotState: number; opStatus: number; curQty: number; components: Component[] }
type Draft = { componentNo: string; goodQty: number | ''; scrapQty: number | '' }
type SplitRecord = { lotNo: string; fromComponentNo: string; newComponents: { componentNo: string; goodQty: number; scrapQty: number }[]; oldLotStamp: number; newLotStamp: number; savedAt: string }
type MockState = { lots: Lot[]; records: SplitRecord[] }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const unavailableMessage = '元件分片服務尚未提供，暫無法查詢或操作。'
const storageKey = 'ymes-dev-mock:wip-component-split:state:v1'
const initialLots: Lot[] = [
  { lotNo: 'DEV-LOT-215', lotStamp: 21501, status: 1, lotState: 1, opStatus: 1, curQty: 2, components: [
    { componentNo: 'DEV-WAF-001', goodQty: 6, scrapQty: 2, unitNo: 'PCS', carrierNo: '' },
    { componentNo: 'DEV-WAF-002', goodQty: 4, scrapQty: 0, unitNo: 'PCS', carrierNo: '' },
  ] },
  { lotNo: 'DEV-LOT-215-CAR', lotStamp: 21502, status: 1, lotState: 1, opStatus: 0, curQty: 1, components: [
    { componentNo: 'DEV-WAF-C01', goodQty: 3, scrapQty: 0, unitNo: 'PCS', carrierNo: 'DEV-CAR-215' },
  ] },
]

const state = ref<MockState>({ lots: [], records: [] })
const lotInput = ref('')
const lot = ref<Lot | null>(null)
const activeComponentNo = ref('')
const fromComponentNo = ref('')
const count = ref<number | ''>(0)
const draft = ref<Draft[]>([])
const errorText = ref('')
const message = ref('輸入 Lot No 並按 Enter，選取元件後按 Select。')

const canEdit = computed(() => dev.value && lot.value !== null && fromComponentNo.value !== '')
const source = computed(() => lot.value?.components.find(item => item.componentNo === fromComponentNo.value) || null)
const active = computed(() => lot.value?.components.find(item => item.componentNo === activeComponentNo.value) || null)
const draftGood = computed(() => draft.value.reduce((sum, row) => sum + (Number(row.goodQty) || 0), 0))
const draftScrap = computed(() => draft.value.reduce((sum, row) => sum + (Number(row.scrapQty) || 0), 0))

function cloneInitial(): MockState { return { lots: initialLots.map(item => ({ ...item, components: item.components.map(comp => ({ ...comp })) })), records: [] } }
function loadState(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (data && typeof data === 'object' && 'lots' in data && 'records' in data && Array.isArray(data.lots) && Array.isArray(data.records)) {
      state.value = data as MockState; return
    }
  } catch { /* use fixtures */ }
  state.value = cloneInitial()
}
function clearSelection(): void { activeComponentNo.value = ''; fromComponentNo.value = ''; count.value = 0; draft.value = [] }
function invalidateLot(): void {
  if (lot.value && lotInput.value.trim().toUpperCase() !== lot.value.lotNo) {
    lot.value = null; clearSelection(); message.value = 'Lot No 已變更，按 Enter 重新載入。'
  }
}
function queryLot(): void {
  errorText.value = ''; clearSelection(); lot.value = null
  if (!dev.value) { errorText.value = unavailableMessage; return }
  const found = state.value.lots.find(item => item.lotNo === lotInput.value.trim().toUpperCase())
  if (!found) { errorText.value = '找不到 DEV Lot；可輸入 DEV-LOT-215。'; return }
  if (![0, 1, 4, 5].includes(found.status) || found.lotState !== 1 || !found.components.length) { errorText.value = '此 Lot 不符合原表單的有元件、LotState=1、Status 0/1/4/5 條件。'; return }
  lot.value = found; lotInput.value = found.lotNo
  message.value = `已載入 ${found.lotNo}，共 ${found.components.length} 顆元件。`
}
function activate(componentNo: string): void { activeComponentNo.value = componentNo; fromComponentNo.value = ''; count.value = 0; draft.value = []; errorText.value = '' }
function selectComponent(defaultCount = 2): void {
  if (!dev.value || !lot.value || !active.value) { errorText.value = '請先選取上方元件列。'; return }
  fromComponentNo.value = active.value.componentNo; count.value = defaultCount; draft.value = []; errorText.value = ''
  message.value = `已選取 ${active.value.componentNo}；輸入 2–26 並按 SplitWafer 產生新元件列。`
}
function splitWafer(): void {
  errorText.value = ''
  if (!canEdit.value || !lot.value || !source.value) { errorText.value = '請先選取來源元件。'; return }
  if (lot.value.components.some(item => item.carrierNo)) { errorText.value = 'Lot 已有載具現況；原表單要求改用載具分批作業。'; return }
  const n = Number(count.value)
  if (!Number.isInteger(n) || n < 2 || n > 26) { errorText.value = 'Split Comp Count 必須是 2–26 的整數。'; return }
  const base = /[A-Z]$/.test(source.value.componentNo) ? source.value.componentNo.slice(0, -1) : source.value.componentNo
  const used = new Set(state.value.lots.flatMap(item => item.components.map(comp => comp.componentNo)))
  const names: string[] = []
  for (const suffix of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
    const name = base + suffix
    if (!used.has(name)) names.push(name)
    if (names.length === n) break
  }
  if (names.length !== n) { errorText.value = 'DEV 預覽找不到足夠的未用 A–Z 元件尾碼。'; return }
  draft.value = names.map(componentNo => ({ componentNo, goodQty: 0, scrapQty: 0 }))
  message.value = `已預覽 ${n} 筆新元件；請逐列填寫 GoodQty／ScrapQty，合計需與來源相同。`
}
function toScaled(value: number | ''): number | null {
  const n = value === '' ? 0 : Number(value)
  if (!Number.isFinite(n) || n < 0) return null
  const scaled = Math.round(n * 10000)
  return Math.abs(scaled / 10000 - n) < 0.00000001 ? scaled : null
}
function confirmSplit(): void {
  errorText.value = ''
  if (!canEdit.value || !lot.value || !source.value || !draft.value.length) { errorText.value = '請先預覽新元件。'; return }
  const good = draft.value.map(row => toScaled(row.goodQty))
  const scrap = draft.value.map(row => toScaled(row.scrapQty))
  if (good.some(value => value === null) || scrap.some(value => value === null)) { errorText.value = 'GoodQty／ScrapQty 必須是非負數，最多四位小數。'; return }
  if (draft.value.some((_, index) => good[index] === 0 && scrap[index] === 0)) { errorText.value = '每筆新元件的 GoodQty 或 ScrapQty 必須大於 0。'; return }
  if (good.reduce<number>((sum, value) => sum + (value || 0), 0) !== toScaled(source.value.goodQty)) { errorText.value = '新元件 GoodQty 合計須等於來源 GoodQty。'; return }
  if (scrap.reduce<number>((sum, value) => sum + (value || 0), 0) !== toScaled(source.value.scrapQty)) { errorText.value = '新元件 ScrapQty 合計須等於來源 ScrapQty。'; return }
  let current: MockState
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    current = data && typeof data === 'object' && 'lots' in data && 'records' in data && Array.isArray(data.lots) && Array.isArray(data.records) ? data as MockState : cloneInitial()
  } catch { errorText.value = '無法讀取 DEV 模擬狀態。'; return }
  const currentLot = current.lots.find(item => item.lotNo === lot.value?.lotNo)
  const currentSource = currentLot?.components.find(item => item.componentNo === source.value?.componentNo)
  if (!currentLot || !currentSource || currentLot.lotStamp !== lot.value.lotStamp || currentSource.carrierNo) { errorText.value = '來源 Lot／元件狀態已改變，請重新查詢。'; return }
  const used = new Set(current.lots.flatMap(item => item.components.map(comp => comp.componentNo)))
  if (draft.value.some(row => used.has(row.componentNo)) || new Set(draft.value.map(row => row.componentNo)).size !== draft.value.length) { errorText.value = '新元件編號重複，請重新產生預覽。'; return }
  const replacement = draft.value.map((row, index) => ({ componentNo: row.componentNo, goodQty: (good[index] || 0) / 10000, scrapQty: (scrap[index] || 0) / 10000, unitNo: currentSource.unitNo, carrierNo: '' }))
  currentLot.components = currentLot.components.filter(item => item.componentNo !== currentSource.componentNo).concat(replacement)
  currentLot.curQty = currentLot.curQty - 1 + replacement.length
  const oldLotStamp = currentLot.lotStamp; currentLot.lotStamp += 1
  const record: SplitRecord = { lotNo: currentLot.lotNo, fromComponentNo: currentSource.componentNo, newComponents: replacement.map(({ componentNo, goodQty, scrapQty }) => ({ componentNo, goodQty, scrapQty })), oldLotStamp, newLotStamp: currentLot.lotStamp, savedAt: new Date().toISOString() }
  current.records.push(record)
  try { window.localStorage.setItem(storageKey, JSON.stringify(current)) }
  catch { errorText.value = '瀏覽器無法儲存 DEV 模擬結果。'; return }
  state.value = current; lot.value = null; lotInput.value = ''; clearSelection()
  message.value = `DEV 已模擬把 ${record.fromComponentNo} 分為 ${replacement.length} 顆，Lot CurQty 現為 ${currentLot.curQty}；未更新 MES。`
}
onMounted(loadState)
</script>

<template>
  <section class="wp215-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽：輸入 DEV-LOT-215 並按 Enter；DEV-LOT-215-CAR 示範載具拒絕。</div>
    <h1>元件分片處理 <small>LOT COMP SPLIT · WP215</small></h1>
    <p v-if="!dev" class="notice" role="status">{{ unavailableMessage }}</p><p v-else-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="source-frame"><legend>ComponentState</legend><div class="control-row"><label for="wp215-lot">Lot No</label><input id="wp215-lot" v-model="lotInput" class="mes-input editable" :disabled="!dev" @input="invalidateLot" @keydown.enter.prevent="queryLot"><small>按 Enter 載入</small></div>
      <div class="grid-wrap source-grid"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th></tr></thead><tbody><tr v-for="item in lot?.components || []" :key="item.componentNo" :class="{ active: item.componentNo === activeComponentNo }" @click="activate(item.componentNo)" @dblclick="selectComponent(0)"><td>{{ item.componentNo }}</td><td class="number">{{ item.goodQty }}</td><td class="number">{{ item.scrapQty }}</td></tr><tr v-if="!lot"><td colspan="3" class="empty">輸入 Lot No 並按 Enter。</td></tr></tbody></table></div><div class="source-actions"><button type="button" class="mes-button" :disabled="!dev || !activeComponentNo" @click="selectComponent()">Select</button></div>
    </fieldset>
    <fieldset class="split-frame"><legend>SplitComponent</legend><div class="control-row"><label for="wp215-comp">ComponentNo</label><input id="wp215-comp" :value="fromComponentNo" class="mes-input" readonly><label for="wp215-count">Split Comp Count</label><input id="wp215-count" v-model.number="count" type="number" min="2" max="26" step="1" class="mes-input editable count" :disabled="!canEdit"><button type="button" class="mes-button" :disabled="!canEdit" @click="splitWafer">SplitWafer</button></div>
      <div class="control-row"><label>GoodQTY <input :value="source?.goodQty ?? ''" class="mes-input number" readonly></label><label>ScrapQTY <input :value="source?.scrapQty ?? ''" class="mes-input number" readonly></label><span class="totals">新元件合計：Good {{ draftGood }} / Scrap {{ draftScrap }}</span></div>
      <div class="grid-wrap draft-grid"><table><thead><tr><th>ComponentNo</th><th>GoodQty</th><th>ScrapQty</th></tr></thead><tbody><tr v-for="(row, index) in draft" :key="row.componentNo"><td class="readonly">{{ row.componentNo }}</td><td><input v-model.number="row.goodQty" type="number" min="0" step="0.0001" class="grid-input" :aria-label="row.componentNo + ' GoodQty'"></td><td><input v-model.number="row.scrapQty" type="number" min="0" step="0.0001" class="grid-input" :aria-label="row.componentNo + ' ScrapQty'"></td></tr><tr v-if="!draft.length"><td colspan="3" class="empty">選取來源元件後按 SplitWafer。</td></tr></tbody><tfoot v-if="draft.length"><tr><th>合計 {{ draft.length }} 筆</th><th class="number">{{ draftGood }}</th><th class="number">{{ draftScrap }}</th></tr></tfoot></table></div>
    </fieldset>
    <footer class="form-footer"><button type="button" class="mes-button confirm" :disabled="!canEdit || !draft.length" @click="confirmSplit">Confirm</button><button type="button" class="mes-button" @click="emit('close')">Close</button></footer>
  </section>
</template>

<style scoped>
.wp215-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:9px;box-sizing:border-box;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}h1{margin:0;font-size:18px;font-weight:600}h1 small{margin-left:8px;color:#777;font-size:12px;font-weight:400}.notice{margin:0;min-height:18px;color:#555}.notice.error{padding:5px 8px;border:1px solid #d6aaa6;background:#fff0ef;color:#99251e}.source-frame,.split-frame{min-width:0;margin:0;padding:6px 8px 8px;box-sizing:border-box;border:1px solid #bdbdbd;background:#fff}legend{padding:0 5px}.source-frame{height:205px;flex:0 0 205px;display:flex;flex-direction:column}.split-frame{flex:1;min-height:230px;display:flex;flex-direction:column}.control-row{min-height:38px;display:flex;align-items:center;gap:8px;flex-wrap:wrap}.control-row label{display:flex;align-items:center;gap:5px}.control-row>label:first-child{min-width:94px;justify-content:flex-end}.control-row small,.totals{color:#666}.mes-input{height:26px;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.editable{background:#fff0b3}.count{width:66px}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f3f3f3);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff5dc}.mes-button:disabled{color:#999;cursor:default}.confirm{color:#fff;background:#f3a62a;border-color:#e99512}.confirm:hover:not(:disabled){background:#e99a1b}.grid-wrap{min-width:0;overflow:auto;border:1px solid #a7a7a7;background:#fff}.source-grid{flex:1;min-height:80px}.draft-grid{flex:1;min-height:110px}.grid-wrap table{width:100%;border-collapse:collapse;white-space:nowrap}.grid-wrap th{position:sticky;top:0;z-index:1;padding:5px 7px;border:1px solid #d4d4d4;background:#ead591;color:#393326;font-weight:500;text-align:left}.grid-wrap td{padding:4px 6px;border:1px solid #d2d2d2;background:#ffe89c}.grid-wrap tr.active td{background:#ffdb73}.grid-wrap td.readonly{background:#e6e6e6}.grid-wrap tfoot th{position:sticky;bottom:0;top:auto;background:#e8e4d7}.grid-input{width:100%;min-width:80px;padding:3px 5px;border:1px solid #aaa;background:#fff0b3;box-sizing:border-box;text-align:right;font:inherit}.number{text-align:right;font-variant-numeric:tabular-nums}.empty{text-align:center!important;background:#fff!important;color:#777!important;height:36px}.source-actions{display:flex;justify-content:flex-end;padding-top:5px}.form-footer{min-height:36px;display:flex;justify-content:flex-end;align-items:center;gap:8px}
</style>
