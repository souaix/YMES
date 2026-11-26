<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface InventoryOption { inventoryNo: string; inventoryName: string; inventoryType: 3 }
interface ScrapRow {
  inventoryNo: string; lotNo: string; lotSerial: string; opNo: string; scrapQty: number
  lossQty: number; unitNo: string; baseLotNo: string; inputDate: string
}
interface ComponentRow {
  inventoryNo: string; lotSerial: string; lotNo: string; componentNo: string
  goodQty: number; scrapQty: number; unitNo: string
}
interface FailBinRow { inventoryNo: string; lotSerial: string; lotNo: string; binQty: Record<string, number> }
interface ScrapInLine { lotNo: string; lotSerial: string; opNo: string; scrapQty: number; lossQty: number; unitNo: string }
interface ScrapInRow {
  scrInNo: string; inventoryNo: string; description: string; creator: string; createDate: string
  state: '待處理' | '已排入'; lines: ScrapInLine[]
}
interface QueueItem { scrInNo: string; source: 40; createdAt: string; reverseLocks: string[] }
interface DevState {
  version: 1; rows: ScrapRow[]; components: ComponentRow[]; failBins: FailBinRow[]
  scrapIn: ScrapInRow[]; transactionQueue: QueueItem[]
}

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.scr-inventory.v1'
const inventories: InventoryOption[] = [
  { inventoryNo: 'SCR-01', inventoryName: '一廠不良品倉', inventoryType: 3 },
  { inventoryNo: 'SCR-02', inventoryName: '二廠報廢倉', inventoryType: 3 },
]
const bins = [{ binNo: 'BIN01', binOrder: 1 }, { binNo: 'BIN02', binOrder: 2 }, { binNo: 'BIN03', binOrder: 3 }]

function createSeed(): DevState {
  return {
    version: 1,
    rows: [
      { inventoryNo: 'SCR-01', lotNo: 'LOT-DEV-26001', lotSerial: 'S001', opNo: 'OP-ETCH-20', scrapQty: 3, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-DEV-26001', inputDate: '2026-11-24 09:14:20' },
      { inventoryNo: 'SCR-01', lotNo: 'LOT-DEV-26002', lotSerial: 'S002', opNo: 'OP-TEST-70', scrapQty: 0, lossQty: 1, unitNo: 'PCS', baseLotNo: 'BASE-DEV-26002', inputDate: '2026-11-24 10:30:05' },
      { inventoryNo: 'SCR-01', lotNo: 'LOT-DEV-26003', lotSerial: 'S003', opNo: 'OP-INSPECT-80', scrapQty: 2, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-DEV-26003', inputDate: '2026-11-25 08:02:00' },
      { inventoryNo: 'SCR-02', lotNo: 'LOT-DEV-26011', lotSerial: 'S011', opNo: 'OP-TEST-70', scrapQty: 4, lossQty: 0, unitNo: 'PCS', baseLotNo: 'BASE-DEV-26011', inputDate: '2026-11-22 16:47:10' },
    ],
    components: [
      { inventoryNo: 'SCR-01', lotSerial: 'S002', lotNo: 'LOT-DEV-26002', componentNo: 'COMP-DEV-A', goodQty: 4, scrapQty: 1, unitNo: 'PCS' },
    ],
    failBins: [
      { inventoryNo: 'SCR-01', lotSerial: 'S003', lotNo: 'LOT-DEV-26003', binQty: { BIN01: 1.25, BIN02: 0.75, BIN03: 0 } },
    ],
    scrapIn: [
      {
        scrInNo: 'SCRIN-DEV-26001', inventoryNo: 'SCR-01', description: '退回可重工不良品', creator: 'DEV01',
        createDate: '2026-11-25 08:35:00', state: '待處理',
        lines: [{ lotNo: 'LOT-DEV-26002', lotSerial: 'S002', opNo: 'OP-TEST-70', scrapQty: 1, lossQty: 0, unitNo: 'PCS' }],
      },
      {
        scrInNo: 'SCRIN-DEV-26002', inventoryNo: 'SCR-02', description: '外觀檢驗不良轉報廢庫', creator: 'DEV02',
        createDate: '2026-11-24 14:22:00', state: '待處理',
        lines: [{ lotNo: 'LOT-DEV-26011', lotSerial: 'S011', opNo: 'OP-TEST-70', scrapQty: 4, lossQty: 0, unitNo: 'PCS' }],
      },
    ],
    transactionQueue: [],
  }
}

function readState(): DevState {
  try {
    const saved = window.localStorage.getItem(storageKey)
    if (saved) {
      const parsed = JSON.parse(saved) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.rows) && Array.isArray(parsed.components) && Array.isArray(parsed.failBins) && Array.isArray(parsed.scrapIn) && Array.isArray(parsed.transactionQueue)) return parsed
    }
  } catch { /* Use DEV sample data if browser storage is unavailable. */ }
  return createSeed()
}

