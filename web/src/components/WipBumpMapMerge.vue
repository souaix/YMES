<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type ComponentRow = {
  componentNo: string
  waferLot: string
  waferId: number
  bumpCode: string
  bumpMapFileName: string
  bumpMapDocState: 0 | 1
  mergeState: 0 | 1
  uMapDocState: 0 | 1 | 2
  mergeDate: string
  freezingStatus: 0 | 1
  uMapFileName: string
  filePath: string
  selected: boolean
}
type LotFixture = {
  lotNo: string
  moNo: string
  status: number
  deviceNo: string
  customerLotNo: string
  productNo: string
  productVersion: string
  opNo: string
  customerNo: string
  productType: string
  currentQty: number
  currentUnit: string
  systemQty: number
  systemUnit: string
  goldenFileName: string
  goldenMapExists: boolean
  sourceTemplateNo: string
  referenceTemplateNo: string
  ruleName: string
  ruleMatch: string
  components: ComponentRow[]
}

const props = defineProps<{ developmentPreview?: boolean; initialLotNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const lotFixtures: LotFixture[] = [
  {
    lotNo: 'DEV-CP-LOT-001', moNo: 'DEV-MO-261121-CP01', status: 1,
    deviceNo: 'DEV-DEVICE-CP-08', customerLotNo: 'DEV-WAFERLOT-261121-08',
    productNo: 'DEV-PRODUCT-BUMP-08', productVersion: 'A2', opNo: 'DEV-OP-BUMP-MERGE',
    customerNo: 'DEV-CUSTOMER-08', productType: 'DEV-CP', currentQty: 8, currentUnit: 'PCS', systemQty: 8, systemUnit: 'PCS',
    goldenFileName: 'DEV-GOLDEN-CP08-A2.map', goldenMapExists: true,
    sourceTemplateNo: 'DEV-MAP-SOURCE-CP08', referenceTemplateNo: 'DEV-MAP-REF-CP08',
    ruleName: 'DEV approved CP bump merge rule', ruleMatch: 'Customer + Product Type + Product + Version exact match',
    components: [
      { componentNo: 'DEV-COMP-CP08-001', waferLot: 'DEV-WAFERLOT-261121-08', waferId: 1, bumpCode: 'DEV-BIN-A', bumpMapFileName: 'DEV-BUMP-CP08-W01.txt', bumpMapDocState: 1, mergeState: 0, uMapDocState: 0, mergeDate: '', freezingStatus: 0, uMapFileName: '', filePath: '', selected: false },
      { componentNo: 'DEV-COMP-CP08-002', waferLot: 'DEV-WAFERLOT-261121-08', waferId: 2, bumpCode: 'DEV-BIN-A', bumpMapFileName: 'DEV-BUMP-CP08-W02.txt', bumpMapDocState: 1, mergeState: 1, uMapDocState: 1, mergeDate: '2026/11/21 10:24:00', freezingStatus: 1, uMapFileName: 'DEV-UFMAP-CP08-W02.map', filePath: 'DEV://mapfiles/CP08/', selected: false },
      { componentNo: 'DEV-COMP-CP08-003', waferLot: 'DEV-WAFERLOT-261121-08', waferId: 3, bumpCode: 'DEV-BIN-B', bumpMapFileName: '', bumpMapDocState: 0, mergeState: 0, uMapDocState: 0, mergeDate: '', freezingStatus: 0, uMapFileName: '', filePath: '', selected: false },
      { componentNo: 'DEV-COMP-CP08-004', waferLot: 'DEV-WAFERLOT-261121-08', waferId: 4, bumpCode: 'DEV-BIN-B', bumpMapFileName: 'DEV-BUMP-CP08-W04.txt', bumpMapDocState: 1, mergeState: 0, uMapDocState: 2, mergeDate: '2026/11/20 15:40:00', freezingStatus: 0, uMapFileName: 'DEV-UFMAP-CP08-W04.map', filePath: 'DEV://mapfiles/CP08/', selected: false },
    ],
  },
  {
    lotNo: 'DEV-CP-LOT-002', moNo: 'DEV-MO-261121-CP02', status: 0,
    deviceNo: 'DEV-DEVICE-CP-12', customerLotNo: 'DEV-WAFERLOT-261121-12',
    productNo: 'DEV-PRODUCT-BUMP-12', productVersion: 'B1', opNo: 'DEV-OP-BUMP-MERGE',
    customerNo: 'DEV-CUSTOMER-12', productType: 'DEV-CP', currentQty: 4, currentUnit: 'PCS', systemQty: 4, systemUnit: 'PCS',
    goldenFileName: 'DEV-GOLDEN-CP12-B1.map', goldenMapExists: true,
    sourceTemplateNo: 'DEV-MAP-SOURCE-CP12', referenceTemplateNo: 'DEV-MAP-REF-CP12',
    ruleName: 'DEV approved CP wildcard rule', ruleMatch: 'Customer + Product Type exact; Product and Version wildcard',
    components: [],
  },
  {
    lotNo: 'DEV-CP-LOT-003', moNo: 'DEV-MO-261120-CP03', status: 4,
    deviceNo: 'DEV-DEVICE-CP-16', customerLotNo: 'DEV-WAFERLOT-261120-16',
    productNo: 'DEV-PRODUCT-BUMP-16', productVersion: 'C1', opNo: 'DEV-OP-BUMP-MERGE',
    customerNo: 'DEV-CUSTOMER-16', productType: 'DEV-CP', currentQty: 2, currentUnit: 'PCS', systemQty: 2, systemUnit: 'PCS',
    goldenFileName: '', goldenMapExists: false,
    sourceTemplateNo: 'DEV-MAP-SOURCE-16', referenceTemplateNo: 'DEV-MAP-REF-16',
    ruleName: 'DEV rule unavailable', ruleMatch: 'No approved rule found for this product fixture',
    components: [
      { componentNo: 'DEV-COMP-CP16-001', waferLot: 'DEV-WAFERLOT-261120-16', waferId: 1, bumpCode: 'DEV-BIN-C', bumpMapFileName: 'DEV-BUMP-CP16-W01.txt', bumpMapDocState: 1, mergeState: 0, uMapDocState: 0, mergeDate: '', freezingStatus: 0, uMapFileName: '', filePath: '', selected: false },
      { componentNo: 'DEV-COMP-CP16-002', waferLot: 'DEV-WAFERLOT-261120-16', waferId: 2, bumpCode: 'DEV-BIN-C', bumpMapFileName: 'DEV-BUMP-CP16-W02.txt', bumpMapDocState: 1, mergeState: 0, uMapDocState: 0, mergeDate: '', freezingStatus: 0, uMapFileName: '', filePath: '', selected: false },
    ],
  },
]

const lotNoInput = ref('')
const currentLot = ref<LotFixture | null>(null)
const components = ref<ComponentRow[]>([])
const errorText = ref('')
const statusText = ref('輸入生產批編號後按 Enter 載入。')
const busy = ref(false)
const progress = ref(0)
const progressLabel = ref('待命')
const previewTitle = ref('')
const previewOpen = ref(false)
const previewCells = ref<string[][]>([])
const previewIsGolden = ref(false)

const selectedRows = computed(() => components.value.filter(row => row.selected))
const eligibleCount = computed(() => components.value.filter(row => row.bumpMapDocState === 1).length)
const runningStatus = computed(() => currentLot.value ? statusName(currentLot.value.status) : '')
const goldenState = computed(() => currentLot.value?.goldenMapExists ? '已取得' : '未上傳')
const allEligibleSelected = computed(() => eligibleCount.value > 0 && components.value.filter(row => row.bumpMapDocState === 1).every(row => row.selected))

function statusName(status: number): string {
  const labels: Record<number, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 5: 'Outsource', 11: 'Inventory', 12: 'Scrap Inventory', 99: 'Closed' }
  return labels[status] || `Status ${status}`
}
function mergeStateName(status: number): string { return status === 1 ? '已凍結' : '未凍結' }
function bumpStateName(state: number): string { return state === 1 ? '檔案存在' : '檔案不存在' }
function uMapStateName(state: number): string { return state === 0 ? '未疊圖' : state === 1 ? '已疊圖' : '檔案不存在' }
function cloneRows(rows: ComponentRow[]): ComponentRow[] { return rows.map(row => ({ ...row })) }

