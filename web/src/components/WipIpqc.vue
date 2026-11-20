<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Abnormal = {
  abnormalNo: string
  abnormalName: string
  linkName: string
  priority: number
  positions: string[]
  selectedPositions: string[]
  selected: boolean
}
type Operation = { opNo: string; opName: string; opClass: number; issueState: number; abnormalities: Abnormal[] }
type AbnormalRecord = { recordNo: string; lotSerial: string; opNo: string; abnormalNo: string; position: string | null; abnormalQty: number; userNo: string | null; shiftNo: string | null; eventTime: string | null }
type PickerMode = 'operation' | 'position' | null

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes-dev-mock:wip-ipqc:abnormal-records:v1'

const operationFixtures: Operation[] = [
  {
    opNo: 'DEV-OP-ETCH-20', opName: 'DEV 蝕刻檢查', opClass: 1, issueState: 2,
    abnormalities: [
      { abnormalNo: 'DEV-AB-CHIP', abnormalName: 'DEV 缺角／崩邊', linkName: 'DEV-LINK-CUT', priority: 1, positions: ['A1', 'A2', 'B1'], selectedPositions: [], selected: false },
      { abnormalNo: 'DEV-AB-CONTAM', abnormalName: 'DEV 表面污染', linkName: 'DEV-LINK-CLEAN', priority: 2, positions: [], selectedPositions: [], selected: false },
      { abnormalNo: 'DEV-AB-SCRATCH', abnormalName: 'DEV 表面刮傷', linkName: 'DEV-LINK-MEAS', priority: 5, positions: ['前端', '中央', '尾端'], selectedPositions: [], selected: false },
    ],
  },
  {
    opNo: 'DEV-OP-PACK-10', opName: 'DEV 包裝檢查', opClass: 1, issueState: 2,
    abnormalities: [
      { abnormalNo: 'DEV-AB-LABEL', abnormalName: 'DEV 標籤異常', linkName: 'DEV-LINK-PACK', priority: 1, positions: [], selectedPositions: [], selected: false },
      { abnormalNo: 'DEV-AB-PACK', abnormalName: 'DEV 包材破損', linkName: 'DEV-LINK-PACK', priority: 2, positions: ['左側', '右側'], selectedPositions: [], selected: false },
    ],
  },
]

const operationNo = ref('')
const operationName = ref('')
const lotNo = ref('')
const errorNo = ref('')
const abnormalities = ref<Abnormal[]>([])
const records = ref<AbnormalRecord[]>([])
const pickerMode = ref<PickerMode>(null)
const pickerSearch = ref('')
const activeAbnormalNo = ref('')
const pendingPositions = ref<string[]>([])
const errorText = ref('')
const message = ref('請選擇 OP，再輸入 Lot No 並勾選不良現象。')
const currentTime = ref('')
const submitted = ref(false)

const filteredOperations = computed(() => operationFixtures
  .filter(item => item.opClass === 1 && item.issueState === 2)
  .filter(item => !pickerSearch.value || (item.opNo + ' ' + item.opName).toLowerCase().includes(pickerSearch.value.trim().toLowerCase())))
const activeAbnormal = computed(() => abnormalities.value.find(item => item.abnormalNo === activeAbnormalNo.value) ?? null)
const selectedRows = computed(() => abnormalities.value.flatMap(item => {
  if (!item.selected) return []
  if (item.positions.length) return item.selectedPositions.map(position => ({ item, position }))
  return [{ item, position: null as string | null }]
}))

function readRecords(): void {
  if (!dev.value) return
  try {
    const raw = window.localStorage.getItem(storageKey)
    records.value = raw ? JSON.parse(raw) as AbnormalRecord[] : []
  } catch {
    records.value = []
  }
}

function persistRecords(): void {
  if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(records.value))
}

function resetMessages(): void {
  errorText.value = ''
  message.value = ''
}

function openOperationPicker(): void {
  if (!dev.value || submitted.value) return
  pickerSearch.value = ''
  pickerMode.value = 'operation'
}