const state = ref<DevState>(readState())
const inventoryInput = ref('SCR-01')
const inventoryNo = ref('')
const loaded = ref(false)
const selectedKey = ref('')
const selectedKeys = ref<string[]>([])
const notice = ref('DEV 假資料：庫存查詢、刪除與 SCR In 排程只在本機瀏覽器模擬。')
const showInventoryPicker = ref(false)
const showScrInDialog = ref(false)
const showDeleteDialog = ref(false)
const selectedScrInNo = ref('')

const inventoryRows = computed(() => loaded.value ? state.value.rows.filter((row) => row.inventoryNo === inventoryNo.value) : [])
const activeRow = computed(() => inventoryRows.value.find((row) => rowKey(row) === selectedKey.value) ?? inventoryRows.value[0] ?? null)
const activeComponents = computed(() => activeRow.value
  ? state.value.components.filter((row) => row.inventoryNo === activeRow.value?.inventoryNo && row.lotSerial === activeRow.value?.lotSerial)
  : [])
const activeFailBins = computed(() => activeRow.value
  ? state.value.failBins.filter((row) => row.inventoryNo === activeRow.value?.inventoryNo && row.lotSerial === activeRow.value?.lotSerial)
  : [])
const detailMode = computed(() => activeComponents.value.length ? 'Component' : activeFailBins.value.length ? 'FailBin' : 'General')
const pendingScrIn = computed(() => state.value.scrapIn.filter((row) => row.state === '待處理').sort((a, b) => a.scrInNo.localeCompare(b.scrInNo)))
const selectedScrIn = computed(() => pendingScrIn.value.find((row) => row.scrInNo === selectedScrInNo.value) ?? pendingScrIn.value[0] ?? null)
const selectedRows = computed(() => inventoryRows.value.filter((row) => selectedKeys.value.includes(rowKey(row))))
const totalScrap = computed(() => inventoryRows.value.reduce((sum, row) => sum + row.scrapQty, 0))
const totalLoss = computed(() => inventoryRows.value.reduce((sum, row) => sum + row.lossQty, 0))

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* DEV state remains usable without persistence. */ }
}, { deep: true })
watch(inventoryRows, (rows) => {
  if (!rows.some((row) => rowKey(row) === selectedKey.value)) selectedKey.value = rowKey(rows[0]) || ''
  selectedKeys.value = selectedKeys.value.filter((key) => rows.some((row) => rowKey(row) === key))
}, { immediate: true })
watch(pendingScrIn, (rows) => {
  if (!rows.some((row) => row.scrInNo === selectedScrInNo.value)) selectedScrInNo.value = rows[0]?.scrInNo ?? ''
}, { immediate: true })