function loadLot(): void {
  errorText.value = ''
  statusText.value = ''
  progress.value = 0
  progressLabel.value = '待命'
  currentLot.value = null
  components.value = []
  const lotNo = lotNoInput.value.trim().toUpperCase()
  if (!lotNo) { errorText.value = '請輸入生產批編號。'; return }
  if (!dev.value) { statusText.value = '正式批次服務尚未連接；DEV 假資料只在開發預覽提供。'; return }
  const fixture = lotFixtures.find(item => item.lotNo === lotNo)
  if (!fixture) { errorText.value = `DEV 假資料中找不到生產批：${lotNo}`; return }
  if (fixture.status === 0) { errorText.value = '來源先檢查 Status 0–5，另檢查 LotState 不可為 0；DEV 以 Queue 樣本示意此守門。'; return }
  currentLot.value = { ...fixture }
  components.value = cloneRows(fixture.components)
  statusText.value = `已載入 ${components.value.length} 個元件；Bump MAP 可用 ${eligibleCount.value} 個。`
  if (!fixture.goldenMapExists) statusText.value += ' Golden MAP 尚未上傳，無法疊圖。'
}

function selectAll(): void {
  components.value = components.value.map(row => ({ ...row, selected: row.bumpMapDocState === 1 }))
}
function unselectAll(): void { components.value = components.value.map(row => ({ ...row, selected: false })) }