function chooseOperation(item: Operation): void {
  if (item.opNo !== operationNo.value) {
    operationNo.value = item.opNo
    operationName.value = item.opName
    abnormalities.value = item.abnormalities
      .map(row => ({ ...row, selectedPositions: [], selected: false }))
      .sort((left, right) => left.abnormalName.localeCompare(right.abnormalName, 'zh-Hant'))
    submitted.value = false
    message.value = '已載入 ' + abnormalities.value.length + ' 項 OP 不良現象。'
  }
  errorText.value = ''
  pickerMode.value = null
}

function updateLotNo(): void {
  lotNo.value = lotNo.value.trim().toUpperCase()
  errorText.value = ''
}

function handleLotEnter(): void {
  updateLotNo()
  // The legacy form preloads all LotState rows here, but its search dialog is commented out and the result is unused.
  message.value = lotNo.value ? 'Lot No 已輸入；DEV 頁不載入整張 LotState 清單。' : '請輸入 Lot No。'
}

function toggleAbnormal(item: Abnormal): void {
  if (item.positions.length) {
    activeAbnormalNo.value = item.abnormalNo
    pendingPositions.value = [...item.selectedPositions]
    pickerSearch.value = ''
    pickerMode.value = 'position'
    return
  }
  item.selected = !item.selected
  errorText.value = ''
}

function togglePendingPosition(position: string): void {
  pendingPositions.value = pendingPositions.value.includes(position)
    ? pendingPositions.value.filter(value => value !== position)
    : [...pendingPositions.value, position]
}

function confirmPositions(): void {
  if (!activeAbnormal.value) return
  activeAbnormal.value.selectedPositions = [...pendingPositions.value]
  activeAbnormal.value.selected = pendingPositions.value.length > 0
  pickerMode.value = null
  errorText.value = ''
}

function cancelPicker(): void {
  pickerMode.value = null
  activeAbnormalNo.value = ''
  pendingPositions.value = []
  pickerSearch.value = ''
}

function confirm(): void {
  errorText.value = ''
  if (!operationNo.value) {
    errorText.value = '請先選擇 OP No。'
    return
  }
  updateLotNo()
  if (!lotNo.value) {
    errorText.value = 'Lot No 不可空白。'
    return
  }
  if (!selectedRows.value.length) {
    errorText.value = '請至少選擇一項不良現象或位置。'
    return
  }
  if (!dev.value) {
    errorText.value = '目前頁面僅提供 DEV 假資料預覽。'
    return
  }

  const submittedAt = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of selectedRows.value) {
    records.value.push({
      recordNo: 'DEV-IPQC-' + Date.now() + '-' + records.value.length,
      lotSerial: lotNo.value,
      opNo: operationNo.value,
      abnormalNo: row.item.abnormalNo,
      position: row.position,
      abnormalQty: 0,
      userNo: null,
      shiftNo: null,
      eventTime: null,
    })
  }
  persistRecords()
  submitted.value = true
  currentTime.value = submittedAt
  message.value = 'DEV 已模擬新增 ' + selectedRows.value.length + ' 筆不良紀錄。'
}

function setClock(): void {
  currentTime.value = new Date().toLocaleTimeString('zh-TW', { hour12: false })
}

onMounted(() => {
  setClock()
  readRecords()
})
</script>