function rowKey(row: ScrapRow | null | undefined): string {
  return row ? row.inventoryNo + '|' + row.lotSerial : ''
}
function queryInventory(): void {
  inventoryNo.value = inventoryInput.value.trim().toUpperCase()
  loaded.value = true
  selectedKey.value = ''
  selectedKeys.value = []
  const option = inventories.find((item) => item.inventoryNo === inventoryNo.value)
  notice.value = option
    ? '已查詢 ' + option.inventoryNo + ' ' + option.inventoryName + '；Type=3 報廢倉。'
    : '找不到 DEV 報廢庫房 ' + (inventoryNo.value || '（空白）') + '。'
}
function chooseInventory(value: InventoryOption): void {
  inventoryInput.value = value.inventoryNo
  showInventoryPicker.value = false
  queryInventory()
}
function selectRow(row: ScrapRow): void { selectedKey.value = rowKey(row) }
function isChecked(row: ScrapRow): boolean { return selectedKeys.value.includes(rowKey(row)) }
function toggleRow(row: ScrapRow, checked: boolean): void {
  const key = rowKey(row)
  selectedKeys.value = checked
    ? [...new Set([...selectedKeys.value, key])]
    : selectedKeys.value.filter((value) => value !== key)
  selectedKey.value = key
}
function toggleAll(checked: boolean): void {
  selectedKeys.value = checked ? inventoryRows.value.map(rowKey) : []
}
function requestDelete(): void {
  if (!selectedKeys.value.length && activeRow.value) selectedKeys.value = [rowKey(activeRow.value)]
  if (!selectedKeys.value.length) {
    notice.value = '請先選取要刪除的庫存列。'
    return
  }
  showDeleteDialog.value = true
}
function confirmDelete(): void {
  const keys = new Set(selectedKeys.value)
  const removed = state.value.rows.filter((row) => keys.has(rowKey(row)))
  state.value.components = state.value.components.filter((row) => !keys.has(row.inventoryNo + '|' + row.lotSerial))
  state.value.failBins = state.value.failBins.filter((row) => !keys.has(row.inventoryNo + '|' + row.lotSerial))
  state.value.rows = state.value.rows.filter((row) => !keys.has(rowKey(row)))
  selectedKeys.value = []
  selectedKey.value = ''
  showDeleteDialog.value = false
  notice.value = 'DEV 已依 InventoryNo＋LotSerial 刪除 ' + removed.length + ' 列及其 Component／FailBin 子列。'
}
function openScrIn(): void {
  if (!pendingScrIn.value.length) {
    notice.value = '目前沒有待處理的 SCR In DEV 單據。'
    return
  }
  selectedScrInNo.value = pendingScrIn.value[0].scrInNo
  showScrInDialog.value = true
}
function confirmScrIn(): void {
  const ticket = selectedScrIn.value
  if (!ticket || ticket.state !== '待處理') {
    notice.value = '請選取一張待處理的 SCR In 單據。'
    return
  }
  const reverseLocks = ticket.lines.map((line) => line.lotNo + '|' + line.lotSerial)
  ticket.state = '已排入'
  state.value.transactionQueue.push({
    scrInNo: ticket.scrInNo,
    source: 40,
    createdAt: new Date().toLocaleString('zh-TW', { hour12: false }),
    reverseLocks,
  })
  showScrInDialog.value = false
  notice.value = 'DEV 已模擬 Source=40 交易排程與 ' + reverseLocks.length + ' 筆 Lot 反悔鎖定；實際入庫由下游交易處理。'
}
function resetDevData(): void {
  state.value = createSeed()
  inventoryInput.value = 'SCR-01'
  inventoryNo.value = ''
  loaded.value = false
  selectedKeys.value = []
  selectedKey.value = ''
  notice.value = 'DEV 範例資料已重設。'
}
function formatQty(value: number): string {
  return new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 6 }).format(value)
}
function binQty(row: FailBinRow, binNo: string): number { return row.binQty[binNo] ?? 0 }
</script>

