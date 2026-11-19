<script setup lang="ts">
import { computed, ref } from 'vue'

type Carrier = {
  carrierNo: string; carrierCategory: string; carrierType: string; carrierState: number
  description: string; carrierLayer: number; carrierCapacity: number
}
type CarrierDetail = {
  id: string; carrierNo: string; carrierPosition: number; bcip: string; contentId: string
  flag: number; lotNo: string; componentNo: string; eventTime: string
}
type Lot = {
  lotNo: string; lotState: number; status: number; carrierNo: string | null
  components: string[]; stockerNo?: string; locatorNo?: string
}
type Snapshot = { version: 1; carriers: Carrier[]; details: CarrierDetail[]; lots: Lot[]; logs: CarrierDetail[] }
type ConfirmAction = 'takeout' | 'clear' | null

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp302-carrier-state.v1'

function seedSnapshot(): Snapshot {
  return {
    version: 1,
    carriers: [
      { carrierNo: 'DEV-CARRIER-01', carrierCategory: 'Tray 載具', carrierType: 'TRAY-25', carrierState: 1, description: 'DEV 多層 Tray 載具', carrierLayer: 1, carrierCapacity: 25 },
      { carrierNo: 'DEV-CARRIER-02', carrierCategory: 'Tray 載具', carrierType: 'TRAY-10', carrierState: 0, description: 'DEV 空載具', carrierLayer: 1, carrierCapacity: 10 },
      { carrierNo: 'DEV-CARRIER-03', carrierCategory: 'Tray 載具', carrierType: 'TRAY-25', carrierState: 0, description: 'DEV 備用載具', carrierLayer: 1, carrierCapacity: 25 },
    ],
    details: [
      { id: 'wp302-detail-1', carrierNo: 'DEV-CARRIER-01', carrierPosition: 1, bcip: 'DEVUSER', contentId: 'DEV-LOT-001', flag: 0, lotNo: 'DEV-LOT-001', componentNo: 'N/A', eventTime: '2026/11/20 08:30:00' },
      { id: 'wp302-detail-2', carrierNo: 'DEV-CARRIER-01', carrierPosition: 2, bcip: 'DEVUSER', contentId: 'DEV-LOT-COMP-001', flag: 0, lotNo: 'DEV-LOT-COMP-001', componentNo: 'DEV-COMP-001', eventTime: '2026/11/20 08:31:00' },
      { id: 'wp302-detail-3', carrierNo: 'DEV-CARRIER-01', carrierPosition: 3, bcip: 'DEVUSER', contentId: 'DEV-LOT-COMP-001', flag: 0, lotNo: 'DEV-LOT-COMP-001', componentNo: 'DEV-COMP-002', eventTime: '2026/11/20 08:31:00' },
    ],
    lots: [
      { lotNo: 'DEV-LOT-001', lotState: 1, status: 1, carrierNo: 'DEV-CARRIER-01', components: [] },
      { lotNo: 'DEV-LOT-COMP-001', lotState: 1, status: 1, carrierNo: 'DEV-CARRIER-01', components: ['DEV-COMP-001', 'DEV-COMP-002', 'DEV-COMP-003'] },
      { lotNo: 'DEV-LOT-FREE-001', lotState: 1, status: 0, carrierNo: null, components: [] },
      { lotNo: 'DEV-LOT-FREE-COMP-001', lotState: 1, status: 2, carrierNo: null, components: ['DEV-COMP-101', 'DEV-COMP-102', 'DEV-COMP-103'] },
      { lotNo: 'DEV-LOT-STOCKER-001', lotState: 1, status: 1, carrierNo: null, components: [], stockerNo: 'DEV-STOCKER-01', locatorNo: 'A-01-02' },
      { lotNo: 'DEV-LOT-CLOSED-001', lotState: 1, status: 6, carrierNo: null, components: [] },
    ],
    logs: [],
  }
}

