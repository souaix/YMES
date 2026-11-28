<script setup lang="ts">
import { computed, ref } from 'vue'

type LotLog = {
  lotNo: string
  opNo: string
  opName: string
  inputQty: number
  goodQty: number
  failQty: number
  startTime: string
  endTime: string
  logGroupSerial: string
  status: number
}
type AttributeLog = {
  componentNo: string | null
  attribNo: string
  attribName: string
  attribValueOrg: string
  attribValueNew: string
  adjustReason: string
  revisor: string
  reviseDate: string
  logGroupSerial: string
}

const emit = defineEmits<{ close: [] }>()
const fixtures: LotLog[] = [
  { lotNo: 'DEV-ATTR001', opNo: 'OP-100', opName: '晶圓測試', inputQty: 1200, goodQty: 1178, failQty: 22, startTime: '2026-11-25T08:10:00', endTime: '2026-11-25T09:02:00', logGroupSerial: 'DEV-RPT801-001', status: 1 },
  { lotNo: 'DEV-ATTR001', opNo: 'OP-210', opName: '外觀檢查', inputQty: 1178, goodQty: 1162, failQty: 16, startTime: '2026-11-26T13:25:00', endTime: '2026-11-26T14:11:00', logGroupSerial: 'DEV-RPT801-002', status: 1 },
  { lotNo: 'DEV-ATTR002', opNo: 'OP-100', opName: '晶圓測試', inputQty: 800, goodQty: 792, failQty: 8, startTime: '2026-11-24T10:00:00', endTime: '2026-11-24T10:37:00', logGroupSerial: 'DEV-RPT801-003', status: 1 },
]
const attributeFixtures: AttributeLog[] = [
  { componentNo: null, attribNo: 'THICKNESS', attribName: '膜厚', attribValueOrg: '1.24', attribValueNew: '1.26', adjustReason: '複測後修正', revisor: 'DEMO', reviseDate: '2026-11-25T09:10:12', logGroupSerial: 'DEV-RPT801-001' },
  { componentNo: null, attribNo: 'YIELD', attribName: '良率', attribValueOrg: '97.8', attribValueNew: '98.1', adjustReason: '實驗室結果回填', revisor: 'DEMO', reviseDate: '2026-11-25T09:12:40', logGroupSerial: 'DEV-RPT801-001' },
  { componentNo: 'DIE-A01', attribNo: 'BIN', attribName: '測試分類', attribValueOrg: 'B2', attribValueNew: 'B1', adjustReason: '複判調整', revisor: 'DEMO', reviseDate: '2026-11-25T09:15:00', logGroupSerial: 'DEV-RPT801-001' },
  { componentNo: 'DIE-A02', attribNo: 'BIN', attribName: '測試分類', attribValueOrg: 'B3', attribValueNew: 'B2', adjustReason: '複判調整', revisor: 'DEMO', reviseDate: '2026-11-25T09:16:30', logGroupSerial: 'DEV-RPT801-001' },
  { componentNo: null, attribNo: 'GRADE', attribName: '外觀等級', attribValueOrg: 'B', attribValueNew: 'A', adjustReason: '外觀照片覆核', revisor: 'DEMO', reviseDate: '2026-11-26T14:18:05', logGroupSerial: 'DEV-RPT801-002' },
  { componentNo: 'DIE-B11', attribNo: 'MARK', attribName: '標記', attribValueOrg: 'HOLD', attribValueNew: 'PASS', adjustReason: '複驗完成', revisor: 'DEMO', reviseDate: '2026-11-26T14:22:00', logGroupSerial: 'DEV-RPT801-002' },
]