function createPreview(title: string, seed: number, golden: boolean): void {
  previewTitle.value = title
  previewIsGolden.value = golden
  previewCells.value = Array.from({ length: 13 }, (_, y) => Array.from({ length: 21 }, (_, x) => {
    const dx = x - 10
    const dy = y - 6
    const inside = (dx * dx) / 95 + (dy * dy) / 31 < 1
    if (!inside) return ' '
    const value = (x * 7 + y * 5 + seed) % 17
    if (golden) return value < 4 ? '.' : value < 12 ? '1' : '2'
    return value < 3 ? '.' : value < 10 ? '1' : value < 14 ? 'A' : 'B'
  }))
  previewOpen.value = true
}
function viewGoldenMap(): void {
  if (!currentLot.value?.goldenMapExists) { errorText.value = 'Golden MAP 檔案不存在，無法查看。'; return }
  errorText.value = ''
  createPreview(currentLot.value.goldenFileName, 3, true)
}
function viewResult(row: ComponentRow): void {
  if (row.uMapDocState !== 1) return
  errorText.value = ''
  createPreview(row.uMapFileName, row.waferId + 9, false)
}

function waitForDevStep(): Promise<void> {
  return new Promise(resolve => window.setTimeout(resolve, 220))
}
async function mergeSelected(): Promise<void> {
  errorText.value = ''
  if (!currentLot.value || selectedRows.value.length === 0) { errorText.value = '請至少選取一個元件。'; return }
  if (!currentLot.value.goldenMapExists) { errorText.value = 'Golden MAP 檔案不存在，無法疊圖。'; return }
  if (selectedRows.value.some(row => row.bumpMapDocState !== 1)) { errorText.value = '所選元件缺少 Bump MAP 檔案。'; return }
  if (selectedRows.value.some(row => row.freezingStatus === 1)) { errorText.value = '已凍結的 UFMAP 不能再次手動疊圖；請先 UndoFrozen。'; return }
  if (currentLot.value.ruleName.includes('unavailable')) { errorText.value = '找不到核准中的 Merge MAP Rule。'; return }
  busy.value = true
  const batch = selectedRows.value.map(row => row.componentNo)
  const phases = [
    '檢查 Golden MAP 與已核准模板／疊圖規則',
    '下載 Golden MAP 與 Bump MAP（DEV 模擬）',
    '比對 Wafer Lot、Wafer ID、方向與座標（DEV 模擬）',
    '依模板及規則產生 UFMAP（DEV 模擬）',
    '上傳 UFMAP 並寫入凍結現況／歷程（DEV 模擬）',
  ]
  try {
    for (let index = 0; index < phases.length; index += 1) {
      progressLabel.value = phases[index]
      await waitForDevStep()
      progress.value = Math.round(((index + 1) / phases.length) * 100)
    }
    const now = new Date().toLocaleString('zh-TW', { hour12: false })
    components.value = components.value.map(row => batch.includes(row.componentNo)
      ? { ...row, mergeState: 1, uMapDocState: 1, freezingStatus: 1, mergeDate: now, uMapFileName: `DEV-UFMAP-${currentLot.value!.customerLotNo}-W${String(row.waferId).padStart(2, '0')}.map`, filePath: `DEV://mapfiles/${currentLot.value!.lotNo}/`, selected: false }
      : row)
    statusText.value = `DEV 模擬完成：${batch.length} 個元件已建立疊圖凍結現況與歷程。未讀寫 FTP 或產生實際 MAP 檔。`
    progressLabel.value = 'DEV 模擬完成'
  } finally {
    busy.value = false
  }
}