function readSnapshot(): Snapshot {
  if (!dev.value) return seedSnapshot()
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) return seedSnapshot()
    const parsed = JSON.parse(raw) as Snapshot
    if (parsed.version === 1 && Array.isArray(parsed.carriers) && Array.isArray(parsed.details) && Array.isArray(parsed.lots) && Array.isArray(parsed.logs)) return parsed
  } catch { /* retain deterministic DEV fixtures */ }
  return seedSnapshot()
}

const snapshot = ref<Snapshot>(readSnapshot())
const selectedCarrierNo = ref('DEV-CARRIER-01')
const selectedDetailId = ref('')
const lotNoInput = ref('')
const positionInput = ref('1')
const actionError = ref('')
const actionMessage = ref('')
const confirmAction = ref<ConfirmAction>(null)
const revealWinFormsHiddenActions = ref(false)
const eventTime = ref(new Date().toLocaleString('zh-TW', { hour12: false }))

const selectedCarrier = computed(() => snapshot.value.carriers.find(item => item.carrierNo === selectedCarrierNo.value) ?? null)
const carrierDetails = computed(() => snapshot.value.details
  .filter(item => item.carrierNo === selectedCarrierNo.value)
  .sort((a, b) => a.carrierPosition - b.carrierPosition || a.contentId.localeCompare(b.contentId)))
const selectedDetail = computed(() => carrierDetails.value.find(item => item.id === selectedDetailId.value) ?? null)
const carrierCapacity = computed(() => selectedCarrier.value ? selectedCarrier.value.carrierLayer * selectedCarrier.value.carrierCapacity : 0)
const availablePositions = computed(() => Math.max(0, carrierCapacity.value - carrierDetails.value.length))
const modalText = computed(() => confirmAction.value === 'clear'
  ? '確定清除載具 ' + selectedCarrierNo.value + ' 的全部內容？系統會先記錄內容歷程。'
  : '確定從載具取出批號 ' + (selectedDetail.value?.contentId ?? '') + '？若同一批號有多個元件，會一併取出。')

function persist(): void {
  if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(snapshot.value))
}
function clearFeedback(): void { actionError.value = ''; actionMessage.value = '' }
function showError(message: string): void { actionError.value = message; actionMessage.value = '' }
function showMessage(message: string): void { actionMessage.value = message; actionError.value = '' }
function resetDev(): void {
  snapshot.value = seedSnapshot()
  selectedCarrierNo.value = 'DEV-CARRIER-01'
  selectedDetailId.value = ''
  lotNoInput.value = ''
  positionInput.value = '1'
  confirmAction.value = null
  persist()
  showMessage('已還原 WP302 DEV 假資料。')
}
function selectCarrier(carrierNo: string): void {
  selectedCarrierNo.value = carrierNo
  selectedDetailId.value = ''
  clearFeedback()
}
function selectDetail(detail: CarrierDetail): void {
  selectedDetailId.value = detail.id
  clearFeedback()
}
function findLot(lotNo: string): Lot | undefined {
  return snapshot.value.lots.find(item => item.lotNo.toUpperCase() === lotNo.toUpperCase())
}
function recordTakeout(rows: CarrierDetail[]): void {
  for (const row of rows) snapshot.value.logs.push({ ...row })
}

