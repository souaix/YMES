<script setup lang="ts">
import { computed, ref } from 'vue'

type OrderFixture = { moNo: string; state: 3 | 6; productNo: string; productVersion: string; unreleaseLotQty: number }
type LotFixture = {
  lotNo: string; moNo: string; hotLot: number; priority: number; status: number; productNo: string; productName: string
  productVersion: string; customerName: string; customerLotNo: string; areaNo: string; processNo: string; opNo: string
  opName: string; brNo: string; curQty: number; curUnitNo: string; sysQty: number; sysUnitNo: string
  lotState: 0; processHaveComponent: boolean; componentBasisExists: boolean; enterpriseNo: string
}
type ComponentRow = { componentNo: string; goodQty: number; waferId: number; unitNo: string; scrapQty: number }
type Activity = { id: number; time: string; message: string }
type UnitOption = { unitNo: string; unitType: string; factorFromRelease: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const orders: OrderFixture[] = [
  { moNo: 'DEV-MO-CG-1001', state: 3, productNo: 'DEV-CP-COMP-A', productVersion: 'A.2', unreleaseLotQty: 12 },
  { moNo: 'DEV-MO-CG-1002', state: 6, productNo: 'DEV-CP-COMP-B', productVersion: 'B.1', unreleaseLotQty: 0 },
]
const lotFixtures: LotFixture[] = [
  { lotNo: 'DEVLOT-CG-1001-01', moNo: 'DEV-MO-CG-1001', hotLot: 1, priority: 2, status: 0, productNo: 'DEV-CP-COMP-A', productName: 'DEV CP Product A', productVersion: 'A.2', customerName: 'DEV Customer A', customerLotNo: 'DEV-CUSTLOT-CG-01', areaNo: 'DEV-CP', processNo: 'DEV-PROC-CP-A', opNo: 'DEV-CP10', opName: 'DEV Wafer Test', brNo: '', curQty: 3, curUnitNo: 'DEV-ORG-U', sysQty: 3, sysUnitNo: 'DEV-SYS-U', lotState: 0, processHaveComponent: true, componentBasisExists: false, enterpriseNo: 'DEV' },
  { lotNo: 'DEVLOT-CG-1001-02', moNo: 'DEV-MO-CG-1001', hotLot: 0, priority: 5, status: 0, productNo: 'DEV-CP-COMP-A', productName: 'DEV CP Product A', productVersion: 'A.2', customerName: 'DEV Customer A', customerLotNo: 'DEV-CUSTLOT-CG-02', areaNo: 'DEV-CP', processNo: 'DEV-PROC-CP-A', opNo: 'DEV-CP10', opName: 'DEV Wafer Test', brNo: '', curQty: 2, curUnitNo: 'DEV-ORG-U', sysQty: 2, sysUnitNo: 'DEV-SYS-U', lotState: 0, processHaveComponent: true, componentBasisExists: false, enterpriseNo: 'DEV' },
  { lotNo: 'DEVLOT-CG-1001-03', moNo: 'DEV-MO-CG-1001', hotLot: 0, priority: 8, status: 0, productNo: 'DEV-CP-COMP-A', productName: 'DEV CP Product A', productVersion: 'A.2', customerName: 'DEV Customer A', customerLotNo: 'DEV-CUSTLOT-CG-03', areaNo: 'DEV-CP', processNo: 'DEV-PROC-CP-B', opNo: 'DEV-CP20', opName: 'DEV Final Test', brNo: '', curQty: 1, curUnitNo: 'DEV-ORG-U', sysQty: 1, sysUnitNo: 'DEV-SYS-U', lotState: 0, processHaveComponent: false, componentBasisExists: false, enterpriseNo: 'DEV' },
  { lotNo: 'DEVLOT-CG-1001-04', moNo: 'DEV-MO-CG-1001', hotLot: 0, priority: 10, status: 0, productNo: 'DEV-CP-COMP-A', productName: 'DEV CP Product A', productVersion: 'A.2', customerName: 'DEV Customer A', customerLotNo: 'DEV-CUSTLOT-CG-04', areaNo: 'DEV-CP', processNo: 'DEV-PROC-CP-A', opNo: 'DEV-CP10', opName: 'DEV Wafer Test', brNo: '', curQty: 1, curUnitNo: 'DEV-ORG-U', sysQty: 1, sysUnitNo: 'DEV-SYS-U', lotState: 0, processHaveComponent: true, componentBasisExists: true, enterpriseNo: 'DEV' },
  { lotNo: 'DEVLOT-CG-1002-01', moNo: 'DEV-MO-CG-1002', hotLot: 0, priority: 4, status: 0, productNo: 'DEV-CP-COMP-B', productName: 'DEV CP Product B', productVersion: 'B.1', customerName: 'DEV Customer B', customerLotNo: 'DEV-CUSTLOT-CG-11', areaNo: 'DEV-CP', processNo: 'DEV-PROC-CP-C', opNo: 'DEV-CP30', opName: 'DEV Probe', brNo: '', curQty: 2, curUnitNo: 'DEV-ORG-U', sysQty: 2, sysUnitNo: 'DEV-SYS-U', lotState: 0, processHaveComponent: true, componentBasisExists: false, enterpriseNo: 'DEV' },
]
const unitOptions: UnitOption[] = [
  { unitNo: 'DEV-COMP-U', unitType: 'DEV-QTY', factorFromRelease: 1 },
  { unitNo: 'DEV-COMP-U2', unitType: 'DEV-QTY', factorFromRelease: 2 },
]
const componentRowsByLot = ref<Record<string, ComponentRow[]>>({
  'DEVLOT-CG-1001-02': [
    { componentNo: 'DEV-COMP-EXIST-0201', goodQty: 1, waferId: 1, unitNo: 'DEV-COMP-U', scrapQty: 0 },
    { componentNo: 'DEV-COMP-EXIST-0202', goodQty: 1, waferId: 2, unitNo: 'DEV-COMP-U', scrapQty: 0 },
  ],
})

const moNo = ref('')
const lotNo = ref('')
const resultLots = ref<LotFixture[]>([])
const selectedLotNo = ref('')
const moDialog = ref(false)
const newComponentDialog = ref(false)
const error = ref('')
const notice = ref('先選取或輸入 MO No，再查詢 Lot State=0 的批次。')
const activities = ref<Activity[]>([])
const nextActivityId = ref(1)

const selectedLot = computed(() => resultLots.value.find(lot => lot.lotNo === selectedLotNo.value) ?? null)
const currentComponents = computed(() => componentRowsByLot.value[selectedLotNo.value] ?? [])
const canShowComponentRows = computed(() => selectedLot.value !== null)
const totalNewGoodQty = computed(() => newRows.value.reduce((sum, row) => sum + Number(row.goodQty || 0), 0))
const targetGoodQty = computed(() => selectedLot.value ? selectedLot.value.sysQty * releaseToSystemFactor.value : 0)
const chosenUnit = computed(() => unitOptions.find(unit => unit.unitNo === componentUnitNo.value) ?? null)
const generatedCount = computed(() => newRows.value.length)

const componentUnitNo = ref('')
const startWaferId = ref(1)
const endWaferId = ref(1)
const serialMode = ref<'auto' | 'manual'>('auto')
const newRows = ref<ComponentRow[]>([])
const newComponentError = ref('')
const releaseToSystemFactor = ref(1)

function timeText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function addActivity(message: string): void {
  activities.value.unshift({ id: nextActivityId.value++, time: timeText(), message })
  activities.value = activities.value.slice(0, 20)
}
function sortLots(lots: LotFixture[]): LotFixture[] {
  return [...lots].sort((a, b) => b.hotLot - a.hotLot || a.priority - b.priority || a.lotNo.localeCompare(b.lotNo))
}
function queryLots(): void {
  error.value = ''
  const queryMo = moNo.value.trim().toUpperCase()
  if (!queryMo) { resultLots.value = []; selectedLotNo.value = ''; error.value = 'MO No 必填。'; return }
  const order = orders.find(item => item.moNo === queryMo && [3, 6].includes(item.state))
  if (!order) { resultLots.value = []; selectedLotNo.value = ''; error.value = '找不到可查詢的 DEV MO（MOState 僅模擬 3 或 6）。'; return }
  const lotFilter = lotNo.value.trim().toUpperCase()
  const lotsForOrder = lotFixtures.filter(lot => lot.moNo === order.moNo && lot.lotState === 0)
  if (order.unreleaseLotQty <= 0 && lotsForOrder.length === 0) {
    resultLots.value = []; selectedLotNo.value = ''; error.value = 'MO 不符合未下線數量或未確認 Lot 查詢條件。'; return
  }
  resultLots.value = sortLots(lotsForOrder.filter(lot => !lotFilter || lot.lotNo.includes(lotFilter)))
  selectedLotNo.value = ''
  notice.value = '查詢完成：' + resultLots.value.length + ' 筆 Lot Basis／State=0。選取批次後會載入 Component State。'
  addActivity('Query lots · ' + order.moNo + ' · ' + resultLots.value.length + ' 筆')
}
function openMoLookup(): void { moDialog.value = true }
function chooseMo(order: OrderFixture): void {
  moNo.value = order.moNo
  moDialog.value = false
  queryLots()
}
function selectLot(lot: LotFixture): void {
  selectedLotNo.value = lot.lotNo
  error.value = ''
  notice.value = '已載入 ' + lot.lotNo + ' 的 Component State；NEWCOMPONENT 會先檢查流程與既有 Component Basis。'
  addActivity('Select lot · ' + lot.lotNo)
}
function lotGenerationBlockReason(lot: LotFixture | null): string {
  if (!lot) return '請先選取一筆 Lot。'
  if ((componentRowsByLot.value[lot.lotNo] ?? []).length > 0 || lot.componentBasisExists) return 'Component State 或 Component Basis 已存在，不能重複產生。'
  if (lot.enterpriseNo === 'SP' || lot.enterpriseNo === 'SET') return '此 DEV Enterprise 不允許此功能產生 Component。'
  if (!lot.processHaveComponent) return '產品流程未設定 HaveComponent。'
  return ''
}
function openNewComponent(): void {
  const reason = lotGenerationBlockReason(selectedLot.value)
  if (reason) { error.value = reason; return }
  const lot = selectedLot.value!
  componentUnitNo.value = ''
  startWaferId.value = 1
  endWaferId.value = Math.max(1, Math.trunc(lot.sysQty))
  serialMode.value = 'auto'
  newRows.value = []
  newComponentError.value = ''
  newComponentDialog.value = true
}
function closeNewComponent(): void {
  newComponentDialog.value = false
  newComponentError.value = ''
}
function generateComponentRows(): void {
  newComponentError.value = ''
  const lot = selectedLot.value
  if (!lot) { newComponentError.value = '請先選取 Lot。'; return }
  const first = Number(startWaferId.value)
  const last = Number(endWaferId.value)
  if (!Number.isInteger(first) || !Number.isInteger(last) || first < 1 || last < 1) { newComponentError.value = 'Wafer ID 起訖必須是正整數。'; return }
  if (first > last) { newComponentError.value = 'Wafer ID 起始值不可大於結束值。'; return }
  if (last - first > 99) { newComponentError.value = 'DEV 預覽最多產生 100 筆 Component。'; return }
  if (!chosenUnit.value) { newComponentError.value = '請選擇與 Release Unit 不同的 Component Unit。'; return }
  if (chosenUnit.value.unitType !== 'DEV-QTY') { newComponentError.value = 'Component Unit 必須與 Release Unit 同 UnitType。'; return }
  if (serialMode.value === 'auto' && last > Math.trunc(lot.sysQty)) {
    newComponentError.value = 'DEV 自動序號 fixture 數量不足；來源以 Wafer ID 作序號陣列索引。'; return
  }
  const width = lot.sysQty < 100 ? 2 : lot.sysQty < 1000 ? 3 : 4
  newRows.value = Array.from({ length: last - first + 1 }, (_, offset) => {
    const waferId = first + offset
    const componentNo = serialMode.value === 'auto'
      ? 'DEV-COMP-SN-' + lot.moNo.slice(-4) + '-' + String(waferId).padStart(5, '0')
      : lot.moNo + '-' + String(waferId).padStart(width, '0')
    return { componentNo, goodQty: chosenUnit.value!.factorFromRelease, waferId, unitNo: chosenUnit.value!.unitNo, scrapQty: 0 }
  })
  addActivity('Generate component preview · ' + lot.lotNo + ' · ' + newRows.value.length + ' 筆')
}
function clearGeneratedRows(): void { newRows.value = []; newComponentError.value = '' }
function confirmNewComponents(): void {
  newComponentError.value = ''
  const lot = selectedLot.value
  if (!lot) { newComponentError.value = 'Lot 已不存在，請重新查詢。'; return }
  const reason = lotGenerationBlockReason(lot)
  if (reason) { newComponentError.value = reason; return }
  if (!componentUnitNo.value || newRows.value.length === 0) { newComponentError.value = '請先選擇單位並產生 Component 清單。'; return }
  const invalid = newRows.value.find(row => !Number.isFinite(Number(row.goodQty)) || Number(row.goodQty) <= 0 || !Number.isInteger(Number(row.waferId)) || Number(row.waferId) < 1)
  if (invalid) { newComponentError.value = '每筆 GoodQty 必須大於 0，Wafer ID 必須是正整數。'; return }
  const waferIds = newRows.value.map(row => Number(row.waferId))
  if (new Set(waferIds).size !== waferIds.length) { newComponentError.value = 'Wafer ID 不可重複。'; return }
  if (Math.abs(totalNewGoodQty.value - targetGoodQty.value) > 0.000001) {
    newComponentError.value = 'GoodQty 合計 ' + totalNewGoodQty.value + ' 必須等於 SumQty ' + targetGoodQty.value + '。'
    return
  }
  const prior = componentRowsByLot.value[lot.lotNo] ?? []
  if (prior.length || lot.componentBasisExists) { newComponentError.value = 'Component 已存在，請重新查詢確認批次狀態。'; return }
  componentRowsByLot.value[lot.lotNo] = newRows.value.map(row => ({ ...row, goodQty: Number(row.goodQty), waferId: Number(row.waferId), unitNo: componentUnitNo.value, scrapQty: 0 }))
  newComponentDialog.value = false
  notice.value = 'DEV 完成：假資料已呈現在 Component State；未呼叫服務或寫入資料庫。'
  addActivity('Confirm DEV component generation · ' + lot.lotNo + ' · ' + newRows.value.length + ' 筆')
}
function resetSearch(): void {
  moNo.value = ''; lotNo.value = ''; resultLots.value = []; selectedLotNo.value = ''; error.value = ''
  notice.value = '查詢條件已重設。'
  addActivity('Reset query')
}
</script>

<template>
  <section v-if="dev" class="lot-component-dev" aria-label="生產批自主產生元件 DEV">
    <header class="page-head">
      <div><p class="eyebrow">WIP · CP LOT COMPONENT</p><h1>生產批自主產生元件</h1><p class="subtitle">P_LOT COMP GEN · WPP08 · 僅 DEV 假資料</p></div>
      <div class="head-actions"><span class="dev-badge">DEV PREVIEW</span><button class="btn light" type="button" @click="resetSearch">重設條件</button><button class="btn dark" type="button" @click="emit('close')">Close (X)</button></div>
    </header>

    <div class="notice" role="status"><span class="dot"></span><span>{{ notice }}</span><span class="fixture-tag">memory only</span></div>

    <section class="panel query-panel">
      <div class="panel-title"><div><p class="eyebrow">QUERY</p><h2>查詢條件</h2></div><span class="subtle">MOState = 3 / 6 · LotState = 0</span></div>
      <form class="query-grid" @submit.prevent="queryLots">
        <label class="field"><span>MO No</span><span class="lookup"><input v-model="moNo" type="text" autocomplete="off" placeholder="DEV-MO-CG-1001" @keydown.enter.prevent="queryLots"><button class="btn light compact" type="button" @click="openMoLookup">...</button></span></label>
        <label class="field"><span>Lot No（選填）</span><input v-model="lotNo" type="text" autocomplete="off" placeholder="可輸入 Lot No 片段" @keydown.enter.prevent="queryLots"></label>
        <div class="query-actions"><span>MO 查詢條件模擬未下線量大於 0，或仍有 LotState=0 的批次。</span><button class="btn primary" type="submit">Query</button></div>
      </form>
    </section>

    <section class="panel lot-panel">
      <div class="panel-title"><div><p class="eyebrow">LOT BASIS / STATE</p><h2>未確認批次</h2></div><span class="subtle">{{ resultLots.length }} rows · HotLot desc, Priority, LotNo</span></div>
      <div class="table-wrap"><table class="lot-table"><thead><tr><th>Lot No</th><th>MO No</th><th>Hot Lot</th><th>Priority</th><th>Status</th><th>Product No</th><th>Product Name</th><th>Customer</th><th>Version</th><th>Area</th><th>Process</th><th>OP</th><th>OP Name</th><th>Cur Qty</th><th>Cur Unit</th><th>Sys Qty</th><th>Sys Unit</th></tr></thead><tbody>
        <tr v-for="lot in resultLots" :key="lot.lotNo" :class="{ selected: selectedLotNo === lot.lotNo }" tabindex="0" @click="selectLot(lot)" @keydown.enter="selectLot(lot)"><td><strong>{{ lot.lotNo }}</strong></td><td>{{ lot.moNo }}</td><td>{{ lot.hotLot ? 'Yes' : 'No' }}</td><td>{{ lot.priority }}</td><td>{{ lot.status === 0 ? 'Unconfirmed' : lot.status }}</td><td>{{ lot.productNo }}</td><td>{{ lot.productName }}</td><td>{{ lot.customerName }}</td><td>{{ lot.productVersion }}</td><td>{{ lot.areaNo }}</td><td>{{ lot.processNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.opName }}</td><td class="numeric">{{ lot.curQty }}</td><td>{{ lot.curUnitNo }}</td><td class="numeric">{{ lot.sysQty }}</td><td>{{ lot.sysUnitNo }}</td></tr>
        <tr v-if="!resultLots.length"><td colspan="17" class="empty">先輸入 MO No 後按 Query。</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="error" class="error" role="alert">{{ error }}</div>

    <section class="panel component-panel">
      <div class="panel-title"><div><p class="eyebrow">COMPONENT STATE</p><h2>批次元件</h2></div><div class="head-actions"><span v-if="selectedLot" class="subtle">{{ selectedLot.lotNo }} · {{ currentComponents.length }} rows</span><button class="btn primary" type="button" :disabled="!selectedLot" @click="openNewComponent">NEWCOMPONENT</button></div></div>
      <p v-if="selectedLot" class="eligibility" :class="{ blocked: lotGenerationBlockReason(selectedLot) }">{{ lotGenerationBlockReason(selectedLot) || '可產生：流程含 Component 且尚無 Component State / Basis。' }}</p>
      <div class="table-wrap"><table class="component-table"><thead><tr><th>Component No</th><th>Good Qty</th><th>Wafer ID</th><th>Unit No</th></tr></thead><tbody>
        <tr v-for="row in currentComponents" :key="row.componentNo"><td>{{ row.componentNo }}</td><td class="numeric">{{ row.goodQty }}</td><td class="numeric">{{ row.waferId }}</td><td>{{ row.unitNo }}</td></tr>
        <tr v-if="canShowComponentRows && !currentComponents.length"><td colspan="4" class="empty">尚無 Component State。</td></tr>
        <tr v-if="!canShowComponentRows"><td colspan="4" class="empty">選取一筆 Lot 後載入 Component State。</td></tr>
      </tbody></table></div>
    </section>

    <section class="panel activity-panel"><div class="panel-title"><div><p class="eyebrow">DEV ACTIVITY</p><h2>操作紀錄</h2></div><span class="dev-badge">頁面記憶體</span></div><div v-if="activities.length" class="activity-list"><article v-for="entry in activities" :key="entry.id"><time>{{ entry.time }}</time><strong>{{ entry.message }}</strong></article></div><p v-else class="empty">尚無操作紀錄。</p></section>

    <div v-if="moDialog" class="backdrop" role="presentation" @click.self="moDialog = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="DEV MO lookup"><header><div><p class="eyebrow">DEV LOOKUP</p><h2>選擇 MO No</h2></div><button class="icon" type="button" @click="moDialog = false">×</button></header><div class="table-wrap lookup-table"><table><thead><tr><th>MO No</th><th>MO State</th><th>Product</th><th>Version</th><th>Unrelease Lot Qty</th><th></th></tr></thead><tbody><tr v-for="order in orders" :key="order.moNo"><td>{{ order.moNo }}</td><td>{{ order.state }}</td><td>{{ order.productNo }}</td><td>{{ order.productVersion }}</td><td>{{ order.unreleaseLotQty }}</td><td><button class="btn light compact" type="button" @click="chooseMo(order)">選取</button></td></tr></tbody></table></div></section></div>

    <div v-if="newComponentDialog && selectedLot" class="backdrop" role="presentation" @click.self="closeNewComponent"><section class="dialog generator-dialog" role="dialog" aria-modal="true" aria-label="DEV New Component"><header><div><p class="eyebrow">DEV COMPONENT GENERATOR</p><h2>NewComponent · {{ selectedLot.lotNo }}</h2></div><button class="icon" type="button" @click="closeNewComponent">×</button></header>
      <div class="context-grid"><div><span>MO No</span><strong>{{ selectedLot.moNo }}</strong></div><div><span>Release Unit</span><strong>{{ selectedLot.curUnitNo }}</strong></div><div><span>System Unit</span><strong>{{ selectedLot.sysUnitNo }}</strong></div><div><span>System Qty</span><strong>{{ selectedLot.sysQty }}</strong></div><div><span>Target SumQty</span><strong>{{ targetGoodQty }}</strong></div><div><span>DEV release→system factor</span><strong>{{ releaseToSystemFactor }}</strong></div></div>
      <div class="generator-fields"><label class="field"><span>Component Unit</span><select v-model="componentUnitNo"><option value="">請選擇（不得與 Release Unit 相同）</option><option v-for="unit in unitOptions" :key="unit.unitNo" :value="unit.unitNo">{{ unit.unitNo }} · {{ unit.unitType }} · DEV factor {{ unit.factorFromRelease }}</option></select></label><label class="field"><span>Wafer ID Start</span><input v-model.number="startWaferId" type="number" min="1" step="1"></label><label class="field"><span>Wafer ID End</span><input v-model.number="endWaferId" type="number" min="1" step="1"></label><label class="field"><span>DEV SerialConfig fixture</span><select v-model="serialMode"><option value="auto">Automatic · fake reserved sequence</option><option value="manual">Manual · MO No + padded Wafer ID</option></select></label></div>
      <div class="generator-actions"><span>按 NewComponent 依序號模式、Wafer ID 範圍和單位換算產生預覽列；可調整 GoodQty／Wafer ID。</span><div><button class="btn light" type="button" @click="clearGeneratedRows">Clear</button><button class="btn dark" type="button" @click="generateComponentRows">NewComponent</button></div></div>
      <div class="table-wrap generated-table"><table><thead><tr><th>Component No</th><th>Good Qty</th><th>Wafer ID</th><th>Unit No</th></tr></thead><tbody><tr v-for="(row, index) in newRows" :key="row.componentNo"><td>{{ row.componentNo }}</td><td><input v-model.number="row.goodQty" type="number" min="0" step="0.0001" :aria-label="'GoodQty ' + (index + 1)"></td><td><input v-model.number="row.waferId" type="number" min="1" step="1" :aria-label="'Wafer ID ' + (index + 1)"></td><td>{{ row.unitNo }}</td></tr><tr v-if="!newRows.length"><td colspan="4" class="empty">設定單位和 Wafer ID 範圍後按 NewComponent。</td></tr></tbody></table></div>
      <div class="sum-row"><span>Component Count <strong>{{ generatedCount }}</strong></span><span>Sum GoodQty <strong>{{ totalNewGoodQty }}</strong></span><span>Required SumQty <strong>{{ targetGoodQty }}</strong></span><span :class="{ mismatch: Math.abs(totalNewGoodQty - targetGoodQty) > 0.000001 }">{{ Math.abs(totalNewGoodQty - targetGoodQty) <= 0.000001 ? 'Quantity balanced' : 'Quantity mismatch' }}</span></div>
      <p v-if="newComponentError" class="error dialog-error" role="alert">{{ newComponentError }}</p>
      <p class="source-hint">DEV 只改頁面記憶體。正式服務需重新檢查批次／流程／單位、序號唯一性和總量，並同步寫入 MO Component、Component Basis、Component State。</p>
      <div class="dialog-actions"><button class="btn light" type="button" @click="closeNewComponent">取消</button><button class="btn primary" type="button" @click="confirmNewComponents">Confirm DEV</button></div>
    </section></div>
  </section>
  <section v-else class="lot-component-pending"><p>生產批自主產生元件正式服務尚未接入。</p><button type="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.lot-component-dev,.lot-component-pending{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:18px 22px;background:#f3f2ed;color:#292922;font:inherit}.page-head,.panel-title,.head-actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head{margin-bottom:13px}.page-head h1{margin:2px 0 3px;font-size:23px;font-weight:650}.subtitle{margin:0;color:#716f66;font-size:12px}.eyebrow{margin:0 0 4px;color:#7d7869;font-size:10px;font-weight:700;letter-spacing:.12em}.dev-badge,.fixture-tag{display:inline-flex;align-items:center;border:1px solid #d6d0bf;border-radius:12px;padding:4px 9px;background:#f7f4e9;color:#70694f;font-size:10px;white-space:nowrap}.btn{min-height:29px;border:1px solid #bdb9ad;border-radius:4px;padding:4px 10px;background:#fff;color:#343229;font:inherit;font-size:12px;cursor:pointer;white-space:nowrap}.btn:disabled{opacity:.5;cursor:not-allowed}.btn.primary{border-color:#465947;background:#465947;color:#fff}.btn.dark{border-color:#3b3930;background:#3b3930;color:#fff}.btn.light{background:#f8f7f2}.btn.compact{min-height:26px;padding:2px 8px;font-size:11px}.notice{display:flex;align-items:center;gap:8px;min-height:34px;margin-bottom:12px;padding:6px 11px;border:1px solid #d7d2c3;border-radius:5px;background:#fbfaf6;color:#49473e;font-size:11px}.notice .dot{width:8px;height:8px;flex:none;border-radius:50%;background:#6a8c58}.notice .fixture-tag{margin-left:auto}.panel{margin:0 0 12px;padding:13px 15px;border:1px solid #d9d6cb;border-radius:6px;background:#fffefa;box-shadow:0 1px 2px #312f2610}.panel-title{margin-bottom:10px}.panel-title h2{margin:0;font-size:15px;font-weight:650}.subtle{color:#807c70;font-size:10px}.query-grid{display:grid;grid-template-columns:1fr 1fr auto;gap:12px;align-items:end}.field{display:grid;gap:5px;min-width:0;color:#514e44;font-size:11px;font-weight:600}.field input,.field select,.lookup input{height:31px;min-width:0;box-sizing:border-box;border:1px solid #c9c5b9;border-radius:4px;background:#fff;padding:5px 8px;color:#292922;font:inherit;font-size:11px}.lookup{display:flex;gap:5px}.lookup input{flex:1}.query-actions{display:flex;align-items:center;gap:12px;color:#807c70;font-size:10px}.query-actions span{max-width:260px}.table-wrap{overflow:auto;border:1px solid #e3e0d7;border-radius:4px}table{width:max-content;min-width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th,td{padding:6px 8px;border-bottom:1px solid #ece9e1;text-align:left}th{position:sticky;top:0;background:#f4f2eb;color:#5f5b50;font-size:9px;font-weight:700;letter-spacing:.03em}tbody tr:hover{background:#faf9f4}.lot-table tbody tr{cursor:pointer}.lot-table tbody tr.selected{background:#e9eee4;box-shadow:inset 3px 0 #536b4d}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty{padding:18px;text-align:center;color:#908b7d;font-size:11px}.component-panel .panel-title{align-items:center}.eligibility{margin:0 0 9px;padding:7px 9px;border:1px solid #d4decf;border-radius:4px;background:#f4f8f1;color:#4c6749;font-size:10px}.eligibility.blocked{border-color:#e0d1c5;background:#fbf5f0;color:#936248}.error{margin:0 0 12px;padding:8px 10px;border:1px solid #ddb0a9;border-radius:4px;background:#fff5f3;color:#963f33;font-size:11px}.activity-list{display:grid}.activity-list article{display:grid;grid-template-columns:140px 1fr;gap:12px;padding:7px 2px;border-bottom:1px solid #eeeae2;font-size:10px}.activity-list time{color:#817c70}.backdrop{position:fixed;z-index:1200;inset:0;display:flex;align-items:center;justify-content:center;padding:18px;background:#211f1c75}.dialog{width:min(760px,100%);max-height:92vh;overflow:auto;box-sizing:border-box;border:1px solid #d6d2c6;border-radius:7px;background:#fffefa;padding:17px;box-shadow:0 16px 48px #1115}.dialog header{display:flex;justify-content:space-between;align-items:flex-start;gap:10px;margin-bottom:13px}.dialog h2{margin:0;font-size:16px}.icon{width:30px;height:30px;border:1px solid #d1cdc1;border-radius:4px;background:white;color:#524f44;font-size:20px;cursor:pointer}.lookup-table{max-height:55vh}.generator-dialog{width:min(960px,100%)}.context-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:1px;margin-bottom:13px;border:1px solid #d9d6cb;border-radius:5px;overflow:hidden;background:#d9d6cb}.context-grid>div{display:grid;gap:4px;padding:9px 11px;background:#fbfaf6}.context-grid span{color:#817c70;font-size:9px;text-transform:uppercase;letter-spacing:.04em}.context-grid strong{font-size:11px;font-weight:650}.generator-fields{display:grid;grid-template-columns:1.5fr 1fr 1fr 1.6fr;gap:10px;margin-bottom:12px}.generator-actions{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-bottom:8px;color:#807c70;font-size:10px}.generator-actions>div{display:flex;gap:7px}.generated-table{max-height:35vh}.generated-table input{width:105px;height:26px;box-sizing:border-box;border:1px solid #c9c5b9;border-radius:3px;padding:3px 6px;font:inherit;font-size:10px;text-align:right}.sum-row{display:flex;align-items:center;justify-content:space-between;gap:10px;margin-top:9px;padding:8px 10px;border:1px solid #e3e0d7;border-radius:4px;background:#fbfaf6;color:#696559;font-size:10px}.sum-row strong{margin-left:4px;color:#292922;font-size:11px}.sum-row .mismatch{color:#9a473a;font-weight:700}.source-hint{margin:9px 0 0;color:#807c70;font-size:10px}.dialog-error{margin-top:10px}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:12px;padding-top:11px;border-top:1px solid #ece9e1}.lot-component-pending{height:auto}.lot-component-pending p{margin:0 0 10px;color:#777366}.lot-component-pending button{min-height:28px;padding:4px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:#fff;font:inherit;cursor:pointer}
@media(max-width:980px){.query-grid{grid-template-columns:1fr 1fr}.query-actions{grid-column:1/-1;justify-content:space-between}.generator-fields{grid-template-columns:1fr 1fr}.sum-row{align-items:flex-start;flex-direction:column}}
@media(max-width:650px){.lot-component-dev{padding:12px}.page-head,.panel-title{align-items:flex-start;flex-direction:column}.head-actions{width:100%;justify-content:flex-start;flex-wrap:wrap}.query-grid{grid-template-columns:1fr}.query-actions{align-items:flex-start;flex-direction:column}.component-panel .panel-title>.head-actions{align-items:flex-start;flex-direction:column}.generator-fields,.context-grid{grid-template-columns:1fr 1fr}.generator-actions{align-items:flex-start;flex-direction:column}.dialog{padding:13px}.generated-table input{width:78px}}
</style>