async function undoFrozen(): Promise<void> {
  errorText.value = ''
  if (!currentLot.value || selectedRows.value.length === 0) { errorText.value = '請選取已凍結的疊圖項目。'; return }
  if (selectedRows.value.some(row => row.freezingStatus !== 1)) { errorText.value = '來源要求每筆選取項目都必須是 FreezingStatus=1。'; return }
  const batchCount = selectedRows.value.length
  busy.value = true
  progressLabel.value = 'DEV 模擬解除凍結並寫入歷程'
  progress.value = 25
  await waitForDevStep()
  components.value = components.value.map(row => selectedRows.value.some(selected => selected.componentNo === row.componentNo)
    ? { ...row, mergeState: 0, freezingStatus: 0, selected: false }
    : row)
  progress.value = 100
  statusText.value = `DEV 模擬完成：${batchCount} 個元件已解除凍結；UFMAP 檔案仍保留。`
  progressLabel.value = 'DEV 模擬完成'
  busy.value = false
}

onMounted(() => {
  if (props.initialLotNo?.trim()) {
    lotNoInput.value = props.initialLotNo.trim().toUpperCase()
    loadLot()
  }
})
</script>

<template>
  <section class="bump-page" aria-label="Bump MAP 疊圖作業">
    <header class="page-title">
      <div><div class="title-line"><h1>Bump MAP 疊圖作業</h1><span class="function-badge">WPP07</span><span class="dev-badge">DEV MOCK</span></div><p>IC / CP Package · Merge Standard MAP</p></div>
      <button class="mes-button" :disabled="busy" @click="emit('close')">Close (&amp;X)</button>
    </header>

    <div class="lot-search">
      <label for="lot-no">生產批編號</label>
      <input id="lot-no" v-model="lotNoInput" class="text-input lot-entry" placeholder="例如 DEV-CP-LOT-001" :disabled="busy" @keyup.enter="loadLot">
      <button class="mes-button primary" :disabled="busy || !dev" @click="loadLot">載入批次 (Enter)</button>
      <span class="search-hint">DEV 假資料：001 可疊圖、002 Queue 守門、003 缺 Golden MAP</span>
    </div>

    <div v-if="errorText" class="message error" role="alert">{{ errorText }}</div>
    <div v-if="!dev" class="message info" role="status">正式批次與檔案服務尚未連接；本頁不會讀取真 Lot、FTP 或 MAP 檔案。</div>

    <section class="lot-card">
      <div class="section-heading"><h2>生產批資訊</h2><span v-if="currentLot" class="status-pill" :class="`status-${currentLot.status}`">{{ runningStatus }}</span></div>
      <div v-if="currentLot" class="lot-grid">
        <div><span>Lot No.</span><strong>{{ currentLot.lotNo }}</strong></div>
        <div><span>工單編號</span><strong>{{ currentLot.moNo }}</strong></div>
        <div><span>客戶批號</span><strong>{{ currentLot.customerLotNo }}</strong></div>
        <div><span>Device No.</span><strong>{{ currentLot.deviceNo }}</strong></div>
        <div><span>產品 / 版次</span><strong>{{ currentLot.productNo }} / {{ currentLot.productVersion }}</strong></div>
        <div><span>作業站</span><strong>{{ currentLot.opNo }}</strong></div>
        <div><span>客戶 / 產品類型</span><strong>{{ currentLot.customerNo }} / {{ currentLot.productType }}</strong></div>
        <div><span>目前數量</span><strong>{{ currentLot.currentQty }} {{ currentLot.currentUnit }}</strong></div>
        <div><span>系統數量</span><strong>{{ currentLot.systemQty }} {{ currentLot.systemUnit }}</strong></div>
      </div>
      <div v-else class="lot-empty">載入生產批後顯示工單、產品、作業站與數量。</div>
    </section>

    <section class="component-card">
      <div class="section-heading component-heading"><div><h2>元件及 Bump MAP 資訊</h2><small v-if="currentLot">{{ components.length }} 個元件 · {{ eligibleCount }} 個 Bump MAP 可用</small></div><div class="toolbar">
        <button class="mes-button" :disabled="busy || !currentLot || eligibleCount === 0" @click="selectAll">全部選擇</button>
        <button class="mes-button" :disabled="busy || !currentLot" @click="unselectAll">全部不選</button>
        <button class="mes-button" :disabled="busy || !currentLot || !currentLot.goldenMapExists" @click="viewGoldenMap">查看 Golden MAP</button>
        <button class="mes-button primary" :disabled="busy || selectedRows.length === 0" @click="mergeSelected">MergeMAP</button>
        <button class="mes-button" :disabled="busy || selectedRows.length === 0" @click="undoFrozen">UndoFrozen</button>
      </div></div>

      <div v-if="currentLot" class="map-status-strip">
        <div class="golden-file" :class="currentLot.goldenMapExists ? 'file-ok' : 'file-missing'"><span>Golden MAP</span><strong>{{ currentLot.goldenMapExists ? currentLot.goldenFileName : '檔案未上傳' }}</strong><em>{{ goldenState }}</em></div>
        <div class="rule-file"><span>核准疊圖規則</span><strong>{{ currentLot.ruleName }}</strong><em>{{ currentLot.ruleMatch }}</em></div>
        <div class="template-file"><span>Source / Reference Template</span><strong>{{ currentLot.sourceTemplateNo }} / {{ currentLot.referenceTemplateNo }}</strong><em>DEV 模擬設定</em></div>
      </div>

      <div class="grid-wrap">
        <table class="result-grid">
          <thead><tr><th class="select-col"><input type="checkbox" :checked="allEligibleSelected" :disabled="busy || eligibleCount === 0" aria-label="選取所有可用 Bump MAP" @change="($event.target as HTMLInputElement).checked ? selectAll() : unselectAll()"></th><th>ComponentNo</th><th>WaferLot</th><th>WaferID</th><th>BumpCode</th><th>Bump MAP</th><th>UFMAP</th><th>Merge State</th><th>Merge Date</th><th>UFMAP File</th><th>Result</th></tr></thead>
          <tbody>
            <tr v-for="row in components" :key="row.componentNo" :class="{ unavailable: row.bumpMapDocState !== 1 }">
              <td class="select-col"><input v-model="row.selected" type="checkbox" :disabled="busy || row.bumpMapDocState !== 1" :aria-label="`選取 ${row.componentNo}`"></td>
              <td>{{ row.componentNo }}</td><td>{{ row.waferLot }}</td><td class="number-cell">{{ row.waferId }}</td><td>{{ row.bumpCode }}</td>
              <td><span class="file-state" :class="row.bumpMapDocState === 1 ? 'ok' : 'missing'">{{ bumpStateName(row.bumpMapDocState) }}</span><small v-if="row.bumpMapFileName">{{ row.bumpMapFileName }}</small></td>
              <td><span class="file-state" :class="row.uMapDocState === 1 ? 'ok' : row.uMapDocState === 2 ? 'missing' : 'idle'">{{ uMapStateName(row.uMapDocState) }}</span></td>
              <td><span class="freeze-state" :class="row.freezingStatus === 1 ? 'frozen' : 'unfrozen'">{{ mergeStateName(row.freezingStatus) }}</span></td>
              <td>{{ row.mergeDate || '—' }}</td><td>{{ row.uMapFileName || '—' }}</td>
              <td><button v-if="row.uMapDocState === 1" class="result-button" @click="viewResult(row)">查看結果</button><span v-else class="muted">—</span></td>
            </tr>
            <tr v-if="components.length === 0"><td colspan="11" class="empty">{{ currentLot ? '此 DEV 生產批沒有元件資料。' : '輸入生產批編號後載入元件。' }}</td></tr>
          </tbody>
        </table>
      </div>

      <div v-if="busy || progress > 0" class="progress-block"><div class="progress-label"><span>{{ progressLabel }}</span><strong>{{ progress }}%</strong></div><div class="progress-track"><div class="progress-value" :style="{ width: `${progress}%` }"></div></div></div>
      <footer class="statusbar"><span>{{ statusText }}</span><span>選取 {{ selectedRows.length }} / {{ components.length }} · Merge MAP Mode: Manual</span></footer>
    </section>

    <div v-if="previewOpen" class="overlay" role="dialog" aria-modal="true" :aria-label="previewTitle">
      <section class="preview-dialog">
        <header><div><strong>{{ previewIsGolden ? 'Golden MAP' : 'UFMAP 結果' }}</strong><small>{{ previewTitle }} · DEV 示意圖</small></div><button class="mes-button" @click="previewOpen = false">Close</button></header>
        <div class="map-legend"><span><i class="die-good">1</i> 有效 Bin</span><span><i class="die-a">A</i> Bump Code A</span><span><i class="die-b">B</i> Bump Code B</span><span><i class="die-empty">.</i> 空 Die</span></div>
        <div class="wafer-map"><div v-for="(line, y) in previewCells" :key="y" class="map-line"><span v-for="(cell, x) in line" :key="x" class="die" :class="cell === ' ' ? 'outside' : cell === '.' ? 'empty-die' : cell === 'A' ? 'bin-a' : cell === 'B' ? 'bin-b' : 'good-die'">{{ cell }}</span></div></div>
        <p class="preview-note">畫面僅示意 Golden／合併後地圖預覽。DEV 不解析或寫入實際 MAP binary。</p>
      </section>
    </div>
  </section>