function putIn(): void {
  clearFeedback()
  if (!dev.value) { showError('DEV 假資料模式停用。'); return }
  const carrier = selectedCarrier.value
  if (!carrier) { showError('請先選取載具。'); return }
  const lotNo = lotNoInput.value.trim().toUpperCase()
  if (!lotNo) { showError('LotNo 不可空白。'); return }
  const position = Number(positionInput.value)
  if (!Number.isInteger(position) || position <= 0) { showError('Position 必須是大於 0 的正整數。'); return }
  const lot = findLot(lotNo)
  if (!lot || lot.lotState !== 1 || ![0, 1, 2, 3, 4, 5].includes(lot.status)) { showError('批號不存在，或批次狀態／LotState 不允許載入。'); return }
  if (lot.stockerNo) { showError('此批號目前在 Stocker：' + lot.stockerNo + '／' + (lot.locatorNo ?? '') + '，不能放入載具。'); return }
  const units = lot.components.length ? lot.components : ['N/A']
  let inserted = 0
  let stopReason = ''
  for (let i = 0; i < units.length; i++) {
    const componentNo = units[i]
    const unitPosition = position + i
    if (snapshot.value.details.some(item => item.contentId.toUpperCase() === lotNo && item.componentNo.toUpperCase() === componentNo.toUpperCase())) {
      stopReason = '批號／元件已存在於另一個載具。'
      break
    }
    if (carrierDetails.value.length >= carrierCapacity.value) { stopReason = '載具容量已滿。'; break }
    if (carrierDetails.value.some(item => item.carrierPosition === unitPosition)) { stopReason = '載具位置 ' + unitPosition + ' 已有內容。'; break }
    if (unitPosition > carrierCapacity.value) { stopReason = 'Position ' + unitPosition + ' 超出載具可用位置。'; break }
    snapshot.value.details.push({
      id: 'wp302-' + Date.now() + '-' + i + '-' + Math.random().toString(36).slice(2, 6),
      carrierNo: selectedCarrierNo.value, carrierPosition: unitPosition, bcip: props.currentUserNo || 'DEVUSER',
      contentId: lotNo, flag: 0, lotNo, componentNo, eventTime: new Date().toLocaleString('zh-TW', { hour12: false }),
    })
    lot.carrierNo = selectedCarrierNo.value
    carrier.carrierState = 1
    inserted++
  }
  if (inserted) {
    persist()
    selectedDetailId.value = ''
    showMessage('DEV 已模擬放入 ' + inserted + ' 筆載具內容。' + (stopReason ? ' 後續停止：' + stopReason : ''))
  } else {
    showError(stopReason || '沒有可放入的內容。')
  }
}

function requestTakeout(): void {
  clearFeedback()
  if (!selectedDetail.value) { showError('請先選取一筆載具內容。'); return }
  const lot = findLot(selectedDetail.value.contentId)
  if (!lot || lot.lotState !== 1 || ![0, 1, 2, 3, 4, 5].includes(lot.status)) { showError('找不到批次現況，請先執行載具管理補正。'); return }
  confirmAction.value = 'takeout'
}
function requestClear(): void {
  clearFeedback()
  if (!selectedCarrier.value) { showError('請先選取載具。'); return }
  const missing = carrierDetails.value.find(row => {
    const lot = findLot(row.contentId)
    return !lot || lot.lotState !== 1 || ![0, 1, 2, 3, 4, 5].includes(lot.status)
  })
  if (missing) { showError('批號 ' + missing.contentId + ' 找不到批次現況，請先執行載具管理補正。'); return }
  confirmAction.value = 'clear'
}
function confirmRemoval(): void {
  if (!dev.value) { confirmAction.value = null; showError('DEV 假資料模式停用。'); return }
  if (confirmAction.value === 'takeout' && selectedDetail.value) {
    const row = selectedDetail.value
    const detailCountBeforeDelete = carrierDetails.value.length
    const removed = snapshot.value.details.filter(item => item.carrierNo === selectedCarrierNo.value && item.contentId === row.contentId)
    recordTakeout(removed)
    snapshot.value.details = snapshot.value.details.filter(item => !(item.carrierNo === selectedCarrierNo.value && item.contentId === row.contentId))
    const lot = findLot(row.contentId)
    if (lot) lot.carrierNo = null
    if (detailCountBeforeDelete <= 1) selectedCarrier.value!.carrierState = 0
    selectedDetailId.value = ''
    persist()
    confirmAction.value = null
    showMessage('DEV 已模擬取出批號 ' + row.contentId + ' 的載具內容並記錄歷程。')
    return
  }
  if (confirmAction.value === 'clear') {
    const removed = carrierDetails.value
    const detailCountBeforeDelete = removed.length
    recordTakeout(removed)
    for (const row of removed) {
      const lot = findLot(row.contentId)
      if (lot) lot.carrierNo = null
    }
    snapshot.value.details = snapshot.value.details.filter(item => item.carrierNo !== selectedCarrierNo.value)
    if (detailCountBeforeDelete <= 1) selectedCarrier.value!.carrierState = 0
    selectedDetailId.value = ''
    persist()
    confirmAction.value = null
    showMessage('DEV 已模擬清空載具並記錄 ' + removed.length + ' 筆內容歷程。')
    return
  }
  confirmAction.value = null
}