<template>
  <section class="wp206-page">
    <div v-if="dev" class="dev-banner">DEV 假資料預覽；送出結果只寫入此瀏覽器 localStorage。</div>
    <h1>計數不良原因收集 <small>IPQC</small></h1>
    <p v-if="errorText" class="message error-message" role="alert">{{ errorText }}</p>
    <p v-else-if="submitted" class="message success-message" role="status">{{ message }}</p>

    <div class="top-fields">
      <label for="wp206-op-no">OP No</label>
      <input id="wp206-op-no" class="mes-input readonly" :value="operationNo" readonly>
      <button type="button" class="mes-button lookup-button" :disabled="!dev || submitted" aria-label="選擇作業站" @click="openOperationPicker">...</button>
      <span class="operation-name">{{ operationName }}</span>
      <label for="wp206-lot-no">Lot No</label>
      <input id="wp206-lot-no" v-model="lotNo" class="mes-input editable lot-input" :disabled="!dev || submitted" autocomplete="off" @blur="updateLotNo" @keydown.enter.prevent="handleLotEnter">
    </div>

    <fieldset class="abnormal-frame">
      <div class="abnormal-panel" role="group" aria-label="OP 不良現象">
        <div v-for="(item, index) in abnormalities" :key="item.abnormalNo" class="abnormal-row" :style="{ top: (10 + index * 45) + 'px' }">
          <input class="abnormal-check" type="checkbox" :checked="item.selected" disabled :aria-label="item.abnormalName">
          <button type="button" class="abnormal-button" :disabled="!dev || submitted" @click="toggleAbnormal(item)">{{ item.abnormalName }}</button>
          <span class="abnormal-meta">{{ item.abnormalNo }}</span>
        </div>
        <div v-if="operationNo && !abnormalities.length" class="empty-abnormal">此 OP 沒有設定不良現象。</div>
        <div v-if="!operationNo" class="empty-abnormal">請先選擇 OP No。</div>
      </div>
      <div class="error-no-row"><label for="wp206-error-no">Error No</label><input id="wp206-error-no" v-model="errorNo" class="mes-input" :disabled="!dev || submitted"></div>
    </fieldset>

    <div class="action-row">
      <span class="action-note">{{ selectedRows.length ? '選取 ' + selectedRows.length + ' 筆位置／不良項目' : '尚未勾選不良項目' }}</span>
      <button type="button" class="mes-button confirm-button" :disabled="!dev || submitted" @click="confirm">Confirm(&amp;C)</button>
      <button type="button" class="mes-button" @click="emit('close')">Close (&amp;X)</button>
    </div>
    <div class="statusbar"><span></span><span></span><span></span><span>{{ currentTime }}</span></div>

    <div v-if="pickerMode" class="modal-backdrop" @click.self="cancelPicker">
      <section v-if="pickerMode === 'operation'" class="picker-dialog operation-dialog" role="dialog" aria-modal="true" aria-label="OP No">
        <header><strong>OP No</strong><button type="button" class="mes-button" @click="cancelPicker">Close</button></header>
        <input v-model="pickerSearch" class="mes-input picker-search" placeholder="OP No / OP Name">
        <div class="picker-list">
          <button v-for="item in filteredOperations" :key="item.opNo" type="button" class="picker-row" @click="chooseOperation(item)"><strong>{{ item.opNo }}</strong><span>{{ item.opName }}</span></button>
          <div v-if="!filteredOperations.length" class="empty-picker">沒有符合的作業站。</div>
        </div>
      </section>
      <section v-else class="picker-dialog position-dialog" role="dialog" aria-modal="true" aria-label="Position">
        <strong>Position</strong>
        <div class="position-list">
          <label v-for="position in activeAbnormal?.positions || []" :key="position" class="position-option">
            <input type="checkbox" :checked="pendingPositions.includes(position)" @change="togglePendingPosition(position)">
            <span>{{ position }}</span>
          </label>
          <div v-if="!activeAbnormal?.positions.length" class="empty-picker">沒有位置可選。</div>
        </div>
        <footer><button type="button" class="mes-button confirm-button" @click="confirmPositions">Confirm</button><button type="button" class="mes-button" @click="cancelPicker">Cancel</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.wp206-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif;box-sizing:border-box}.dev-banner{padding:7px 10px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.wp206-page h1{margin:0;padding:0 2px;font-size:18px;font-weight:600}.wp206-page h1 small{margin-left:8px;color:#777;font-size:13px;font-weight:400}.message{margin:0;padding:7px 10px;border:1px solid}.error-message{border-color:#d8aaa6;background:#fff0ef;color:#9c241d}.success-message{border-color:#a9c6a2;background:#f1faed;color:#2e6332}.top-fields{display:grid;grid-template-columns:82px 176px 28px minmax(100px,1fr);grid-template-rows:28px 28px;align-items:center;gap:1px 6px}.top-fields label{text-align:right;padding-right:3px}.top-fields label[for=wp206-lot-no]{grid-column:1;grid-row:2}.top-fields input#wp206-lot-no{grid-column:2;grid-row:2}.operation-name{color:#555}.mes-input{height:25px;min-width:0;padding:3px 6px;border:1px solid #888;border-radius:2px;background:#fff;color:#333;font:inherit;box-sizing:border-box}.mes-input.editable{background:#fff0b3}.mes-input.readonly{background:#eee;color:#555}.mes-button{min-height:29px;padding:3px 11px;border:1px solid #b9b9b9;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#333;font:inherit;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#fff8e6}.mes-button:disabled{color:#999;cursor:default}.lookup-button{min-width:28px;padding:1px 4px}.abnormal-frame{position:relative;flex:1;min-height:180px;margin:0;padding:8px 8px 44px;border:1px solid #bdbdbd;background:#fff;box-sizing:border-box}.abnormal-panel{position:absolute;inset:8px 8px 44px;overflow:auto;border:1px solid #aaa;background:#fff}.abnormal-row{position:absolute;left:68px;right:8px;height:38px;display:grid;grid-template-columns:22px 180px minmax(90px,1fr);align-items:center;gap:8px}.abnormal-check{width:17px;height:17px;justify-self:center}.abnormal-button{height:35px;padding:3px 10px;border:1px solid #b7b7b7;border-radius:2px;background:linear-gradient(#fff,#f2f2f2);color:#333;text-align:center;font:16px "Times New Roman","Microsoft JhengHei",serif;cursor:pointer}.abnormal-button:hover:not(:disabled){background:#fff2cc}.abnormal-button:disabled{color:#999}.abnormal-meta{color:#777;font-size:11px}.empty-abnormal{padding:24px;color:#777;text-align:center}.error-no-row{position:absolute;left:8px;right:8px;bottom:9px;height:25px;display:flex;align-items:center;gap:8px}.error-no-row label{width:88px}.error-no-row .mes-input{width:136px}.action-row{display:flex;align-items:center;justify-content:flex-end;gap:8px}.action-note{margin-right:auto;color:#777}.confirm-button{min-width:88px;color:#fff;background:#f3a62a;border-color:#e99512}.confirm-button:hover:not(:disabled){background:#e99a1b}.statusbar{height:22px;display:grid;grid-template-columns:110px 150px 1fr 90px;align-items:center;border:1px solid #aaa;background:#eee;color:#444;font-size:12px}.statusbar span{height:100%;padding:2px 5px;border-right:1px solid #ccc;text-align:right}.statusbar span:last-child{border-right:0}.modal-backdrop{position:fixed;inset:0;z-index:1300;display:grid;place-items:center;padding:18px;background:#0005}.picker-dialog{display:flex;flex-direction:column;gap:9px;padding:13px;background:#fff;border:1px solid #999;box-shadow:0 8px 28px #0004;box-sizing:border-box}.operation-dialog{width:min(560px,94vw);max-height:84vh}.position-dialog{width:min(232px,94vw);height:min(300px,90vh)}.picker-dialog header{display:flex;align-items:center;justify-content:space-between}.picker-search{width:100%}.picker-list,.position-list{flex:1;min-height:0;overflow:auto;border:1px solid #ccc}.picker-row{width:100%;display:grid;grid-template-columns:170px 1fr;gap:8px;padding:8px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;color:#333;cursor:pointer}.picker-row:hover{background:#fff2cc}.empty-picker{padding:18px;text-align:center;color:#777}.position-option{min-height:50px;display:flex;align-items:center;gap:12px;padding:0 14px;border-bottom:1px solid #ddd;font-size:15px}.position-option input{width:17px;height:17px}.position-dialog footer{display:flex;justify-content:space-between;gap:8px}.position-dialog footer .mes-button{width:80px}@media(max-width:660px){.top-fields{grid-template-columns:72px minmax(90px,1fr) 28px;grid-template-rows:28px 28px 28px}.operation-name{grid-column:2/4;grid-row:2}.top-fields label[for=wp206-lot-no]{grid-row:3}.top-fields input#wp206-lot-no{grid-row:3}.abnormal-row{left:5px;grid-template-columns:22px minmax(140px,180px) minmax(60px,1fr)}.statusbar{grid-template-columns:50px 50px 1fr 75px}.action-row{flex-wrap:wrap}.action-note{width:100%}}
</style>