</template>

<style scoped>
.bump-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:10px;padding:12px;box-sizing:border-box;background:#f2f4f5;color:#30383d;font:13px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;justify-content:space-between;align-items:center;gap:12px}.page-title h1{margin:0;font-size:20px;font-weight:600}.page-title p{margin:4px 0 0;color:#718088;font-size:12px}.title-line{display:flex;align-items:center;gap:8px}.function-badge,.dev-badge{padding:3px 7px;border-radius:3px;background:#e4edf2;color:#42616e;font:11px Consolas,monospace}.dev-badge{background:#fff0ca;color:#795617}.mes-button{height:30px;padding:4px 11px;border:1px solid #aab5ba;border-radius:2px;background:#f8f9fa;color:#35434a;white-space:nowrap;cursor:pointer}.mes-button:hover:not(:disabled){background:#e9eff2}.mes-button:disabled{opacity:.48;cursor:not-allowed}.mes-button.primary{border-color:#327897;background:#347c9b;color:#fff}.lot-search{display:flex;align-items:center;gap:8px;padding:10px;border:1px solid #b8c3c8;background:#fff}.lot-search>label{font-weight:600;white-space:nowrap}.text-input{height:31px;box-sizing:border-box;padding:5px 8px;border:1px solid #b8c3c8;border-radius:2px;background:#fff;color:#2d383e}.lot-entry{width:min(290px,35vw);font-family:Consolas,monospace}.search-hint{margin-left:auto;color:#718087;font-size:11px}.message{padding:7px 10px;border:1px solid}.message.error{border-color:#d38d8d;background:#fff0f0;color:#a23636}.message.info{border-color:#a2bdc9;background:#eef7fa;color:#3b6573}.lot-card,.component-card{display:flex;flex-direction:column;min-height:0;border:1px solid #b7c1c6;background:#fff}.lot-card{flex:0 0 auto;padding:10px}.section-heading{display:flex;align-items:center;justify-content:space-between;gap:10px}.section-heading h2{margin:0;font-size:14px;font-weight:600}.section-heading small{color:#738088}.lot-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px 16px;margin-top:10px}.lot-grid>div{display:flex;align-items:center;gap:9px;min-width:0}.lot-grid span{width:108px;flex:0 0 108px;color:#718087;text-align:right}.lot-grid strong{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font:12px Consolas,"Microsoft JhengHei",sans-serif}.lot-empty{padding:16px 0;color:#7b868b;text-align:center}.status-pill{padding:3px 9px;border:1px solid #bdd4c4;border-radius:20px;background:#eaf5ed;color:#39724c;font-size:11px}.status-pill.status-3{border-color:#ddc7a0;background:#fff4dc;color:#805e1f}.status-pill.status-4{border-color:#b8c8d6;background:#eef3f7;color:#526b7b}.component-card{flex:1 1 0;padding:10px;overflow:hidden}.component-heading{align-items:flex-start}.toolbar{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:6px}.map-status-strip{display:grid;grid-template-columns:1.2fr 1fr 1fr;gap:8px;margin:10px 0}.map-status-strip>div{display:flex;flex-direction:column;gap:3px;min-width:0;padding:7px 9px;border:1px solid #d3dadd;background:#fafbfb}.map-status-strip span{color:#78858b;font-size:10px;text-transform:uppercase}.map-status-strip strong{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:12px}.map-status-strip em{overflow:hidden;color:#75828a;font-size:10px;font-style:normal;text-overflow:ellipsis;white-space:nowrap}.golden-file.file-ok{border-left:3px solid #63a078}.golden-file.file-missing{border-left:3px solid #c77070}.grid-wrap{flex:1;min-height:130px;overflow:auto;border:1px solid #bbc4c9}.result-grid{width:100%;border-collapse:collapse;white-space:nowrap}.result-grid th{position:sticky;top:0;z-index:1;padding:7px 8px;border:1px solid #c0c9cd;background:#e3ecef;text-align:left;font-size:11px;font-weight:600}.result-grid td{padding:5px 8px;border:1px solid #d1d8db;background:#fff;vertical-align:middle}.result-grid tbody tr:hover td{background:#f5f9fa}.result-grid tbody tr.unavailable td{background:#eef0f1;color:#859096}.result-grid .select-col{width:34px;text-align:center}.select-col input{accent-color:#367e9d}.result-grid td small{display:block;margin-top:3px;color:#7d898e;font:10px Consolas,monospace}.number-cell{text-align:right;font-family:Consolas,monospace}.file-state,.freeze-state{display:inline-block;padding:3px 7px;border-radius:10px;font-size:10px}.file-state.ok{background:#e7f4eb;color:#34714a}.file-state.missing{background:#fae9e9;color:#9f4545}.file-state.idle{background:#edf0f2;color:#67747a}.freeze-state.frozen{background:#fff1d4;color:#835d15}.freeze-state.unfrozen{background:#eef1f3;color:#5d6b72}.result-button{padding:4px 8px;border:1px solid #d6b75a;border-radius:2px;background:#fff8de;color:#685116;cursor:pointer}.muted{color:#899398}.empty{text-align:center;color:#7c878c;height:70px}.progress-block{padding:6px 1px}.progress-label{display:flex;justify-content:space-between;margin-bottom:5px;color:#61727a;font-size:11px}.progress-track{height:8px;overflow:hidden;background:#e4e9eb}.progress-value{height:100%;background:#438cab;transition:width .15s ease}.statusbar{display:flex;justify-content:space-between;align-items:center;gap:12px;min-height:24px;padding:0 3px;border-top:1px solid #d1d7da;color:#6d7a80;font-size:10px}.overlay{position:fixed;inset:0;z-index:1600;display:grid;place-items:center;padding:18px;background:#0007}.preview-dialog{width:min(760px,94vw);max-height:92vh;display:flex;flex-direction:column;gap:11px;padding:13px;border:1px solid #aeb9be;background:#fff;box-shadow:0 14px 48px #0004}.preview-dialog>header{display:flex;align-items:center;justify-content:space-between}.preview-dialog>header>div{display:flex;flex-direction:column;gap:3px}.preview-dialog>header small{color:#718087;font-size:11px}.map-legend{display:flex;flex-wrap:wrap;gap:12px;color:#69767b;font-size:10px}.map-legend span{display:flex;align-items:center;gap:5px}.map-legend i{display:inline-grid;width:17px;height:17px;place-items:center;border:1px solid #cbd4d8;font:10px Consolas,monospace;font-style:normal}.die-good,.good-die{background:#dcecf3;color:#37637a}.die-a,.bin-a{background:#e9e3fa;color:#634e94}.die-b,.bin-b{background:#f8e3d6;color:#8a5939}.die-empty,.empty-die{background:#eef0f1;color:#9aa3a7}.wafer-map{align-self:center;display:flex;flex-direction:column;gap:2px;padding:12px;border:1px solid #d4dcdf;border-radius:4px;background:#fafbfb}.map-line{display:flex;gap:2px}.die{width:21px;height:21px;display:grid;place-items:center;border-radius:50%;font:10px Consolas,monospace}.die.outside{visibility:hidden}.preview-note{margin:0;color:#829096;font-size:10px}@media(max-width:1000px){.lot-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.component-heading{flex-direction:column}.toolbar{justify-content:flex-start}.map-status-strip{grid-template-columns:1fr 1fr}.search-hint{display:none}}@media(max-width:650px){.lot-search{flex-wrap:wrap}.lot-entry{flex:1;width:auto}.lot-grid{grid-template-columns:1fr}.lot-grid>div{justify-content:space-between}.lot-grid span{width:auto;flex:auto;text-align:left}.map-status-strip{grid-template-columns:1fr}.page-title h1{font-size:17px}.statusbar{align-items:flex-start;flex-direction:column;padding:5px}.component-card{overflow:auto}.toolbar .mes-button{padding:4px 7px;font-size:11px}}
</style>