function toggleCarrierState(): void {
  if (!dev.value || !selectedCarrier.value) { showError('DEV 假資料模式停用或尚未選取載具。'); return }
  selectedCarrier.value.carrierState = selectedCarrier.value.carrierState === 1 ? 0 : 1
  persist()
  showMessage('DEV 已模擬切換 CarrierState；此操作在 WinForms 預設隱藏並受權限控制。')
}
function toggleDetailFlag(): void {
  if (!dev.value || !selectedDetail.value) { showError('DEV 假資料模式停用或尚未選取內容。'); return }
  selectedDetail.value.flag = selectedDetail.value.flag === 1 ? 0 : 1
  persist()
  showMessage('DEV 已模擬切換內容 Flag；此操作在 WinForms 預設隱藏並受權限控制。')
}
function onHiddenStateActionsToggle(event: Event): void {
  revealWinFormsHiddenActions.value = (event.currentTarget as HTMLDetailsElement).open
}
</script>

<template>
  <section class="carrier-state-page">
    <header class="page-heading">
      <div><span class="function-code">WP302 · CARRIER STATE</span><h1>載具現況管理</h1></div>
      <div class="heading-tools">
        <span v-if="dev" class="dev-badge">DEV 假資料</span>
        <button v-if="dev" class="quiet-button" type="button" @click="resetDev">還原假資料</button>
      </div>
    </header>

    <div class="entry-toolbar">
      <fieldset class="lot-entry">
        <legend>載具內容</legend>
        <label for="wp302-lot">LotNo</label>
        <input id="wp302-lot" v-model="lotNoInput" class="kd-input" autocomplete="off" placeholder="輸入批號" @keydown.enter.prevent="putIn">
        <label for="wp302-position">Position</label>
        <input id="wp302-position" v-model="positionInput" class="kd-input position-input" inputmode="numeric" @keydown.enter.prevent="putIn">
      </fieldset>
      <div class="action-buttons">
        <button class="primary-button" type="button" :disabled="!dev" @click="putIn">放入 (Put In)</button>
        <button type="button" :disabled="!dev || !selectedDetail" @click="requestTakeout">取出 (Takes Out)</button>
        <button type="button" :disabled="!dev || !selectedCarrier" @click="requestClear">清空載具</button>
      </div>
    </div>

    <details v-if="dev" class="hidden-state-actions" :open="revealWinFormsHiddenActions" @toggle="onHiddenStateActionsToggle">
      <summary>WinForms 預設隱藏的狀態切換（受系統權限控制）</summary>
      <button type="button" :disabled="!selectedCarrier" @click="toggleCarrierState">Carrier Change：切換載具狀態</button>
      <button type="button" :disabled="!selectedDetail" @click="toggleDetailFlag">Lot Change：切換內容 Flag</button>
    </details>

    <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p>
    <p v-else-if="actionMessage" class="feedback success" role="status">{{ actionMessage }}</p>

    <section class="carrier-grid-panel carrier-master-panel" aria-label="載具現況">
      <div class="grid-title"><span>CarrierState</span><span v-if="selectedCarrier">目前選取：{{ selectedCarrier.carrierNo }} · {{ carrierDetails.length }}/{{ carrierCapacity }} 個位置</span></div>
      <div class="table-wrap">
        <table class="carrier-grid">
          <thead><tr><th>載具編號</th><th>載具分類</th><th>載具類別</th><th>載具狀態</th><th>說明</th></tr></thead>
          <tbody>
            <tr v-for="carrier in snapshot.carriers" :key="carrier.carrierNo" :class="{ selected: selectedCarrierNo === carrier.carrierNo }" @click="selectCarrier(carrier.carrierNo)">
              <td>{{ carrier.carrierNo }}</td><td>{{ carrier.carrierCategory }}</td><td>{{ carrier.carrierType }}</td>
              <td><span class="state-chip" :class="carrier.carrierState === 1 ? 'occupied' : 'empty'">{{ carrier.carrierState === 1 ? '使用中' : '空載' }}</span></td><td>{{ carrier.description }}</td>
            </tr>
            <tr v-if="!snapshot.carriers.length"><td class="empty-state" colspan="5">查無載具現況。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>資料總筆數 {{ snapshot.carriers.length }}</span><span>容量依 CarrierLayer × CarrierCapacity 計算</span></footer>
    </section>

    <section class="carrier-grid-panel carrier-detail-panel" aria-label="載具內容明細">
      <div class="grid-title"><span>CarrierState Detail</span><span>{{ selectedCarrierNo || '未選取載具' }}</span></div>
      <div class="table-wrap">
        <table class="carrier-grid detail-grid">
          <thead><tr><th>Position</th><th>LotNo</th><th>UserNo</th><th>ComponentNo</th></tr></thead>
          <tbody>
            <tr v-for="detail in carrierDetails" :key="detail.id" :class="{ selected: selectedDetailId === detail.id }" @click="selectDetail(detail)">
              <td class="numeric">{{ detail.carrierPosition }}</td><td>{{ detail.contentId }}</td><td>{{ detail.bcip }}</td><td>{{ detail.componentNo }}</td>
            </tr>
            <tr v-if="!carrierDetails.length"><td class="empty-state" colspan="4">此載具目前沒有內容。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>資料總筆數 {{ carrierDetails.length }}</span><span>剩餘位置 {{ availablePositions }}</span></footer>
    </section>

    <div class="page-footer-actions"><button class="quiet-button close-button" type="button" @click="emit('close')">關閉 (Close)</button></div>
    <footer class="status-bar"><span>{{ dev ? 'DEV 假資料' : '唯讀畫面' }}</span><span>操作者 {{ props.currentUserNo || 'DEVUSER' }}</span><span class="status-spacer">選取載具列可查看內容</span><span>{{ eventTime }}</span></footer>

    <div v-if="confirmAction" class="modal-backdrop" role="presentation" @click.self="confirmAction = null">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="wp302-confirm-title">
        <h2 id="wp302-confirm-title">{{ confirmAction === 'clear' ? '清空載具確認' : '取出載具內容確認' }}</h2>
        <p>{{ modalText }}</p>
        <div class="dialog-actions"><button type="button" @click="confirmAction = null">取消</button><button class="danger-button" type="button" @click="confirmRemoval">確定</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.carrier-state-page { min-height: 0; height: 100%; display: flex; flex-direction: column; gap: 8px; padding: 12px; color: #292929; background: #fff; overflow: auto; }
.page-heading { display: flex; align-items: center; justify-content: space-between; min-height: 38px; }
.page-heading h1 { display: inline; margin: 0 0 0 10px; font-size: 20px; font-weight: 600; }
.function-code { color: #6d6d6d; font-size: 11px; letter-spacing: .04em; }
.heading-tools { display: flex; align-items: center; gap: 8px; }
.dev-badge { border: 1px solid #b48a29; border-radius: 3px; padding: 4px 8px; color: #76580e; background: #fff3c7; font-size: 12px; }
.entry-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; min-height: 72px; }
.lot-entry { min-width: 380px; min-height: 66px; display: flex; align-items: center; gap: 8px; margin: 0; border: 1px solid #aaa; padding: 4px 10px 8px; }
.lot-entry legend { padding: 0 5px; color: #444; font-weight: 600; }
.lot-entry label { min-width: 52px; text-align: right; }
.kd-input { width: 175px; height: 30px; border: 1px solid #929292; border-radius: 0; padding: 4px 7px; color: #292929; background: #fff0ad; font: inherit; }
.kd-input:focus { outline: 2px solid #c99a30; outline-offset: 0; }
.kd-input.position-input { width: 82px; text-align: right; }
.action-buttons { display: flex; align-items: center; gap: 8px; }
.action-buttons button, .quiet-button, .hidden-state-actions button, .confirm-dialog button { min-height: 32px; border: 1px solid #aaa; border-radius: 3px; padding: 4px 10px; color: #333; background: #f8f8f8; font: inherit; white-space: nowrap; }
.action-buttons .primary-button { border-color: #d58b16; color: #fff; background: #f59a18; }
button:disabled { cursor: not-allowed; color: #999 !important; border-color: #ccc !important; background: #eee !important; }
.quiet-button { min-height: 28px; font-size: 12px; }
.hidden-state-actions { display: flex; align-items: center; gap: 8px; min-height: 30px; padding: 4px 8px; border: 1px solid #ddd; color: #666; font-size: 12px; }
.hidden-state-actions summary { cursor: pointer; margin-right: 8px; }
.carrier-grid-panel { min-height: 120px; display: flex; flex-direction: column; border: 1px solid #aaa; }
.carrier-master-panel { flex: 0 0 30%; min-height: 152px; }
.carrier-detail-panel { flex: 1 1 0; min-height: 200px; }
.grid-title { display: flex; justify-content: space-between; padding: 7px 9px; color: #444; background: #f3f3f3; border-bottom: 1px solid #aaa; font-weight: 600; }
.table-wrap { min-height: 0; flex: 1; overflow: auto; }
.carrier-grid { width: 100%; border-collapse: collapse; font-size: 12px; }
.carrier-grid th { position: sticky; top: 0; z-index: 1; padding: 7px 6px; border: 1px solid #c5c5c5; color: #303030; background: #e8e8e8; text-align: left; white-space: nowrap; }
.carrier-grid td { padding: 6px; border: 1px solid #d1d1d1; background: #fff0ad; white-space: nowrap; }
.carrier-grid tbody tr { cursor: pointer; }
.carrier-grid tr.selected td { outline: 1px solid #a77a16; outline-offset: -1px; background: #ffe18a; }
.carrier-grid .numeric { text-align: right; }
.state-chip { display: inline-block; min-width: 48px; border-radius: 2px; padding: 2px 6px; text-align: center; }
.state-chip.occupied { color: #7a4800; background: #ffe2a0; }
.state-chip.empty { color: #555; background: #e9e9e9; }
.empty-state { height: 58px; color: #777; text-align: center !important; background: #fafafa !important; }
.grid-footer { display: flex; justify-content: space-between; padding: 5px 9px; color: #666; border-top: 1px solid #c5c5c5; font-size: 12px; }
.feedback { flex: 0 0 auto; margin: 0; padding: 7px 10px; border: 1px solid; font-size: 13px; }
.feedback.error { border-color: #d5a2a2; color: #812c2c; background: #fff0f0; }
.feedback.success { border-color: #b5c9a8; color: #3e6631; background: #f1faeb; }
.status-bar { min-height: 24px; display: flex; align-items: center; gap: 16px; padding: 2px 7px; border: 1px solid #bbb; color: #555; background: #f5f5f5; font-size: 11px; }
.page-footer-actions { display: flex; justify-content: flex-end; }
.close-button { min-width: 88px; }
.status-spacer { flex: 1; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 18px; background: rgb(0 0 0 / 34%); }
.confirm-dialog { width: min(430px, 100%); border: 1px solid #aaa; border-radius: 4px; padding: 18px; background: #fff; box-shadow: 0 8px 30px rgb(0 0 0 / 20%); }
.confirm-dialog h2 { margin: 0 0 10px; font-size: 17px; }
.confirm-dialog p { margin: 0; line-height: 1.6; }
.dialog-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 18px; }
.confirm-dialog .danger-button { border-color: #a54236; color: #fff; background: #b64a42; }
@media (max-width: 940px) {
  .carrier-state-page { height: auto; min-height: 100%; }
  .entry-toolbar { align-items: stretch; flex-direction: column; }
  .lot-entry { min-width: 0; flex-wrap: wrap; }
  .lot-entry label { min-width: auto; }
  .action-buttons { flex-wrap: wrap; }
  .carrier-master-panel { flex-basis: 210px; }
  .carrier-detail-panel { min-height: 250px; }
  .carrier-grid { min-width: 650px; }
  .hidden-state-actions { flex-wrap: wrap; }
  .status-bar { flex-wrap: wrap; }
}
</style>