const lotNo = ref('')
const rows = ref<LotLog[]>([])
const queried = ref(false)
const selectedSerial = ref('')
const detailLoaded = ref(false)
const activeTab = ref<'lot' | 'component'>('lot')
const message = ref('輸入 Lot No 後查詢。選取一筆報工歷程，再按 Detail 載入該筆異動明細。')
const selectedRow = computed(() => rows.value.find(row => row.logGroupSerial === selectedSerial.value) ?? null)
const activeDetails = computed(() => {
  if (!detailLoaded.value || !selectedSerial.value) return []
  return attributeFixtures
    .filter(row => row.logGroupSerial === selectedSerial.value && (activeTab.value === 'lot' ? row.componentNo == null || row.componentNo === '' : row.componentNo != null && row.componentNo !== ''))
    .sort((a, b) => (a.componentNo ?? '').localeCompare(b.componentNo ?? '') || a.attribNo.localeCompare(b.attribNo) || a.reviseDate.localeCompare(b.reviseDate))
})

function formatTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

function selectLotNo(event: FocusEvent) {
  if (event.currentTarget instanceof HTMLInputElement) event.currentTarget.select()
}

function query() {
  const value = lotNo.value.trim().toUpperCase()
  lotNo.value = value
  if (!value) {
    rows.value = []
    queried.value = false
    selectedSerial.value = ''
    detailLoaded.value = false
    message.value = 'Lot No 不可空白。'
    return
  }
  rows.value = fixtures.filter(row => row.lotNo === value && row.status === 1).sort((a, b) => a.startTime.localeCompare(b.startTime))
  queried.value = true
  selectedSerial.value = ''
  detailLoaded.value = false
  message.value = `查詢完成：${rows.value.length} 筆 DEV 報工歷程。舊版固定 Status=1，按 StartTime 遞增排序。`
}

function selectRow(row: LotLog) {
  selectedSerial.value = row.logGroupSerial
  detailLoaded.value = false
  message.value = `已選取 ${row.opNo} / ${row.logGroupSerial}；按 Detail 載入特性異動。`
}

function showDetail() {
  if (!selectedRow.value) {
    message.value = '請先選取一筆報工歷程。'
    return
  }
  detailLoaded.value = true
  activeTab.value = 'lot'
  message.value = `已載入 ${selectedRow.value.logGroupSerial} 的 DEV 異動明細；批特性與元件特性依 ComponentNo 分頁。`
}
</script>