<template>
  <main class="scr-inventory" aria-label="報廢倉庫房資料 DEV" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="heading">
      <div><p class="eyebrow">INVENTORY · IV601</p><h1>報廢倉庫房資料</h1><p class="subtitle">查詢 SCR Inventory、選列檢視 Component／FailBin，並管理 SCR In 待處理交易</p></div>
      <div class="heading-actions"><span class="dev-pill">DEV MOCK</span><button type="button" @click="emit('close')">關閉</button></div>
    </header>

    <section class="toolbar card" aria-label="報廢庫房查詢">
      <label>報廢庫房 InventoryNo
        <span class="input-pair"><input v-model="inventoryInput" list="scr-inventory-options" aria-label="報廢庫房編號" @keydown.enter="queryInventory"><datalist id="scr-inventory-options"><option v-for="item in inventories" :key="item.inventoryNo" :value="item.inventoryNo">{{ item.inventoryName }}</option></datalist><button type="button" class="square" aria-label="選擇報廢庫房" @click="showInventoryPicker = true">…</button></span>
      </label>
      <button class="primary" type="button" @click="queryInventory">查詢</button>
      <button type="button" @click="openScrIn">SCR In</button>
      <button class="danger" type="button" :disabled="!inventoryRows.length" @click="requestDelete">刪除選取</button>
      <button type="button" @click="resetDevData">重設 DEV</button>
    </section>

    <p class="notice" role="status">{{ notice }}</p>

    <section class="summary-grid" aria-label="查詢統計">
      <article class="metric"><span>庫存列</span><strong>{{ inventoryRows.length }}</strong><small>{{ inventoryNo || '尚未查詢' }}</small></article>
      <article class="metric"><span>ScrapQty 合計</span><strong>{{ formatQty(totalScrap) }}</strong><small>Unit No 依明細列顯示</small></article>
      <article class="metric"><span>LossQty 合計</span><strong>{{ formatQty(totalLoss) }}</strong><small>不良／損耗數量</small></article>
      <article class="metric"><span>SCR In 待處理</span><strong>{{ pendingScrIn.length }}</strong><small>Source=40 交易入口</small></article>
    </section>

    <section class="card">
      <div class="card-heading"><div><h2>SCR Inventory</h2><p>主表選列會依序查 Component，再查 FailBin；一般 Lot 沒有子表時不另外帶回明細。</p></div><span class="key-note">parent key：InventoryNo ＋ LotSerial</span></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選庫存列" :checked="inventoryRows.length > 0 && selectedKeys.length === inventoryRows.length" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>UnitNo</th><th>BaseLotNo</th><th>InputDate</th></tr></thead>
          <tbody>
            <tr v-for="row in inventoryRows" :key="rowKey(row)" :class="{ active: activeRow && rowKey(activeRow) === rowKey(row) }" @click="selectRow(row)">
              <td class="check-cell"><input type="checkbox" :aria-label="'選取 ' + row.lotNo + ' ' + row.lotSerial" :checked="isChecked(row)" @click.stop @change="toggleRow(row, ($event.target as HTMLInputElement).checked)"></td>
              <td class="mono">{{ row.lotNo }}</td><td class="mono">{{ row.lotSerial }}</td><td>{{ row.opNo }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td class="num">{{ formatQty(row.lossQty) }}</td><td>{{ row.unitNo }}</td><td>{{ row.baseLotNo }}</td><td>{{ row.inputDate }}</td>
            </tr>
            <tr v-if="!inventoryRows.length"><td colspan="9" class="empty">{{ loaded ? '此報廢庫房沒有 DEV 庫存列' : '輸入 InventoryNo 或用 … 選擇報廢庫房後查詢' }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card detail-card">
      <div class="card-heading"><div><h2>選取列明細</h2><p>{{ activeRow ? activeRow.lotNo + ' · ' + activeRow.lotSerial + ' · ' + detailMode : '尚未選取庫存列' }}</p></div><span v-if="activeRow" class="key-note">{{ activeRow.inventoryNo }} / {{ activeRow.lotSerial }}</span></div>
      <template v-if="activeRow && detailMode === 'Component'">
        <div class="table-wrap"><table><thead><tr><th>ComponentNo</th><th class="num">GoodQty</th><th class="num">ScrapQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="row in activeComponents" :key="row.componentNo + '|' + row.unitNo"><td class="mono">{{ row.componentNo }}</td><td class="num">{{ formatQty(row.goodQty) }}</td><td class="num">{{ formatQty(row.scrapQty) }}</td><td>{{ row.unitNo }}</td></tr></tbody></table></div>
      </template>
      <template v-else-if="activeRow && detailMode === 'FailBin'">
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th v-for="bin in bins" :key="bin.binNo" class="num">{{ bin.binNo }}</th></tr></thead><tbody><tr v-for="row in activeFailBins" :key="row.inventoryNo + '|' + row.lotSerial"><td class="mono">{{ row.lotNo }}</td><td v-for="bin in bins" :key="bin.binNo" class="num">{{ formatQty(binQty(row, bin.binNo)) }}</td></tr></tbody></table></div>
        <p class="subnote">Bin 欄依 SYS Bin 的 BinOrder 動態顯示；此 DEV 欄位集僅供互動示範。</p>
      </template>
      <div v-else class="general-detail"><span class="general-chip">General SCR Inventory</span><span>沒有 Component／FailBin 子列；SCRAP／LOSS 數量直接顯示在上方庫存列。</span></div>
    </section>

    <section class="card queue-card">
      <div class="card-heading"><div><h2>DEV 交易排程</h2><p>模擬 frmINVSCRInConfirm：建立 Source=40 任務，並對每筆 Lot／LotSerial 設反悔鎖。</p></div><span>{{ state.transactionQueue.length }} 筆</span></div>
      <div class="table-wrap"><table><thead><tr><th>SCRInNo</th><th>Source</th><th>排入時間</th><th>反悔鎖定 Lot</th></tr></thead><tbody><tr v-for="job in [...state.transactionQueue].reverse()" :key="job.scrInNo"><td class="mono">{{ job.scrInNo }}</td><td>{{ job.source }}</td><td>{{ job.createdAt }}</td><td>{{ job.reverseLocks.join(', ') || '—' }}</td></tr><tr v-if="!state.transactionQueue.length"><td colspan="4" class="empty">尚未排入交易</td></tr></tbody></table></div>
    </section>

    <div v-if="showInventoryPicker" class="modal-backdrop" role="presentation" @click.self="showInventoryPicker = false">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="選擇報廢庫房"><header><div><p class="eyebrow">InventoryType = 3</p><h2>選擇報廢庫房</h2></div><button type="button" @click="showInventoryPicker = false">關閉</button></header><div class="choice-list"><button v-for="item in inventories" :key="item.inventoryNo" type="button" @click="chooseInventory(item)"><strong>{{ item.inventoryNo }}</strong><span>{{ item.inventoryName }}</span></button></div></section>
    </div>
    <div v-if="showDeleteDialog" class="modal-backdrop" role="presentation" @click.self="showDeleteDialog = false">
      <section class="dialog" role="dialog" aria-modal="true" aria-label="確認刪除報廢庫存"><header><div><p class="eyebrow">DELETE SCR INVENTORY</p><h2>確認刪除 {{ selectedRows.length || selectedKeys.length }} 列？</h2></div><button type="button" @click="showDeleteDialog = false">取消</button></header><p>會依 InventoryNo＋LotSerial 刪除 Component、FailBin 子列與 SCR Inventory 主列。舊 Core 沒有先檢查單據狀態或預約關係。</p><div class="dialog-actions"><button type="button" @click="showDeleteDialog = false">返回</button><button class="danger" type="button" @click="confirmDelete">確認刪除 DEV 資料</button></div></section>
    </div>
    <div v-if="showScrInDialog" class="modal-backdrop" role="presentation" @click.self="showScrInDialog = false">
      <section class="dialog wide" role="dialog" aria-modal="true" aria-label="SCR In 確認">
        <header><div><p class="eyebrow">SCR IN · SOURCE 40</p><h2>SCR In 確認</h2></div><button type="button" @click="showScrInDialog = false">關閉</button></header>
        <label>待處理單據<select v-model="selectedScrInNo"><option v-for="ticket in pendingScrIn" :key="ticket.scrInNo" :value="ticket.scrInNo">{{ ticket.scrInNo }} · {{ ticket.inventoryNo }}</option></select></label>
        <div v-if="selectedScrIn" class="ticket-info"><span><b>InventoryNo</b>{{ selectedScrIn.inventoryNo }}</span><span><b>Creator</b>{{ selectedScrIn.creator }}</span><span><b>CreateDate</b>{{ selectedScrIn.createDate }}</span><span><b>Description</b>{{ selectedScrIn.description }}</span></div>
        <div class="table-wrap"><table><thead><tr><th>LotNo</th><th>LotSerial</th><th>OPNo</th><th class="num">ScrapQty</th><th class="num">LossQty</th><th>UnitNo</th></tr></thead><tbody><tr v-for="line in selectedScrIn?.lines ?? []" :key="line.lotNo + '|' + line.lotSerial"><td class="mono">{{ line.lotNo }}</td><td class="mono">{{ line.lotSerial }}</td><td>{{ line.opNo }}</td><td class="num">{{ formatQty(line.scrapQty) }}</td><td class="num">{{ formatQty(line.lossQty) }}</td><td>{{ line.unitNo }}</td></tr><tr v-if="!selectedScrIn?.lines.length"><td colspan="6" class="empty">沒有可排入的明細</td></tr></tbody></table></div>
        <p class="subnote">正式流程先排交易，並對明細 Lot 設反悔鎖；本模擬不直接改 SCR Inventory 數量。</p>
        <div class="dialog-actions"><button type="button" @click="showScrInDialog = false">取消</button><button class="primary" type="button" :disabled="!selectedScrIn" @click="confirmScrIn">排入 Source=40 交易</button></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.scr-inventory { display: grid; gap: 14px; min-height: 0; padding: 16px; overflow: auto; box-sizing: border-box; background: #f4f2eb; color: #292820; font: inherit; }
.heading, .heading-actions, .card-heading, .toolbar, .dialog header, .dialog-actions { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.heading h1, .dialog h2 { margin: 2px 0 0; font-size: 22px; }
.eyebrow { margin: 0; color: #777466; font-size: 11px; font-weight: 700; letter-spacing: .11em; }
.subtitle, .card-heading p, .subnote { margin: 5px 0 0; color: #706e62; font-size: 12px; }
.heading-actions { justify-content: flex-end; }
.dev-pill, .key-note, .general-chip { border: 1px solid #c8c4b8; border-radius: 999px; padding: 5px 10px; color: #5c5a50; background: #fbfaf6; font-size: 11px; white-space: nowrap; }
button, input, select { font: inherit; }
button { min-height: 30px; border: 1px solid #c4c0b4; border-radius: 4px; padding: 4px 10px; background: #fffefa; color: #302f28; cursor: pointer; }
button:hover { border-color: #777467; }
button:disabled { opacity: .48; cursor: not-allowed; }
.primary { border-color: #176356; background: #176356; color: #fff; }
.danger { border-color: #a44940; color: #9d3e37; }
.danger:not(:disabled):hover { background: #a44940; color: white; }
.card { min-width: 0; border: 1px solid #d6d2c7; border-radius: 6px; background: #fffefa; padding: 13px; box-shadow: 0 2px 8px #3e392412; }
.toolbar { justify-content: flex-start; flex-wrap: wrap; }
.toolbar label, .dialog label { display: grid; gap: 5px; color: #555349; font-size: 12px; }
.input-pair { display: flex; }
.input-pair input { width: 190px; }
.input-pair input, .dialog select { min-height: 31px; border: 1px solid #c9c5b9; border-radius: 4px; padding: 4px 8px; color: #292820; background: white; }
.square { width: 32px; margin-left: 4px; font-size: 18px; line-height: 1; }
.notice { margin: 0; border-left: 3px solid #7f9f74; padding: 8px 10px; color: #5a5b50; background: #e9eee4; font-size: 12px; }
.summary-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; }
.metric { display: grid; gap: 5px; border: 1px solid #d6d2c7; border-radius: 6px; padding: 11px 13px; background: #fffefa; }
.metric span, .metric small { color: #777466; font-size: 11px; }
.metric strong { font-size: 21px; font-weight: 650; }
.card-heading { margin-bottom: 10px; }
.card-heading h2 { margin: 0; font-size: 15px; }
.table-wrap { width: 100%; overflow: auto; }
table { width: 100%; min-width: 720px; border-collapse: collapse; font-size: 12px; }
th, td { border-bottom: 1px solid #e5e2da; padding: 8px 9px; text-align: left; white-space: nowrap; }
th { background: #f4f2eb; color: #666357; font-weight: 650; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f7f5ef; }
tbody tr.active { background: #e8efe9; }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.mono { font-family: Consolas, 'Courier New', monospace; }
.check-cell { width: 34px; text-align: center; }
.check-cell input { accent-color: #176356; }
.empty { padding: 18px; text-align: center; color: #898679; }
.general-detail { display: flex; align-items: center; gap: 12px; padding: 12px 2px 4px; color: #6d6a60; font-size: 12px; }
.subnote { margin-top: 8px; }
.queue-card .card-heading > span { color: #6f6b5f; font-size: 12px; }
.modal-backdrop { position: fixed; z-index: 800; inset: 0; display: grid; place-items: center; padding: 18px; background: #17191477; }
.dialog { display: grid; gap: 14px; width: min(520px, 96vw); max-height: 88vh; overflow: auto; border: 1px solid #c9c5b9; border-radius: 7px; padding: 16px; background: #fffefa; box-shadow: 0 18px 48px #15160e44; }
.dialog.wide { width: min(820px, 96vw); }
.dialog header { align-items: flex-start; }
.dialog p { margin: 0; color: #6f6b5f; font-size: 12px; line-height: 1.5; }
.choice-list { display: grid; gap: 7px; }
.choice-list button { display: flex; justify-content: space-between; align-items: center; min-height: 42px; text-align: left; }
.choice-list span { color: #777466; }
.dialog-actions { justify-content: flex-end; }
.ticket-info { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 8px; }
.ticket-info span { display: grid; gap: 3px; color: #4f4d44; font-size: 12px; }
.ticket-info b { color: #817e70; font-size: 10px; font-weight: 600; }
@media (max-width: 780px) {
  .scr-inventory { padding: 10px; }
  .heading, .card-heading { align-items: flex-start; flex-wrap: wrap; }
  .summary-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .toolbar label { flex: 1 1 100%; }
  .input-pair input { width: 100%; }
}
</style>
