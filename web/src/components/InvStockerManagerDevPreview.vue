<script setup lang="ts">
import { computed, ref } from 'vue'

type ContentKind = 'LOT' | 'CARRIER'
type Operation = 'StockOn' | 'StockOff'
interface Stocker { stockerNo: string; areaNo: string; vertical: number; horizontal: number; capacityCtrl: boolean; qtyLimit: number }
interface Slot { stockerNo: string; locatorNo: string; vertical: number; horizontal: number }
interface Occupant { stockerNo: string; locatorNo: string; contentNo: string; kind: ContentKind; areaNo: string; createDate: string }
interface HistoryRow { at: string; operation: Operation; stockerNo: string; locatorNo: string; contentNo: string; occupantCount: number }

const props = withDefaults(defineProps<{ developmentPreview?: boolean; initialStockerNo?: string }>(), { developmentPreview: true, initialStockerNo: '' })
const emit = defineEmits<{ close: [] }>()
const previewLabel = computed(() => props.developmentPreview ? 'DEV 假資料預覽' : 'DEV 模擬')
const stockers: Stocker[] = [
  { stockerNo: 'STK-DEV-01', areaNo: 'AREA-DEV-01', vertical: 4, horizontal: 3, capacityCtrl: true, qtyLimit: 2 },
  { stockerNo: 'STK-DEV-02', areaNo: 'AREA-DEV-02', vertical: 3, horizontal: 2, capacityCtrl: false, qtyLimit: 1 },
]
const slots: Slot[] = stockers.flatMap((stocker) => {
  const result: Slot[] = []
  for (let horizontal = 1; horizontal <= stocker.horizontal; horizontal += 1) {
    for (let vertical = 1; vertical <= stocker.vertical; vertical += 1) {
      const serial = (horizontal - 1) * stocker.vertical + vertical
      result.push({ stockerNo: stocker.stockerNo, locatorNo: `${stocker.stockerNo}-L${String(serial).padStart(2, '0')}`, vertical, horizontal })
    }
  }
  return result
})
const lotState = [
  { lotNo: 'LOT-DEV-001', status: 5, curQty: 240 },
  { lotNo: 'LOT-DEV-002', status: 3, curQty: 80 },
  { lotNo: 'LOT-DEV-003', status: 11, curQty: 120 },
  { lotNo: 'LOT-DEV-004', status: 99, curQty: 48 },
]
const carriers = [
  { carrierNo: 'CAR-DEV-01', issueState: 2 },
  { carrierNo: 'CAR-DEV-02', issueState: 1 },
  { carrierNo: 'CAR-DEV-03', issueState: 2 },
]
const blockedLotStatuses = new Set([1, 11, 12, 99])
const occupants = ref<Occupant[]>([
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L01', contentNo: 'LOT-DEV-001', kind: 'LOT', areaNo: 'AREA-DEV-01', createDate: '2026-11-24 08:20:00' },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L01', contentNo: 'LOT-DEV-002', kind: 'LOT', areaNo: 'AREA-DEV-01', createDate: '2026-11-24 09:10:00' },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L02', contentNo: 'CAR-DEV-01', kind: 'CARRIER', areaNo: 'AREA-DEV-01', createDate: '2026-11-23 14:35:00' },
  { stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L07', contentNo: 'LOT-DEV-003', kind: 'LOT', areaNo: 'AREA-DEV-01', createDate: '2026-11-22 11:00:00' },
  { stockerNo: 'STK-DEV-02', locatorNo: 'STK-DEV-02-L01', contentNo: 'CAR-DEV-03', kind: 'CARRIER', areaNo: 'AREA-DEV-02', createDate: '2026-11-22 08:15:00' },
])
const initialHistory: HistoryRow[] = [
  { at: '2026-11-24 09:10:00', operation: 'StockOn', stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L01', contentNo: 'LOT-DEV-002', occupantCount: 2 },
  { at: '2026-11-23 14:35:00', operation: 'StockOn', stockerNo: 'STK-DEV-01', locatorNo: 'STK-DEV-01-L02', contentNo: 'CAR-DEV-01', occupantCount: 1 },
]
const selectedStockerNo = ref(props.initialStockerNo)
const selectedLocatorNo = ref('')
const contentNo = ref('')
const operation = ref<Operation>('StockOn')
const history = ref<HistoryRow[]>(initialHistory.slice())
const notice = ref(props.initialStockerNo ? `已由 IV306 帶入 ${props.initialStockerNo}；選擇格位後輸入 LotNo 或 CarrierNo。` : 'DEV 假資料：選擇已核准貨架後，依序選格位並輸入 LotNo 或 CarrierNo。')
const selectedStocker = computed(() => stockers.find((item) => item.stockerNo === selectedStockerNo.value) ?? null)
const selectedSlot = computed(() => slots.find((item) => item.stockerNo === selectedStockerNo.value && item.locatorNo === selectedLocatorNo.value) ?? null)
const stockerSlots = computed(() => slots.filter((item) => item.stockerNo === selectedStockerNo.value).slice().sort((a, b) => a.horizontal - b.horizontal || a.vertical - b.vertical))
const stockerOccupants = computed(() => occupants.value.filter((item) => item.stockerNo === selectedStockerNo.value).slice().sort((a, b) => a.locatorNo.localeCompare(b.locatorNo) || a.contentNo.localeCompare(b.contentNo)))
const selectedOccupants = computed(() => occupants.value.filter((item) => item.stockerNo === selectedStockerNo.value && item.locatorNo === selectedLocatorNo.value))
const rackColumns = computed(() => selectedStocker.value?.vertical ?? 1)
const visibleHistory = computed(() => history.value.filter((row) => !selectedStockerNo.value || row.stockerNo === selectedStockerNo.value).slice(0, 6))

function occupantsAt(slot: Slot): Occupant[] { return occupants.value.filter((item) => item.stockerNo === slot.stockerNo && item.locatorNo === slot.locatorNo) }
function isLocked(slot: Slot): boolean {
  const stocker = stockers.find((item) => item.stockerNo === slot.stockerNo)
  return !!stocker?.capacityCtrl && occupantsAt(slot).length >= stocker.qtyLimit
}
function slotKind(slot: Slot): ContentKind | '' { return occupantsAt(slot)[0]?.kind ?? '' }
function selectStocker(value: string): void {
  selectedStockerNo.value = value; selectedLocatorNo.value = ''; contentNo.value = ''
  const stocker = stockers.find((item) => item.stockerNo === value)
  notice.value = stocker ? `${stocker.stockerNo} 已載入 ${stockerSlots.value.length} 個格位及目前上架清單。` : '請選擇已核准貨架。'
}
function handleStockerChange(event: Event): void {
  const target = event.target
  if (target instanceof HTMLSelectElement) selectStocker(target.value)
}
function selectSlot(slot: Slot): void { selectedLocatorNo.value = slot.locatorNo; notice.value = `已選格位 ${slot.locatorNo}；輸入內容編號後按 Enter 或執行操作。` }
function chooseContentRow(row: Occupant): void {
  const slot = slots.find((item) => item.stockerNo === row.stockerNo && item.locatorNo === row.locatorNo)
  if (!slot) return
  selectSlot(slot)
  fillContent(row.contentNo)
}
function stampNow(): string {
  const date = new Date(); const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}
function reject(message: string): void { notice.value = message }
function commitOperation(): void {
  const stocker = selectedStocker.value; const slot = selectedSlot.value; const normalized = contentNo.value.trim().toUpperCase()
  if (!stocker) return reject('請先選擇 StockerNo。')
  if (!slot) return reject('請先選擇有效 LocatorNo。')
  if (!normalized) return reject('請輸入 ContentNo。')
  const sameContent = occupants.value.find((item) => item.contentNo === normalized)
  if (operation.value === 'StockOn') {
    if (sameContent) return reject(`${normalized} 已經上架於 ${sameContent.stockerNo} / ${sameContent.locatorNo}。`)
    if (isLocked(slot)) return reject(`格位容量已滿：${slot.locatorNo}，本機配置 QtyLimit=${stocker.qtyLimit}。`)
    const current = occupantsAt(slot)
    const lot = lotState.find((item) => item.lotNo === normalized)
    let kind: ContentKind
    if (lot) {
      if (blockedLotStatuses.has(lot.status)) return reject(`Lot ${normalized} 的 Status=${lot.status}，舊 Core 不允許上架。`)
      kind = 'LOT'
    } else {
      const carrier = carriers.find((item) => item.carrierNo === normalized)
      if (!carrier) return reject(`${normalized} 不是可識別的 Lot 或 Carrier。`)
      if (carrier.issueState !== 2) return reject(`Carrier ${normalized} 尚未核准（IssueState=${carrier.issueState}）。`)
      kind = 'CARRIER'
    }
    if (current.length && current[0]?.kind !== kind) return reject(`格位已放入 ${current[0]?.kind === 'LOT' ? 'Lot' : 'Carrier'}，不能混放 ${kind === 'LOT' ? 'Lot' : 'Carrier'}。`)
    const at = stampNow()
    occupants.value.push({ stockerNo: stocker.stockerNo, locatorNo: slot.locatorNo, contentNo: normalized, kind, areaNo: stocker.areaNo, createDate: at })
    history.value.unshift({ at, operation: 'StockOn', stockerNo: stocker.stockerNo, locatorNo: slot.locatorNo, contentNo: normalized, occupantCount: current.length + 1 })
    notice.value = `DEV 上架完成：${normalized} → ${slot.locatorNo}。假資料已加入本頁現況與歷程。`
  } else {
    const located = occupants.value.find((item) => item.contentNo === normalized && item.stockerNo === stocker.stockerNo && item.locatorNo === slot.locatorNo)
    if (!located) return reject(`${normalized} 不在指定的 ${stocker.stockerNo} / ${slot.locatorNo}，或從未上架。`)
    if (located.kind === 'LOT') {
      const lot = lotState.find((item) => item.lotNo === normalized)
      if (lot && blockedLotStatuses.has(lot.status)) return reject(`Lot ${normalized} 的 Status=${lot.status}，舊 Core 不允許下架。`)
    } else if (!carriers.some((item) => item.carrierNo === normalized)) {
      return reject(`Carrier ${normalized} 不存在於載具現況。`)
    }
    const at = stampNow(); const remainingCount = occupantsAt(slot).length - 1
    occupants.value = occupants.value.filter((item) => item !== located)
    history.value.unshift({ at, operation: 'StockOff', stockerNo: stocker.stockerNo, locatorNo: slot.locatorNo, contentNo: normalized, occupantCount: remainingCount })
    notice.value = `DEV 下架完成：${normalized} ← ${slot.locatorNo}。假資料已從本頁現況移除並記錄歷程。`
  }
  contentNo.value = ''
}
function fillContent(value: string): void { contentNo.value = value; notice.value = `${value} 已帶入 ContentNo；確認格位及 StockOn／StockOff 後執行。` }
</script>

<template>
  <main class="stocker-manager" aria-label="在製貨架上下架作業">
    <header class="page-head">
      <div><p class="eyebrow">INV · IV305</p><h1>在製貨架上下架作業</h1><p class="subtitle">Stocker Management · Lot／Carrier 格位操作</p></div>
      <div class="head-actions"><span class="dev-tag">{{ previewLabel }}</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <section class="control-card">
      <div class="control-grid">
        <label class="field stocker-field"><span>StockerNo <b>*</b></span><select :value="selectedStockerNo" @change="handleStockerChange"><option value="">請選擇已核准貨架</option><option v-for="item in stockers" :key="item.stockerNo" :value="item.stockerNo">{{ item.stockerNo }} · {{ item.areaNo }}</option></select></label>
        <label class="field"><span>LocatorNo <b>*</b></span><input :value="selectedLocatorNo" readonly placeholder="從左側貨架選格位"></label>
        <label class="field content-field"><span>ContentNo <b>*</b></span><input v-model="contentNo" placeholder="掃描／輸入 LotNo 或 CarrierNo" @keyup.enter="commitOperation"></label>
        <fieldset class="mode-select"><legend>操作</legend><label><input v-model="operation" type="radio" value="StockOn"> StockOn</label><label><input v-model="operation" type="radio" value="StockOff"> StockOff</label></fieldset>
        <button type="button" class="primary" @click="commitOperation">{{ operation === 'StockOn' ? '上架' : '下架' }}</button>
      </div>
      <p class="notice" role="status">{{ notice }}</p>
    </section>

    <div class="work-area">
      <section class="rack-card">
        <div class="section-head"><div><h2>Locator 格位</h2><p v-if="selectedStocker">{{ selectedStocker.stockerNo }} · Area {{ selectedStocker.areaNo }} · {{ selectedStocker.vertical }} × {{ selectedStocker.horizontal }}</p><p v-else>選擇貨架後載入格位</p></div><div class="legend"><span><i class="empty-dot"></i>空格</span><span><i class="occupied-dot"></i>有內容</span><span><i class="locked-dot"></i>容量已滿</span></div></div>
        <div v-if="selectedStocker" class="rack-grid" :style="{ '--rack-columns': rackColumns }">
          <button v-for="slot in stockerSlots" :key="slot.locatorNo" type="button" class="slot" :class="{ occupied: occupantsAt(slot).length > 0, locked: isLocked(slot), selected: slot.locatorNo === selectedLocatorNo, carrier: slotKind(slot) === 'CARRIER' }" :aria-label="`${slot.locatorNo}, ${occupantsAt(slot).length} contents`" @click="selectSlot(slot)">
            <span class="slot-name">{{ slot.locatorNo }}</span><strong>{{ occupantsAt(slot).length ? `${occupantsAt(slot).length} / ${selectedStocker.capacityCtrl ? selectedStocker.qtyLimit : '∞'}` : 'EMPTY' }}</strong><small>{{ occupantsAt(slot)[0]?.kind === 'CARRIER' ? 'Carrier' : occupantsAt(slot).length ? 'Lot' : `V${slot.vertical} · H${slot.horizontal}` }}</small>
          </button>
        </div>
        <div v-else class="rack-placeholder">選取 StockerNo 後顯示貨架位置。</div>
        <div v-if="selectedSlot" class="selected-slot"><span>目前格位</span><strong>{{ selectedSlot.locatorNo }}</strong><span>{{ selectedOccupants.length }} 個 ContentNo</span></div>
      </section>

      <section class="state-card">
        <div class="section-head"><div><h2>Stocker State</h2><p>舊 Grid 僅顯示 LocatorNo、ContentNo、操作狀態</p></div><span v-if="selectedStocker" class="count-pill">{{ stockerOccupants.length }} 筆</span></div>
        <div class="table-wrap"><table><thead><tr><th>LocatorNo</th><th>ContentNo</th><th>Kind</th><th>Status</th><th></th></tr></thead><tbody>
          <tr v-for="row in stockerOccupants" :key="`${row.locatorNo}|${row.contentNo}`" :class="{ 'row-active': row.locatorNo === selectedLocatorNo }"><td class="code">{{ row.locatorNo }}</td><td class="code">{{ row.contentNo }}</td><td><span class="kind" :class="row.kind.toLowerCase()">{{ row.kind }}</span></td><td>StockOnFinished</td><td><button type="button" class="use-row" title="帶入 ContentNo 與 LocatorNo" @click="chooseContentRow(row)">選取</button></td></tr>
          <tr v-if="selectedStocker && !stockerOccupants.length"><td colspan="5" class="empty">此貨架目前沒有上架內容</td></tr>
          <tr v-if="!selectedStocker"><td colspan="5" class="empty">請先選擇貨架</td></tr>
        </tbody></table></div>
        <p class="hint">Lot ContentNo 依舊 Form 的讀碼流程驗證；下架需指定目前所在貨架及格位。</p>
      </section>
    </div>

    <section class="history-card">
      <div class="section-head"><div><h2>本頁 DEV 操作回饋</h2><p>舊 Form 不顯示 Stocker Detail Log；下方只用來辨認本頁假操作</p></div><span class="read-only">DEV ONLY</span></div>
      <div class="table-wrap"><table><thead><tr><th>Time</th><th>Operation</th><th>StockerNo</th><th>LocatorNo</th><th>ContentNo</th><th class="numeric">ContentQty</th></tr></thead><tbody>
        <tr v-for="row in visibleHistory" :key="`${row.at}|${row.operation}|${row.contentNo}`"><td>{{ row.at }}</td><td><span class="operation" :class="row.operation.toLowerCase()">{{ row.operation }}</span></td><td class="code">{{ row.stockerNo }}</td><td class="code">{{ row.locatorNo }}</td><td class="code">{{ row.contentNo }}</td><td class="numeric">{{ row.occupantCount }}</td></tr><tr v-if="!visibleHistory.length"><td colspan="6" class="empty">尚無本頁操作</td></tr>
      </tbody></table></div>
      <footer class="card-footer"><span>假現況只存在本頁記憶體，不呼叫正式 API 或資料庫。</span><span>現況與歷程不代表真正庫存數量；ContentQty 沿舊 Core 為格位內容筆數。</span></footer>
    </section>
  </main>
</template>

<style scoped>
.stocker-manager { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 18px 22px 22px; background: #f1f2ef; color: #2d332e; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-head,.head-actions,.section-head,.card-footer { display: flex; align-items: center; justify-content: space-between; gap: 14px; }
.page-head { max-width: 1440px; margin: 0 auto 15px; }
.page-head h1 { margin: 0; color: #25352a; font-size: 22px; line-height: 1.25; }
.eyebrow { margin: 0 0 3px; color: #557961; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
.subtitle,.section-head p { margin: 3px 0 0; color: #73796f; font-size: 11px; }
.head-actions { flex-shrink: 0; }
.dev-tag,.read-only,.count-pill { display: inline-block; border: 1px solid #c0d5c4; border-radius: 999px; padding: 4px 9px; color: #3e6849; background: #edf5ed; font-size: 10px; font-weight: 700; }
.close,.primary { min-height: 32px; border: 1px solid #ccd1ca; border-radius: 5px; padding: 5px 12px; background: white; color: #39443b; font: inherit; cursor: pointer; }
.primary { align-self: end; min-width: 84px; border-color: #467b55; background: #467b55; color: white; font-weight: 700; }
.control-card,.rack-card,.state-card,.history-card { max-width: 1440px; margin: 0 auto 14px; border: 1px solid #d9ded7; border-radius: 8px; background: white; box-shadow: 0 2px 9px #252e260a; }
.control-card { padding: 14px 16px 10px; }
.control-grid { display: grid; grid-template-columns: minmax(210px, 1.1fr) minmax(190px, .9fr) minmax(220px, 1.2fr) minmax(190px, .9fr) auto; align-items: end; gap: 10px; }
.field { display: grid; gap: 4px; min-width: 0; color: #626a61; font-size: 11px; }
.field b { color: #bc4a42; }
.field input,.field select { width: 100%; min-height: 34px; box-sizing: border-box; border: 1px solid #d0d7cf; border-radius: 4px; padding: 5px 8px; color: #29332c; background: white; font: inherit; font-size: 12px; }
.field input[readonly] { background: #f4f6f3; }
.mode-select { display: flex; align-items: center; gap: 12px; min-width: 190px; min-height: 34px; box-sizing: border-box; margin: 0; border: 1px solid #d0d7cf; border-radius: 4px; padding: 3px 8px 5px; color: #596259; }
.mode-select legend { padding: 0 4px; color: #747b71; font-size: 10px; }
.mode-select label { white-space: nowrap; font-weight: 600; }
.mode-select input { accent-color: #477f56; vertical-align: middle; }
.notice { margin: 11px 0 0; border-left: 3px solid #70a37b; padding: 5px 9px; color: #5d685d; background: #f3f8f2; font-size: 11px; }
.work-area { display: grid; grid-template-columns: minmax(360px, .92fr) minmax(460px, 1.08fr); gap: 14px; max-width: 1440px; margin: 0 auto 14px; }
.work-area > section { min-width: 0; margin-bottom: 0; }
.rack-card,.state-card,.history-card { overflow: hidden; }
.section-head { padding: 13px 15px 11px; border-bottom: 1px solid #e9ece7; }
.section-head h2 { margin: 0; color: #344538; font-size: 14px; }
.legend { display: flex; flex-wrap: wrap; gap: 9px; color: #778076; font-size: 9px; }
.legend span { display: inline-flex; align-items: center; gap: 4px; white-space: nowrap; }
.legend i { width: 9px; height: 9px; border: 1px solid #c4ccc3; border-radius: 3px; background: #f8faf7; }
.legend .occupied-dot { border-color: #96bad0; background: #e8f2f8; }
.legend .locked-dot { border-color: #d9837c; background: #fde9e7; }
.rack-grid { display: grid; grid-template-columns: repeat(var(--rack-columns), minmax(58px, 1fr)); gap: 8px; align-content: start; max-height: 410px; overflow: auto; padding: 16px; }
.slot { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 77px; border: 1px solid #d6ded4; border-radius: 6px; background: #fbfcfa; color: #606e60; cursor: pointer; transition: border-color .12s, background .12s; }
.slot:hover { border-color: #77a381; }
.slot.occupied { border-color: #a6c3d4; background: #eaf4f9; color: #3e6173; }
.slot.carrier { border-color: #bdadd7; background: #f2eef8; color: #60517d; }
.slot.locked { border-color: #dc918a; background: #fff0ee; color: #a34b43; }
.slot.selected { outline: 2px solid #477f56; outline-offset: 1px; }
.slot-name { font-family: Consolas, "Courier New", monospace; font-size: 10px; }
.slot strong { margin: 3px 0 1px; font-size: 13px; }
.slot small { font-size: 9px; opacity: .8; }
.rack-placeholder { display: grid; min-height: 260px; place-items: center; color: #899086; }
.selected-slot { display: flex; align-items: center; justify-content: space-between; gap: 8px; border-top: 1px solid #e8ebe6; padding: 9px 15px; color: #7b8177; font-size: 10px; }
.selected-slot strong { color: #456c4d; font-family: Consolas, "Courier New", monospace; font-size: 11px; }
.table-wrap { overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 10px; }
th,td { border-bottom: 1px solid #ebeeea; padding: 8px 9px; text-align: left; }
th { background: #f4f7f3; color: #5c695c; font-weight: 700; }
tbody tr:hover { background: #f8fbf7; }
tbody tr.row-active { background: #edf5eb; }
.code { color: #42644b; font-family: Consolas, "Courier New", monospace; }
.kind,.operation { display: inline-block; border-radius: 4px; padding: 2px 6px; background: #edf4ed; color: #47734d; font-size: 9px; font-weight: 700; }
.kind.carrier { background: #f2eef8; color: #695587; }
.operation.stockoff { background: #fff2e2; color: #98652f; }
.use-row { border: 0; background: transparent; color: #477954; font: inherit; font-weight: 700; cursor: pointer; }
.hint { margin: 9px 14px 12px; color: #7a8178; font-size: 10px; }
.empty { height: 70px; color: #8c9389; text-align: center; vertical-align: middle; }
.numeric { text-align: right; font-variant-numeric: tabular-nums; }
.card-footer { align-items: flex-start; padding: 9px 14px; color: #82887e; background: #fafbf9; font-size: 9px; }
@media (max-width: 1120px) { .control-grid { grid-template-columns: repeat(3, minmax(190px, 1fr)); } .work-area { grid-template-columns: 1fr; } }
@media (max-width: 700px) { .stocker-manager { padding: 12px; } .page-head { align-items: flex-start; } .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; } .control-grid { grid-template-columns: 1fr 1fr; } .content-field { grid-column: 1 / -1; } .mode-select { grid-column: 1 / -1; } .primary { min-height: 36px; } .section-head,.card-footer { align-items: flex-start; flex-direction: column; } .legend { width: 100%; } .rack-grid { grid-template-columns: repeat(4, minmax(60px, 1fr)); padding: 12px; } }
</style>