<template>
  <section class="adjust-log" aria-label="生產批作業站製程特性值調整記錄 DEV 預覽">
    <header class="head">
      <div><h1>生產批作業站製程特性值調整記錄 <small>RT801</small></h1><p>DEV 假資料；只在本頁記憶體查詢，未連資料庫</p></div>
      <button type="button" @click="emit('close')">Close (X)</button>
    </header>

    <form class="query" @submit.prevent="query">
      <label for="lot-no">Lot No</label>
      <input id="lot-no" v-model="lotNo" autocomplete="off" autofocus @focus="selectLotNo" @blur="lotNo = lotNo.trim().toUpperCase()" />
      <button type="submit">Query (Q)</button>
    </form>

    <p class="notice" role="status">{{ message }}</p>
    <section class="history" aria-label="Lot OP History">
      <div class="section-head"><h2>Lot OP History</h2><button type="button" :disabled="!selectedRow" @click="showDetail">Detail</button></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>LotNo</th><th>OPNo</th><th>OPName</th><th class="num">InputQty</th><th class="num">GoodQty</th><th class="num">FailQty</th><th>StartTime</th><th>EndTime</th></tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="row.logGroupSerial" tabindex="0" :class="{ selected: selectedSerial === row.logGroupSerial }" :aria-selected="selectedSerial === row.logGroupSerial" @click="selectRow(row)" @keydown.enter.space.prevent="selectRow(row)">
              <td>{{ row.lotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td class="num">{{ row.inputQty }}</td><td class="num">{{ row.goodQty }}</td><td class="num">{{ row.failQty }}</td><td>{{ formatTime(row.startTime) }}</td><td>{{ formatTime(row.endTime) }}</td>
            </tr>
            <tr v-if="!rows.length"><td colspan="8" class="empty">{{ queried ? '沒有符合的報工歷程' : '查詢結果會顯示在此處' }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="details" aria-label="特性異動明細">
      <div class="tabs" role="tablist" aria-label="特性類型">
        <button type="button" role="tab" :aria-selected="activeTab === 'lot'" :class="{ active: activeTab === 'lot' }" @click="activeTab = 'lot'">Lot Attrib</button>
        <button type="button" role="tab" :aria-selected="activeTab === 'component'" :class="{ active: activeTab === 'component' }" @click="activeTab = 'component'">Component Attrib</button>
      </div>
      <div class="table-wrap detail-wrap">
        <table>
          <thead><tr><th v-if="activeTab === 'component'">ComponentNo</th><th>AttribNo</th><th>AttribName</th><th>AttribValueOrg</th><th>AttribValueNew</th><th>AdjustReason</th><th>Revisor</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="(row, index) in activeDetails" :key="`${row.logGroupSerial}-${row.componentNo}-${row.attribNo}-${row.reviseDate}-${index}`">
              <td v-if="activeTab === 'component'">{{ row.componentNo }}</td><td>{{ row.attribNo }}</td><td>{{ row.attribName }}</td><td>{{ row.attribValueOrg }}</td><td>{{ row.attribValueNew }}</td><td class="reason">{{ row.adjustReason }}</td><td>{{ row.revisor }}</td><td>{{ formatTime(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!activeDetails.length"><td :colspan="activeTab === 'component' ? 8 : 7" class="empty">{{ detailLoaded ? '此類別沒有異動記錄' : '選取報工歷程並按 Detail 後載入' }}</td></tr>
          </tbody>
        </table>
      </div>
      <footer><span>{{ rows.length }} 筆報工歷程 · {{ activeDetails.length }} 筆{{ activeTab === 'lot' ? '批' : '元件' }}特性異動</span><span>所有操作只使用 DEV fixture，不執行調整或寫入歷程。</span></footer>
    </section>
  </section>
</template>

<style scoped>
.adjust-log{height:100%;min-height:440px;display:flex;flex-direction:column;gap:9px;padding:12px;box-sizing:border-box;background:#f7f5ee;color:#292820;font:13px/1.4 "Segoe UI",Arial,sans-serif}.head,.section-head{display:flex;align-items:center;justify-content:space-between;gap:12px}.head{border-bottom:1px solid #cbc5b4;padding-bottom:7px}.head h1{margin:0;font-size:18px}.head h1 small{color:#777;font-size:12px}.head p{margin:2px 0 0;color:#777}.head button,.query button,.section-head button,.tabs button{min-height:28px;padding:4px 10px;border:1px solid #777365;border-radius:3px;background:#fff;color:#302f29;font:inherit;cursor:pointer}.query{display:flex;align-items:center;gap:8px}.query label{min-width:64px;text-align:right}.query input{width:210px;padding:5px 6px;border:1px solid #aaa594;background:#fff;color:#292820;font:inherit}.query button,.section-head button{background:#39485e;border-color:#39485e;color:#fff}.section-head h2{margin:0;font-size:14px}.section-head button:disabled{opacity:.5;cursor:default}.notice{margin:0;padding:5px 8px;background:#eee9d9;color:#59574b}.history{display:flex;flex-direction:column;gap:5px;min-height:150px;flex:1}.details{display:flex;flex-direction:column;gap:5px;min-height:190px;flex:1.1}.table-wrap{min-height:100px;flex:1;overflow:auto;border:1px solid #aaa594;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap}th,td{padding:5px 7px;border:1px solid #d2cebf;text-align:left}th{position:sticky;top:0;z-index:1;background:#e9e5d7;font-weight:600}td.num,th.num{text-align:right}tbody tr[aria-selected="true"]{background:#dce6ef}tbody tr[aria-selected="true"] td{border-color:#9eb2c5}.empty{text-align:center;color:#888;padding:16px}.tabs{display:flex;gap:4px}.tabs button{border-radius:3px 3px 0 0}.tabs button.active{background:#e2decf;border-color:#aaa594;font-weight:600}.detail-wrap{min-height:100px}.reason{max-width:260px;white-space:normal}footer{display:flex;justify-content:space-between;gap:8px;color:#777;font-size:11px}@media(max-width:760px){.adjust-log{overflow:auto}.head{align-items:flex-start}.query{flex-wrap:wrap}.details,.history{min-height:220px}footer{flex-direction:column}}
</style>
