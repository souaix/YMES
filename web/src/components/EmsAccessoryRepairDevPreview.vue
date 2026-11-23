<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface RepairRow {
  serial: string
  accessoryNo: string
  version: string
  state: number
  stateName: string
  repairCycle: number | null
  scrapType: number | null
  scrapLife: number | null
  repairQty: number
  totalUsedQty: number
  accumulateQty: number
  newAccumulateQty: number
  spareQty: number
  timeCycleStart: string
  selected: boolean
  scrapTransitionBlocked: boolean
}

interface CatalogItem {
  serial: string
  accessoryNo: string
  version: string
  issueState: number
  state: number
  stateName: string
  repairCycle: number | null
  scrapType: number | null
  scrapLife: number | null
  repairQty: number
  totalUsedQty: number
  accumulateQty: number
  spareQty: number
  timeCycleStart: string
  scrapTransitionBlocked: boolean
}

type Operation = 'adjust' | 'zero'

function localDate(offsetDays: number, hour: number, minute = 0): string {
  const value = new Date()
  value.setDate(value.getDate() - offsetDays)
  value.setHours(hour, minute, 0, 0)
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

function currentLocalDateTime(): string {
  const value = new Date()
  const pad = (part: number) => String(part).padStart(2, '0')
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

// All records below are local DEV fixtures. They do not come from MES or a database.
const catalog: CatalogItem[] = [
  { serial: 'DEV-REPAIR-001', accessoryNo: 'PRB-01-008', version: 'A02', issueState: 2, state: 1, stateName: '庫存', repairCycle: 250, scrapType: 0, scrapLife: 4, repairQty: 3, totalUsedQty: 420, accumulateQty: 120, spareQty: 130, timeCycleStart: localDate(8, 9, 10), scrapTransitionBlocked: false },
  { serial: 'DEV-REPAIR-002', accessoryNo: 'PRB-01-008', version: 'A01', issueState: 2, state: 1, stateName: '庫存', repairCycle: 250, scrapType: 0, scrapLife: 4, repairQty: 1, totalUsedQty: 180, accumulateQty: 0, spareQty: 250, timeCycleStart: '', scrapTransitionBlocked: false },
  { serial: 'DEV-REPAIR-003', accessoryNo: 'MOLD-02-015', version: 'B01', issueState: 2, state: 3, stateName: '維修中', repairCycle: 500, scrapType: 1, scrapLife: 1500, repairQty: 2, totalUsedQty: 1525, accumulateQty: 175, spareQty: 325, timeCycleStart: localDate(3, 14, 25), scrapTransitionBlocked: true },
  { serial: 'DEV-REPAIR-004', accessoryNo: 'GAUGE-09-002', version: 'A03', issueState: 2, state: 1, stateName: '庫存', repairCycle: 1000, scrapType: 1, scrapLife: 5000, repairQty: 0, totalUsedQty: 800, accumulateQty: 250, spareQty: 750, timeCycleStart: '', scrapTransitionBlocked: false },
  { serial: 'DEV-REPAIR-005', accessoryNo: 'SEAL-OVN-005', version: 'C01', issueState: 2, state: 5, stateName: '報廢', repairCycle: 100, scrapType: 0, scrapLife: 3, repairQty: 4, totalUsedQty: 360, accumulateQty: 0, spareQty: 100, timeCycleStart: '', scrapTransitionBlocked: false },
  { serial: 'DEV-REPAIR-006', accessoryNo: 'PRB-01-099', version: 'A01', issueState: 0, state: 1, stateName: '庫存', repairCycle: 250, scrapType: 0, scrapLife: 4, repairQty: 0, totalUsedQty: 0, accumulateQty: 0, spareQty: 250, timeCycleStart: '', scrapTransitionBlocked: false },
  { serial: 'DEV-REPAIR-007', accessoryNo: 'JIG-04-021', version: 'A01', issueState: 2, state: 1, stateName: '庫存', repairCycle: 300, scrapType: 0, scrapLife: 4, repairQty: 4, totalUsedQty: 960, accumulateQty: 60, spareQty: 240, timeCycleStart: localDate(1, 11, 40), scrapTransitionBlocked: false },
]

const accessoryInput = ref('')
const repairNo = ref('')
const description = ref('')
const rows = ref<RepairRow[]>([])
const pickerOpen = ref(false)
const pickerSelection = ref<Set<string>>(new Set())
const confirmOperation = ref<Operation | null>(null)
const message = ref('輸入配件編號後按 Enter，或用 … 多選 IssueState=2 的配件。DEV 假資料只保存在本頁。')
const operationNumber = ref(0)
const applyState = 2
const scrapState = 5

const pickerOptions = computed(() => [...new Set(catalog.filter((item) => item.issueState === 2).map((item) => item.accessoryNo))].sort())
const selectedCount = computed(() => rows.value.filter((row) => row.selected).length)
const operationTitle = computed(() => confirmOperation.value === 'adjust' ? '確認調帳' : '歸零')

function toRepairRow(item: CatalogItem): RepairRow {
  return {
    ...item,
    newAccumulateQty: item.accumulateQty,
    selected: true,
  }
}

function loadAccessoryNumbers(values: string[], pickerOnly = false) {
  const requested = [...new Set(values.map((value) => value.trim().toUpperCase()).filter(Boolean))]
  if (!requested.length) {
    message.value = '請輸入至少一個配件編號。'
    return
  }
  const invalid = requested.filter((accessoryNo) => !catalog.some((item) => item.accessoryNo === accessoryNo && (!pickerOnly || item.issueState === 2)))
  if (invalid.length) {
    message.value = pickerOnly
      ? `找不到可選取的 IssueState=2 配件：${invalid.join(', ')}。`
      : `找不到 DEV 配件現況：${invalid.join(', ')}。`
    return
  }
  const seen = new Set(rows.value.map((row) => row.serial))
  const added = catalog.filter((item) => requested.includes(item.accessoryNo) && (!pickerOnly || item.issueState === 2) && !seen.has(item.serial))
  rows.value.push(...added.map(toRepairRow))
  accessoryInput.value = requested.join(', ')
  message.value = added.length ? `已載入 ${added.length} 筆現況；查詢會同時帶出編號的各版本。` : '所選配件現況已在清單中。'
}

function loadByEnter() {
  if (/[\s,;]+/.test(accessoryInput.value.trim())) {
    message.value = 'Enter／Load 一次輸入一個配件編號；多選請按 …。'
    return
  }
  loadAccessoryNumbers([accessoryInput.value])
}

function openPicker() {
  pickerSelection.value = new Set()
  pickerOpen.value = true
}

function togglePickerSelection(accessoryNo: string) {
  const next = new Set(pickerSelection.value)
  if (next.has(accessoryNo)) next.delete(accessoryNo)
  else next.add(accessoryNo)
  pickerSelection.value = next
}

function loadPickerSelection() {
  loadAccessoryNumbers([...pickerSelection.value], true)
  pickerOpen.value = false
}

function reserveRepairNo() {
  operationNumber.value += 1
  const today = new Date()
  const stamp = `${today.getFullYear()}${String(today.getMonth() + 1).padStart(2, '0')}${String(today.getDate()).padStart(2, '0')}`
  repairNo.value = `DEV-AR-${stamp}-${String(operationNumber.value).padStart(3, '0')}`
  message.value = '已保留 DEV 維修單號；這不會呼叫正式序號服務。'
}

function isValidQty(value: number): boolean {
  return Number.isFinite(value) && value >= 0 && value <= 100_000_000
}

function validateOperation(): RepairRow[] | null {
  if (!repairNo.value.trim()) {
    message.value = '請先按單號 … 取得 DEV 維修單號。'
    return null
  }
  if (!rows.value.length) {
    message.value = '請先載入配件現況。'
    return null
  }
  const selected = rows.value.filter((row) => row.selected)
  if (!selected.length) {
    message.value = '至少勾選一筆配件。'
    return null
  }
  const invalidQty = rows.value.find((row) => !isValidQty(row.newAccumulateQty))
  if (invalidQty) {
    message.value = `${invalidQty.accessoryNo} 的 Accumulate Qty 必須介於 0 與 100,000,000；來源會檢查清單內所有列。`
    return null
  }
  const applied = selected.find((row) => row.state === applyState)
  if (applied) {
    message.value = `${applied.accessoryNo} 為上機狀態（ES103 Apply），不可執行壽命維護。`
    return null
  }
  const scrapped = selected.find((row) => row.state === scrapState)
  if (scrapped) {
    message.value = `${scrapped.accessoryNo} 已是報廢狀態；Legacy Core 會中止本次整批更新。`
    return null
  }
  return selected
}

function opensAdjustConfirm() {
  confirmOperation.value = 'adjust'
}

function returnSelectedToZero() {
  // WinForms ReturnZero runs the validation and submit path without an extra confirmation dialog.
  const selected = validateOperation()
  if (selected) applyOperation('zero', selected)
}

function exceedsScrapLife(row: RepairRow): boolean {
  if (row.scrapType === 0 && row.scrapLife !== null) return row.repairQty + 1 > row.scrapLife
  if (row.scrapType === 1 && row.scrapLife !== null) return row.totalUsedQty > row.scrapLife
  return false
}

function applyOperation(operation: Operation, knownSelection?: RepairRow[]) {
  const selected = knownSelection ?? validateOperation()
  if (!selected) return
  const newlyScrapped = selected.filter(exceedsScrapLife)
  const blocked = newlyScrapped.find((row) => row.scrapTransitionBlocked)
  if (blocked) {
    message.value = `${blocked.accessoryNo} 達報廢門檻，但 DEV 互斥設定禁止由「${blocked.stateName}」轉到報廢；本次不套用。`
    confirmOperation.value = null
    return
  }

  const now = currentLocalDateTime()
  for (const row of selected) {
    const originalQty = row.accumulateQty
    const newQty = operation === 'zero' ? 0 : row.newAccumulateQty
    const qtyDifference = newQty - originalQty
    row.spareQty = Math.max(0, row.spareQty - qtyDifference)
    if (operation === 'adjust') {
      row.totalUsedQty += qtyDifference
    } else {
      row.repairQty += 1
      row.timeCycleStart = now
      // Legacy sets the total-used delta to zero for ReturnZero.
    }
    row.accumulateQty = newQty
    row.newAccumulateQty = newQty
    if (newlyScrapped.some((item) => item.serial === row.serial)) {
      row.state = scrapState
      row.stateName = '報廢'
    }
  }

  const actionLabel = operation === 'adjust' ? '調帳' : '歸零維修'
  message.value = `${repairNo.value}：${selected.length} 筆 ${actionLabel} DEV 假套用完成；${newlyScrapped.length} 筆依 ES103 Scrap 狀態模擬轉報廢。`
  repairNo.value = ''
  description.value = ''
  confirmOperation.value = null
}
</script>

<template>
  <section v-if="dev" class="accessory-repair-page" aria-label="配件壽命維護 DEV">
    <header class="page-heading">
      <div class="title-line"><h1>配件壽命維護</h1><span class="dev-badge">DEV 假資料</span></div>
      <p>多選配件後調整累積用量，或執行維修歸零；所有操作只改變本頁記憶體資料。</p>
    </header>

    <section class="repair-order" aria-label="維修單資料">
      <label class="form-field repair-number"><span>Accessory Repair No</span><div class="picker-input"><input v-model="repairNo" readonly aria-label="Accessory Repair No" /><button type="button" aria-label="保留 DEV 維修單號" @click="reserveRepairNo">…</button></div></label>
      <label class="form-field accessory-number"><span>Accessory No</span><div class="picker-input"><input v-model="accessoryInput" aria-label="Accessory No" placeholder="輸入單一編號；多選請按 …" @keydown.enter.prevent="loadByEnter" @blur="accessoryInput = accessoryInput.toUpperCase()" /><button type="button" aria-label="多選配件" @click="openPicker">…</button></div></label>
      <button type="button" class="load-button" @click="loadByEnter">Load</button>
    </section>

    <section class="grid-section" aria-label="配件現況與壽命資料">
      <div class="section-heading"><h2>Accessory State / Life</h2><span>{{ selectedCount }} selected / {{ rows.length }} rows</span></div>
      <div class="table-wrap">
        <table class="repair-table">
          <thead><tr><th>Select</th><th>Accessory No</th><th>Repair Cycle</th><th>Scrap Type</th><th>Scrap Life</th><th>Acc Repair Qty</th><th>Acc Total Used Qty</th><th>Accumulate Qty (edit)</th><th>Time Cycle Start (edit)</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="row.serial" :class="{ 'scrapped-row': row.state === scrapState }">
              <td class="select-cell"><input v-model="row.selected" type="checkbox" :aria-label="`選取 ${row.accessoryNo} ${row.version}`" /></td>
              <td><strong>{{ row.accessoryNo }}</strong><small>v{{ row.version }} · {{ row.stateName }}</small></td>
              <td class="number-cell">{{ row.repairCycle ?? '—' }}</td>
              <td>{{ row.scrapType === 0 ? '重複使用次數' : row.scrapType === 1 ? '總使用數量' : '—' }}</td>
              <td class="number-cell">{{ row.scrapLife ?? '—' }}</td>
              <td class="number-cell">{{ row.repairQty }}</td>
              <td class="number-cell">{{ row.totalUsedQty }}</td>
              <td><input v-model.number="row.newAccumulateQty" type="number" min="0" max="100000000" step="0.0001" :aria-label="`${row.accessoryNo} Accumulate Qty`" /></td>
              <td><input v-model="row.timeCycleStart" type="datetime-local" step="1" :aria-label="`${row.accessoryNo} Time Cycle Start`" /></td>
            </tr>
            <tr v-if="rows.length === 0"><td class="empty-cell" colspan="9">先輸入一個配件編號，或按 … 選取 IssueState=2 的配件。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="description-row" aria-label="維修說明">
      <label for="repair-description">Description</label>
      <textarea id="repair-description" v-model="description" rows="2" placeholder="輸入維修或調帳說明" />
    </section>

    <footer class="page-footer">
      <p role="status">{{ message }}</p>
      <div class="actions"><button type="button" class="secondary-button" @click="returnSelectedToZero">Return Zero</button><button type="button" class="primary-button" @click="opensAdjustConfirm">Adjust</button><button type="button" class="secondary-button" @click="emit('close')">Close (X)</button></div>
    </footer>

    <div v-if="pickerOpen" class="modal-backdrop" @click.self="pickerOpen = false">
      <section class="picker-dialog" role="dialog" aria-modal="true" aria-label="選擇配件">
        <header><div><h2>選擇配件</h2><p>只列出 IssueState=2 的 DEV 主檔。選中編號會載入該編號的所有版本。</p></div><button type="button" class="icon-button" aria-label="關閉" @click="pickerOpen = false">×</button></header>
        <div class="option-list">
          <label v-for="option in pickerOptions" :key="option" class="option-row"><input type="checkbox" :checked="pickerSelection.has(option)" @change="togglePickerSelection(option)" /><span>{{ option }}</span><small>{{ catalog.filter((item) => item.accessoryNo === option && item.issueState === 2).length }} version(s)</small></label>
        </div>
        <footer><span>{{ pickerSelection.size }} selected</span><div><button type="button" class="secondary-button" @click="pickerOpen = false">Cancel</button><button type="button" class="primary-button" @click="loadPickerSelection">Load Selected</button></div></footer>
      </section>
    </div>

    <div v-if="confirmOperation" class="modal-backdrop" @click.self="confirmOperation = null">
      <section class="confirm-dialog" role="dialog" aria-modal="true" :aria-label="operationTitle">
        <h2>{{ operationTitle }}?</h2>
        <p>將對已勾選的 {{ selectedCount }} 筆 DEV 配件套用壽命數量與維修狀態規則。</p>
        <p v-if="description.trim()">說明：{{ description }}</p>
        <footer><button type="button" class="secondary-button" @click="confirmOperation = null">Cancel</button><button type="button" class="primary-button" @click="applyOperation('adjust')">Adjust</button></footer>
      </section>
    </div>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.accessory-repair-page { display: flex; flex-direction: column; gap: 9px; height: 100%; min-height: 0; padding: 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-heading { flex: none; }
.title-line { display: flex; align-items: center; gap: 9px; }
h1 { margin: 0; font-size: 19px; }
.page-heading p { margin: 4px 0 0; color: #777366; font-size: 12px; }
.dev-badge { padding: 3px 8px; border-radius: 12px; background: #fff0bd; color: #6d5713; font-size: 10px; font-weight: 700; }
.repair-order { display: flex; flex: none; align-items: end; gap: 12px; padding: 9px; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.form-field { display: grid; gap: 4px; color: #555247; font-size: 11px; font-weight: 600; }
.repair-number { width: min(290px, 34%); }.accessory-number { flex: 1; min-width: 220px; }
.picker-input { display: flex; min-width: 0; }
.picker-input input { flex: 1; min-width: 0; height: 27px; padding: 3px 6px; border: 1px solid #c8c2b4; border-radius: 3px; background: #fff; color: #26251f; font: inherit; font-size: 11px; }
.picker-input input[readonly] { background: #f1efe8; }
.picker-input button { width: 30px; margin-left: 4px; border: 1px solid #b9b4a6; border-radius: 3px; background: #f8f6ef; color: #37352f; cursor: pointer; }
.load-button, .primary-button, .secondary-button { min-height: 28px; padding: 4px 11px; border: 1px solid #3b3930; border-radius: 4px; font: inherit; font-size: 11px; cursor: pointer; }
.load-button, .primary-button { background: #3b3930; color: white; }.secondary-button { background: #fffdf7; color: #3b3930; }
.grid-section { display: flex; flex: 1 1 auto; flex-direction: column; min-height: 190px; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.section-heading { display: flex; flex: none; align-items: center; justify-content: space-between; padding: 6px 9px; border-bottom: 1px solid #e7e3d8; background: #ece9df; }
.section-heading h2 { margin: 0; color: #555247; font-size: 11px; }.section-heading span { color: #777366; font-size: 10px; }
.table-wrap { min-height: 0; overflow: auto; }.repair-table { width: max-content; min-width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
thead { position: sticky; top: 0; z-index: 1; background: #f5f3eb; color: #625f55; text-align: left; }
th, td { padding: 6px 8px; border-bottom: 1px solid #eeeae0; } th { font-size: 10px; font-weight: 700; }
td small { display: block; margin-top: 2px; color: #837e70; font-size: 9px; }.select-cell { width: 42px; text-align: center; }.number-cell { text-align: right; font-variant-numeric: tabular-nums; }
.repair-table input[type="number"], .repair-table input[type="datetime-local"] { height: 25px; padding: 2px 5px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #26251f; font: inherit; font-size: 10px; }
.repair-table input[type="number"] { width: 104px; text-align: right; }.repair-table input[type="datetime-local"] { width: 172px; }
.scrapped-row { color: #777366; background: #f5f1eb; }.empty-cell { padding: 20px 8px; color: #8a867a; text-align: center; }
.description-row { display: grid; flex: none; grid-template-columns: 108px 1fr; align-items: start; gap: 8px; color: #555247; font-size: 11px; }
.description-row label { padding-top: 5px; text-align: right; }.description-row textarea { min-height: 44px; padding: 6px; resize: vertical; border: 1px solid #c8c2b4; border-radius: 3px; background: #fffdf7; color: #26251f; font: inherit; font-size: 11px; }
.page-footer { display: flex; flex: none; align-items: center; justify-content: space-between; gap: 10px; min-height: 31px; }.page-footer p { margin: 0; color: #686457; font-size: 10px; }.actions { display: flex; flex: none; gap: 6px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #18171277; }
.picker-dialog, .confirm-dialog { width: min(480px, 100%); max-height: min(540px, 90vh); display: flex; flex-direction: column; gap: 10px; padding: 14px; border: 1px solid #c9c3b4; border-radius: 7px; background: #fffdf7; box-shadow: 0 16px 48px #0003; }
.picker-dialog header { display: flex; justify-content: space-between; gap: 12px; }.picker-dialog h2, .confirm-dialog h2 { margin: 0; font-size: 16px; }.picker-dialog p, .confirm-dialog p { margin: 4px 0 0; color: #777366; font-size: 11px; }
.icon-button { width: 28px; height: 28px; border: 0; border-radius: 4px; background: transparent; color: #625f55; font-size: 21px; cursor: pointer; }
.option-list { display: grid; gap: 4px; overflow: auto; }.option-row { display: flex; align-items: center; gap: 9px; min-height: 34px; padding: 5px 8px; border: 1px solid #e3dfd4; border-radius: 4px; background: white; font-size: 11px; }.option-row span { flex: 1; font-weight: 600; }.option-row small { color: #837e70; }
.picker-dialog footer, .confirm-dialog footer { display: flex; align-items: center; justify-content: space-between; gap: 10px; color: #777366; font-size: 10px; }.picker-dialog footer div, .confirm-dialog footer { gap: 6px; }
.confirm-dialog { width: min(400px, 100%); }.confirm-dialog footer { justify-content: flex-end; margin-top: 5px; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
button:focus-visible, input:focus-visible, textarea:focus-visible { outline: 2px solid #d8b622; outline-offset: 2px; }
@media (max-width: 760px) { .repair-order { flex-wrap: wrap; }.repair-number { width: calc(50% - 8px); }.accessory-number { min-width: calc(50% - 8px); }.load-button { margin-left: auto; }.page-footer { align-items: flex-start; flex-direction: column; }.actions { align-self: flex-end; } }
</style>
