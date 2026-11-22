<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type Lot = { lotNo: string; moNo: string; customerLotNo: string; customerNo: string; productNo: string; deviceNo: string; routeCard: string }
type Wafer = { id: string; slotNo: number; waferId: string; serialNo: string; opNo: string; waferFlat: string; totalTestQty: number; totalPassQty: number; uploadTime: string; currentVersion: number; hasBinDefine: boolean; selected: boolean }
type Basis = { customerLotNo: string; productNo: string; functionFlag: number; targetFolder: string; filePattern: string; compressAllFile: boolean; ftpFlag: boolean; shipMapOption: string }
type ExportDetail = { id: string; itemNo: number; exportFormat: string; categoryName: string; targetFolder: string; filePattern: string }
type OutputRow = { id: string; waferId: string; format: string; fileName: string; target: string; result: string }
type Activity = { id: number; time: string; action: string; detail: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wpp02-wafer-map-export.v1'
const lots: Lot[] = [
  { lotNo: 'DEVLOT-CP-1001', moNo: 'DEVMO-CP-1001', customerLotNo: 'DEV-CUSTLOT-01', customerNo: 'DEV-CUST-01', productNo: 'DEV-CP-A01', deviceNo: 'DEV-DEVICE-A', routeCard: 'DEV-RC-CP-01' },
  { lotNo: 'DEVLOT-CP-1002', moNo: 'DEVMO-CP-1002', customerLotNo: 'DEV-CUSTLOT-02', customerNo: 'DEV-CUST-02', productNo: 'DEV-CP-B02', deviceNo: 'DEV-DEVICE-B', routeCard: 'DEV-RC-CP-02' },
  { lotNo: 'DEVLOT-CP-1003', moNo: 'DEVMO-CP-1003', customerLotNo: 'DEV-CUSTLOT-03', customerNo: 'DEV-CUST-03', productNo: 'DEV-CP-C03', deviceNo: 'DEV-DEVICE-C', routeCard: 'DEV-RC-CP-03' },
]
const mapFixtures: Record<string, Wafer[]> = {
  'DEVLOT-CP-1001': [
    { id: 'WM-01', slotNo: 1, waferId: 'DEV-WAF-A001', serialNo: 'DEV-SER-A001', opNo: 'DEV-CP10', waferFlat: '0', totalTestQty: 4120, totalPassQty: 3994, uploadTime: '2026-11-22 08:42:11', currentVersion: 1, hasBinDefine: true, selected: false },
    { id: 'WM-02', slotNo: 2, waferId: 'DEV-WAF-A002', serialNo: 'DEV-SER-A002', opNo: 'DEV-CP10', waferFlat: '0', totalTestQty: 4108, totalPassQty: 3976, uploadTime: '2026-11-22 08:48:32', currentVersion: 1, hasBinDefine: true, selected: false },
    { id: 'WM-03', slotNo: 3, waferId: 'DEV-WAF-A003', serialNo: 'DEV-SER-A003', opNo: 'DEV-CP20', waferFlat: '180', totalTestQty: 4088, totalPassQty: 3951, uploadTime: '2026-11-22 09:03:54', currentVersion: 1, hasBinDefine: true, selected: false },
    { id: 'WM-04', slotNo: 4, waferId: 'DEV-WAF-A004', serialNo: 'DEV-SER-A004', opNo: 'DEV-CP20', waferFlat: '180', totalTestQty: 4116, totalPassQty: 3990, uploadTime: '2026-11-22 09:10:20', currentVersion: 1, hasBinDefine: true, selected: false },
  ],
  'DEVLOT-CP-1002': [],
  'DEVLOT-CP-1003': [
    { id: 'WM-31', slotNo: 1, waferId: 'DEV-WAF-C001', serialNo: 'DEV-SER-C001', opNo: 'DEV-CP10', waferFlat: '0', totalTestQty: 3950, totalPassQty: 3821, uploadTime: '2026-11-21 13:22:05', currentVersion: 1, hasBinDefine: true, selected: false },
  ],
}
const basisFixtures: Basis[] = [
  { customerLotNo: 'DEV-CUSTLOT-01', productNo: 'DEV-CP-A01', functionFlag: 1, targetFolder: 'DEV://wafer-export/automatic', filePattern: '[LotNo]_[FLOW]_[WF_ID]', compressAllFile: false, ftpFlag: false, shipMapOption: 'SHIP MAP' },
  { customerLotNo: 'DEV-CUSTLOT-01', productNo: 'DEV-CP-A01', functionFlag: 0, targetFolder: 'DEV://wafer-export/manual', filePattern: '[LotNo]_[FLOW]_[WF_ID]', compressAllFile: true, ftpFlag: false, shipMapOption: 'SHIP MAP' },
  { customerLotNo: '*', productNo: 'DEV-CP-C03', functionFlag: 1, targetFolder: 'DEV://wafer-export/fallback', filePattern: '[LotNo]_[FLOW]_[WaferCount]', compressAllFile: false, ftpFlag: true, shipMapOption: 'LAST FULL TEST' },
]
const detailFixtures: Record<string, ExportDetail[]> = {
  'DEV-CUSTLOT-01|DEV-CP-A01|1': [
    { id: 'ED-A1', itemNo: 1, exportFormat: 'WaferMAPNormal1', categoryName: 'CustomerMAP', targetFolder: 'DEV://wafer-export/automatic', filePattern: '[LotNo]_[FLOW]_[WF_ID].map' },
    { id: 'ED-A2', itemNo: 2, exportFormat: 'BinSummary', categoryName: 'BinSummary', targetFolder: 'DEV://wafer-export/automatic', filePattern: '[LotNo]_[FLOW]_[WF_ID].txt' },
    { id: 'ED-A3', itemNo: 3, exportFormat: 'BinSummary ExcelFile', categoryName: 'Excel', targetFolder: 'DEV://wafer-export/automatic', filePattern: '[LotNo]_[FLOW]_[WF_ID].xlsx' },
  ],
  'DEV-CUSTLOT-01|DEV-CP-A01|0': [
    { id: 'ED-M1', itemNo: 1, exportFormat: 'WaferMAPNormal1', categoryName: 'CustomerMAP', targetFolder: 'DEV://wafer-export/manual', filePattern: '[LotNo]_[FLOW]_[WF_ID].map' },
    { id: 'ED-M2', itemNo: 2, exportFormat: 'BinSummary', categoryName: 'BinSummary', targetFolder: 'DEV://wafer-export/manual', filePattern: '[LotNo]_[FLOW]_[WF_ID].txt' },
  ],
  '*|DEV-CP-C03|1': [
    { id: 'ED-C1', itemNo: 1, exportFormat: 'InkMAPNormal', categoryName: 'InkMAP', targetFolder: 'DEV://wafer-export/fallback', filePattern: '[LotNo]_[FLOW]_[WF_ID].map' },
  ],
}

const lotInput = ref('DEVLOT-CP-1001')
const currentLot = ref<Lot | null>(null)
const waferRows = ref<Wafer[]>([])
const mappingBasis = ref<Basis | null>(null)
const activeBasis = ref<Basis | null>(null)
const exportDetails = ref<ExportDetail[]>([])
const functionFlag = ref(1)
const outputRows = ref<OutputRow[]>([])
const activity = ref<Activity[]>([])
const notice = ref('輸入 DEV LotNo 查詢；匯出只產生本機假 manifest，不會建立檔案。')
const error = ref('')
const folderDialog = ref(false)
const folderEditId = ref('')
const folderEditValue = ref('')
const nextId = ref(1)
const selectedCount = computed(() => waferRows.value.filter(row => row.selected).length)
const allSelected = computed(() => waferRows.value.length > 0 && waferRows.value.every(row => row.selected))
const outputCount = computed(() => outputRows.value.filter(row => row.result === '待產生').length)
const modeLabel = computed(() => functionFlag.value === 1 ? 'Automatic / FunctionFlag 1' : 'Manual / FunctionFlag 0')
const matchedConfigLabel = computed(() => mappingBasis.value ? mappingBasis.value.customerLotNo + ' × ' + mappingBasis.value.productNo : '未找到設定')

function nowText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function addActivity(action: string, detail: string): void {
  activity.value.unshift({ id: nextId.value++, time: nowText(), action, detail })
  activity.value = activity.value.slice(0, 25)
  persist()
}
function persist(): void {
  if (!dev.value) return
  try { localStorage.setItem(storageKey, JSON.stringify(activity.value)) }
  catch { notice.value = '瀏覽器無法儲存 DEV 操作紀錄；目前畫面仍可操作。' }
}
function resolveMapping(lot: Lot): Basis | null {
  const ranked = [
    [lot.customerLotNo, lot.productNo], ['*', lot.productNo], [lot.customerLotNo, '*'], ['*', '*'],
  ]
  for (const [customerLotNo, productNo] of ranked) {
    const found = basisFixtures.find(row => row.customerLotNo === customerLotNo && row.productNo === productNo)
    if (found) return found
  }
  return null
}
function applyMode(flag: number): void {
  functionFlag.value = flag
  outputRows.value = []
  if (!currentLot.value || !mappingBasis.value) { activeBasis.value = null; exportDetails.value = []; return }
  const key = mappingBasis.value.customerLotNo + '|' + mappingBasis.value.productNo + '|' + flag
  const fallbackKey = '*|' + currentLot.value.productNo + '|' + flag
  activeBasis.value = basisFixtures.find(row => row.customerLotNo === mappingBasis.value?.customerLotNo && row.productNo === mappingBasis.value?.productNo && row.functionFlag === flag) ?? null
  exportDetails.value = detailFixtures[key] ?? detailFixtures[fallbackKey] ?? []
  if (activeBasis.value) {
    activeBasis.value = { ...activeBasis.value }
    exportDetails.value = exportDetails.value.map(row => ({ ...row }))
  }
}
function queryLot(): void {
  error.value = ''
  outputRows.value = []
  const lotNo = lotInput.value.trim().toUpperCase()
  const lot = lots.find(row => row.lotNo === lotNo)
  if (!lot) {
    currentLot.value = null; waferRows.value = []; mappingBasis.value = null; activeBasis.value = null; exportDetails.value = []
    error.value = 'DEV fixtures 找不到此 LotNo；請用 DEVLOT-CP-1001／1002／1003。'
    addActivity('查詢失敗', lotNo || '(空白 LotNo)')
    return
  }
  currentLot.value = lot
  waferRows.value = (mapFixtures[lot.lotNo] ?? []).map(row => ({ ...row, selected: false }))
  mappingBasis.value = resolveMapping(lot)
  if (mappingBasis.value) applyMode(mappingBasis.value.functionFlag)
  else { activeBasis.value = null; exportDetails.value = [] }
  const mode = activeBasis.value?.shipMapOption === 'LAST FULL TEST' ? 'LAST FULL TEST' : 'Ship MAP'
  notice.value = '已載入 ' + waferRows.value.length + ' 筆目前 MAP；CP 設定匹配：' + matchedConfigLabel.value + '。資料來源為固定 DEV fixtures。查詢模式：' + mode + '。'
  if (waferRows.value.length === 0) notice.value = 'Lot fixture 存在，但沒有 Wafer MAP rows。'
  if (!mappingBasis.value) error.value = '此 DEV Lot 沒有 Wafer Export Basis 設定，匯出不可用。'
  addActivity('查詢 ' + lot.lotNo, waferRows.value.length + ' 筆 Wafer MAP；設定 ' + matchedConfigLabel.value)
}
function toggleAll(checked: boolean): void { waferRows.value = waferRows.value.map(row => ({ ...row, selected: checked })) }
function setMode(flag: number): void {
  if (!currentLot.value) return
  error.value = ''
  applyMode(flag)
  if (!exportDetails.value.length) error.value = '此設定沒有對應的 Wafer Export Detail；無法匯出。'
  else notice.value = '已載入 ' + modeLabel.value + ' 匯出明細 ' + exportDetails.value.length + ' 筆。'
}
function openFolderEditor(detail: ExportDetail): void {
  folderEditId.value = detail.id
  folderEditValue.value = detail.targetFolder
  folderDialog.value = true
}
function saveFolder(): void {
  exportDetails.value = exportDetails.value.map(row => row.id === folderEditId.value ? { ...row, targetFolder: folderEditValue.value.trim() } : row)
  folderDialog.value = false
  addActivity('設定 DEV 目標資料夾', exportDetails.value.find(row => row.id === folderEditId.value)?.targetFolder ?? '')
}
function translatePattern(pattern: string, wafer: Wafer): string {
  const values: Record<string, string> = {
    LotNo: currentLot.value?.lotNo ?? '', FLOW: wafer.opNo, WF_ID: String(wafer.slotNo).padStart(2, '0'),
    WaferID: wafer.waferId, WaferCount: String(selectedCount.value), DATE: new Date().toISOString().slice(0, 10).replace(/-/g, ''),
  }
  return pattern.replace(/\[([^\]]+)\]/g, (whole, key: string) => values[key] ?? whole)
}
function outputTarget(detail: ExportDetail, wafer: Wafer): string {
  const base = activeBasis.value?.ftpFlag ? activeBasis.value.targetFolder : detail.targetFolder || activeBasis.value?.targetFolder || 'DEV://wafer-export'
  return [base.replace(/[\\/]+$/g, ''), wafer.opNo, detail.categoryName, translatePattern(detail.filePattern || activeBasis.value?.filePattern || '[LotNo]_[FLOW]_[WF_ID]', wafer)].join('/')
}
function exportPreview(): void {
  error.value = ''
  outputRows.value = []
  if (!currentLot.value || !waferRows.value.length) { error.value = '目前沒有可匯出的 Wafer MAP。'; return }
  const selected = waferRows.value.filter(row => row.selected)
  if (!selected.length) { error.value = '請至少勾選一筆 Wafer MAP。'; return }
  const seen = new Set<string>()
  for (const row of selected) {
    const key = row.slotNo + '|' + row.opNo
    if (seen.has(key)) { error.value = 'Wafer No 重複：SlotNo + OPNo 必須唯一。'; return }
    seen.add(key)
  }
  if (!activeBasis.value || !exportDetails.value.length) { error.value = '目前 FunctionFlag 沒有可用的匯出設定明細。'; return }
  const failed = selected.filter(row => !row.hasBinDefine)
  const built: OutputRow[] = []
  for (const row of selected.filter(item => item.hasBinDefine)) {
    for (const detail of exportDetails.value) {
      built.push({ id: 'OUT-' + nextId.value++, waferId: row.waferId, format: detail.exportFormat, fileName: translatePattern(detail.filePattern || activeBasis.value.filePattern, row), target: outputTarget(detail, row), result: '待產生' })
    }
  }
  if (activeBasis.value.compressAllFile && built.length) {
    const ops = [...new Set(selected.filter(row => row.hasBinDefine).map(row => row.opNo))]
    for (const op of ops) built.push({ id: 'ZIP-' + nextId.value++, waferId: 'OP ' + op, format: '壓縮檔預覽', fileName: translatePattern(activeBasis.value.filePattern, selected.find(row => row.opNo === op)!) + '.zip', target: activeBasis.value.targetFolder + '/' + op, result: '待產生' })
  }
  outputRows.value = built
  const failedText = failed.length ? '；' + failed.length + ' 筆缺 Bin Define 而略過' : ''
  notice.value = 'DEV manifest 已產生 ' + built.length + ' 個輸出項目' + failedText + '；未寫入資料夾、未連 FTP/File Service。'
  if (failed.length) error.value = failed.map(row => row.waferId + '：找不到 Bin Define').join('；')
  addActivity('預覽匯出', '選取 ' + selected.length + ' 片，預覽 ' + built.length + ' 個輸出項目' + failedText)
}
function resetFixtures(): void {
  lotInput.value = 'DEVLOT-CP-1001'
  activity.value = []
  outputRows.value = []
  queryLot()
  notice.value = 'DEV fixtures 與操作紀錄已還原。'
}

onMounted(() => {
  if (!dev.value) return
  try {
    const raw = localStorage.getItem(storageKey)
    if (raw) activity.value = JSON.parse(raw) as Activity[]
  } catch { notice.value = 'DEV 操作紀錄無法讀取，改用空白紀錄。' }
  queryLot()
})
</script>

<template>
  <section v-if="dev" class="wafer-export-dev" aria-label="晶圓針測資料匯出 DEV">
    <header class="page-head">
      <div><p class="eyebrow">WIP · CP WAFER MAP</p><h1>晶圓針測資料匯出</h1><p class="subtitle">P_WAFERMAP EXPORT · WPP02 · 僅 DEV 假資料</p></div>
      <div class="head-actions"><span class="dev-badge">DEV PREVIEW</span><button class="btn light" type="button" @click="resetFixtures">重置 DEV 資料</button><button class="btn dark" type="button" @click="emit('close')">Close (X)</button></div>
    </header>
    <div class="notice" role="status"><span class="dot"></span>{{ notice }}<span v-if="currentLot" class="time">{{ nowText() }}</span></div>

    <section class="panel query-panel">
      <div class="panel-title"><div><p class="eyebrow">QUERY CONDITION</p><h2>Lot 查詢</h2></div><span class="subtle">WinForms 只接受非空 LotNo</span></div>
      <form class="query-row" @submit.prevent="queryLot"><label class="field"><span>LotNo</span><input v-model="lotInput" autocomplete="off" placeholder="DEVLOT-CP-1001" aria-label="LotNo"></label><button class="btn primary" type="submit">Query</button><span class="fixture-hint">Fixtures：1001 可匯出／1002 無 MAP／1003 無 exact Config</span></form>
    </section>

    <section v-if="currentLot" class="lot-strip">
      <div><span>LotNo</span><strong>{{ currentLot.lotNo }}</strong></div><div><span>MO</span><strong>{{ currentLot.moNo }}</strong></div><div><span>Customer Lot</span><strong>{{ currentLot.customerLotNo }}</strong></div><div><span>Product / Device</span><strong>{{ currentLot.productNo }} / {{ currentLot.deviceNo }}</strong></div><div><span>Route Card</span><strong>{{ currentLot.routeCard }}</strong></div>
    </section>

    <section v-if="currentLot" class="panel config-panel">
      <div class="panel-title"><div><p class="eyebrow">EXPORT BASIS</p><h2>匯出設定</h2></div><span class="config-match">設定匹配 {{ matchedConfigLabel }}</span></div>
      <div class="config-grid">
        <div class="mode-select"><span class="label">匯出模式</span><label><input type="radio" name="mode" :checked="functionFlag === 1" :disabled="!mappingBasis" @change="setMode(1)"> Automatic <small>FunctionFlag 1</small></label><label><input type="radio" name="mode" :checked="functionFlag === 0" :disabled="!mappingBasis" @change="setMode(0)"> Manual <small>FunctionFlag 0</small></label></div>
        <div class="config-value"><span class="label">本機目標資料夾（fixture）</span><strong>{{ activeBasis?.targetFolder ?? '—' }}</strong></div>
        <div class="config-value"><span class="label">檔名樣式</span><strong class="code">{{ activeBasis?.filePattern ?? '—' }}</strong></div>
        <div class="config-flags"><span class="label">輸出策略</span><span class="flag" :class="activeBasis?.compressAllFile ? 'on' : ''">{{ activeBasis?.compressAllFile ? '整批壓縮' : '不壓縮' }}</span><span class="flag" :class="activeBasis?.ftpFlag ? 'on' : ''">{{ activeBasis?.ftpFlag ? 'FTP 設定啟用' : '本機設定' }}</span><span class="flag">{{ activeBasis?.shipMapOption ?? 'No config' }}</span></div>
      </div>
      <p class="hint">DEV 會套用 Lot／Product 設定匹配、FunctionFlag 與固定檔名 token 子集；本機路徑與 FTP 狀態只做顯示，不會碰真實儲存位置。</p>
    </section>

    <section class="panel map-panel">
      <div class="panel-title"><div><p class="eyebrow">WAFER MAP</p><h2>針測 Wafer 清單</h2></div><div class="selection-tools"><span>已選 {{ selectedCount }} / {{ waferRows.length }}</span><button class="btn light" type="button" :disabled="!waferRows.length" @click="toggleAll(true)">Select All</button><button class="btn light" type="button" :disabled="!waferRows.length" @click="toggleAll(false)">Unselect All</button></div></div>
      <div class="table-wrap"><table><thead><tr><th><input type="checkbox" aria-label="全選 Wafer" :checked="allSelected" :disabled="!waferRows.length" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th><th>MO No</th><th>Customer LOT</th><th>Wafer Slot No</th><th>OP No</th><th>Flat</th><th>Test Qty</th><th>Pass Qty</th><th>Upload Time</th></tr></thead><tbody><tr v-for="row in waferRows" :key="row.id"><td><input v-model="row.selected" type="checkbox" :aria-label="'選取 ' + row.waferId"></td><td>{{ currentLot?.moNo }}</td><td>{{ currentLot?.customerLotNo }}</td><td class="numeric">{{ String(row.slotNo).padStart(2, '0') }} <small>{{ row.waferId }}</small></td><td>{{ row.opNo }}</td><td class="numeric">{{ row.waferFlat }}</td><td class="numeric">{{ row.totalTestQty.toLocaleString() }}</td><td class="numeric">{{ row.totalPassQty.toLocaleString() }}</td><td>{{ row.uploadTime }}</td></tr><tr v-if="!waferRows.length"><td colspan="9" class="empty">此 Lot fixture 沒有可匯出的 Wafer MAP。</td></tr></tbody></table></div>
    </section>

    <section class="panel detail-panel">
      <div class="panel-title"><div><p class="eyebrow">EXPORT DETAIL</p><h2>輸出格式明細</h2></div><span class="subtle">依 Product／Customer Lot／FunctionFlag 載入</span></div>
      <div class="table-wrap"><table><thead><tr><th>ItemNo</th><th>ProductNo</th><th>ExportFormat</th><th>TargetFolder</th><th>FilePatten</th><th></th></tr></thead><tbody><tr v-for="row in exportDetails" :key="row.id"><td class="numeric">{{ row.itemNo }}</td><td>{{ currentLot?.productNo }}</td><td>{{ row.exportFormat }}</td><td class="code">{{ row.targetFolder }}</td><td class="code">{{ row.filePattern }}</td><td><button class="btn light compact" type="button" @click="openFolderEditor(row)">Set Folder</button></td></tr><tr v-if="!exportDetails.length"><td colspan="6" class="empty">目前沒有符合設定的匯出格式。</td></tr></tbody></table></div>
      <div class="detail-footer"><span>Basis：{{ activeBasis?.customerLotNo ?? '—' }} / {{ activeBasis?.productNo ?? '—' }} / {{ modeLabel }}</span><button class="btn primary" type="button" :disabled="!currentLot || !exportDetails.length" @click="exportPreview">Export DEV Preview</button></div>
    </section>

    <section class="panel output-panel">
      <div class="panel-title"><div><p class="eyebrow">SIMULATED MANIFEST</p><h2>輸出預覽</h2></div><span class="subtle">{{ outputRows.length }} 項目 {{ outputCount ? '· ' + outputCount + ' 待產生' : '' }}</span></div>
      <div v-if="outputRows.length" class="output-list"><article v-for="row in outputRows" :key="row.id" class="output-row"><span class="result-dot"></span><strong>{{ row.waferId }}</strong><span>{{ row.format }}</span><code>{{ row.fileName }}</code><small>{{ row.target }}</small><em>{{ row.result }}</em></article></div><p v-else class="empty">選取 Wafer 後執行 Export DEV Preview，這裡會顯示虛構輸出 manifest。</p>
    </section>

    <section class="panel activity-panel"><div class="panel-title"><div><p class="eyebrow">DEV ACTIVITY</p><h2>操作紀錄</h2></div><span class="dev-badge">本機假資料</span></div><div v-if="activity.length" class="activity-list"><article v-for="entry in activity" :key="entry.id" class="activity-row"><time>{{ entry.time }}</time><strong>{{ entry.action }}</strong><span>{{ entry.detail }}</span></article></div><p v-else class="empty">尚無操作紀錄</p></section>

    <p v-if="error" class="error" role="alert">{{ error }}</p>
    <div v-if="folderDialog" class="backdrop" role="presentation" @click.self="folderDialog = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="設定 DEV 匯出資料夾"><header><div><p class="eyebrow">DEV FOLDER OVERRIDE</p><h2>設定明細目標資料夾</h2></div><button class="icon" type="button" aria-label="關閉" @click="folderDialog = false">×</button></header><p class="dialog-note">這只修改目前頁面的文字 fixture，不會建立目錄或存取檔案系統。</p><label class="field"><span>DEV 目標資料夾</span><input v-model="folderEditValue" type="text" autocomplete="off"></label><div class="dialog-actions"><button class="btn light" type="button" @click="folderDialog = false">取消</button><button class="btn primary" type="button" @click="saveFolder">套用</button></div></section></div>
  </section>
  <section v-else class="wafer-export-pending"><p>晶圓針測資料匯出正式服務尚未接入。</p><button type="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.wafer-export-dev,.wafer-export-pending{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:18px 22px;background:#f3f2ed;color:#292922;font:inherit}.page-head,.panel-title,.head-actions,.query-row,.selection-tools,.detail-footer{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head{margin-bottom:13px}.page-head h1{margin:2px 0 3px;font-size:23px;font-weight:650}.subtitle{margin:0;color:#716f66;font-size:12px}.eyebrow{margin:0 0 4px;color:#7d7869;font-size:10px;font-weight:700;letter-spacing:.12em}.dev-badge,.config-match{display:inline-flex;align-items:center;border:1px solid #d6d0bf;border-radius:12px;padding:4px 9px;background:#f7f4e9;color:#70694f;font-size:10px;white-space:nowrap}.btn{min-height:29px;border:1px solid #bdb9ad;border-radius:4px;padding:4px 10px;background:#fff;color:#343229;font:inherit;font-size:12px;cursor:pointer;white-space:nowrap}.btn:hover:not(:disabled){filter:brightness(.97)}.btn:disabled{opacity:.5;cursor:not-allowed}.btn.primary{border-color:#465947;background:#465947;color:#fff}.btn.dark{border-color:#3b3930;background:#3b3930;color:#fff}.btn.light{background:#f8f7f2}.btn.compact{min-height:25px;padding:2px 8px;font-size:11px}.notice{display:flex;align-items:center;gap:8px;min-height:34px;margin-bottom:12px;padding:6px 11px;border:1px solid #d7d2c3;border-radius:5px;background:#fbfaf6;color:#49473e;font-size:11px}.dot,.result-dot{width:8px;height:8px;flex:none;border-radius:50%;background:#6a8c58}.time{margin-left:auto;color:#858172;font-size:10px}.panel{margin:0 0 12px;padding:13px 15px;border:1px solid #d9d6cb;border-radius:6px;background:#fffefa;box-shadow:0 1px 2px #312f2610}.panel-title{margin-bottom:10px}.panel-title h2{margin:0;font-size:15px;font-weight:650}.subtle,.fixture-hint{color:#807c70;font-size:10px}.query-row{justify-content:flex-start}.field{display:grid;gap:4px;color:#5e5a50;font-size:11px}.field>span,.label{font-weight:600}.field input{height:30px;min-width:225px;box-sizing:border-box;border:1px solid #c9c5b9;border-radius:4px;background:white;padding:4px 8px;font:inherit}.fixture-hint{margin-left:4px}.lot-strip{display:grid;grid-template-columns:1fr 1fr 1.3fr 1.6fr 1fr;gap:1px;margin-bottom:12px;border:1px solid #d9d6cb;border-radius:6px;overflow:hidden;background:#d9d6cb}.lot-strip>div{display:grid;gap:4px;padding:10px 12px;background:#fbfaf6}.lot-strip span{color:#817c70;font-size:9px;text-transform:uppercase;letter-spacing:.06em}.lot-strip strong{font-size:11px;font-weight:650}.config-grid{display:grid;grid-template-columns:1.5fr 1.2fr 1.6fr 1.4fr;gap:12px}.mode-select,.config-value,.config-flags{display:flex;flex-direction:column;align-items:flex-start;gap:6px;min-width:0;padding:8px;border:1px solid #ece9e1;border-radius:4px;background:#fcfbf7}.label{color:#777266;font-size:9px;text-transform:uppercase;letter-spacing:.04em}.mode-select label{display:flex;align-items:center;gap:5px;font-size:11px}.mode-select small{color:#8a8578;font-size:9px}.config-value strong{max-width:100%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:11px}.code{font-family:Consolas,monospace;font-size:10px!important}.config-flags{flex-direction:row;align-items:center;flex-wrap:wrap}.config-flags .label{width:100%}.flag{border:1px solid #d7d2c3;border-radius:10px;padding:3px 7px;color:#787365;font-size:9px}.flag.on{border-color:#b8ccb5;background:#f1f7ef;color:#4f6d49}.hint{margin:8px 0 0;color:#807c70;font-size:10px}.selection-tools{justify-content:flex-end;color:#777366;font-size:10px}.table-wrap{overflow:auto;border:1px solid #e3e0d7;border-radius:4px}table{width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th,td{padding:6px 8px;border-bottom:1px solid #ece9e1;text-align:left}th{background:#f4f2eb;color:#5f5b50;font-size:9px;font-weight:700;letter-spacing:.04em}tbody tr:hover{background:#faf9f4}tbody tr:last-child td{border-bottom:0}td small{display:block;color:#837e71;font-size:9px}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty{padding:18px;text-align:center;color:#908b7d;font-size:11px}.detail-footer{margin-top:11px;color:#777366;font-size:10px}.output-list,.activity-list{display:grid;gap:0}.output-row{display:grid;grid-template-columns:10px 115px 150px minmax(170px,1fr) minmax(170px,1.4fr) 65px;align-items:center;gap:9px;padding:7px 2px;border-bottom:1px solid #eeeae2;font-size:10px}.output-row code{overflow:hidden;text-overflow:ellipsis;color:#514e44;font-family:Consolas,monospace}.output-row small{overflow:hidden;text-overflow:ellipsis;color:#817c70;white-space:nowrap}.output-row em{color:#55714e;font-size:9px;font-style:normal}.result-dot{width:6px;height:6px}.activity-row{display:grid;grid-template-columns:140px 140px minmax(0,1fr);gap:10px;padding:7px 2px;border-bottom:1px solid #eeeae2;font-size:10px}.activity-row time,.activity-row span{color:#817c70}.error{margin:0 0 12px;padding:8px 10px;border:1px solid #ddb0a9;border-radius:4px;background:#fff5f3;color:#963f33;font-size:11px}.backdrop{position:fixed;z-index:1200;inset:0;display:flex;align-items:center;justify-content:center;padding:20px;background:#211f1c75}.dialog{width:min(520px,100%);box-sizing:border-box;border:1px solid #d6d2c6;border-radius:7px;background:#fffefa;padding:17px;box-shadow:0 16px 48px #1115}.dialog>header{display:flex;align-items:flex-start;justify-content:space-between;gap:12px}.dialog h2{margin:0;font-size:16px}.icon{width:30px;height:30px;border:1px solid #d1cdc1;border-radius:4px;background:white;color:#524f44;font-size:20px;cursor:pointer}.dialog-note{margin:10px 0;color:#807c70;font-size:10px}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:14px;padding-top:12px;border-top:1px solid #ece9e1}.wafer-export-pending{height:auto}.wafer-export-pending p{margin:0 0 10px;color:#777366}.wafer-export-pending button{min-height:28px;padding:4px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:#fff;font:inherit;cursor:pointer}
@media(max-width:1100px){.lot-strip{grid-template-columns:repeat(3,1fr)}.config-grid{grid-template-columns:1fr 1fr}.output-row{grid-template-columns:10px 100px minmax(100px,1fr) minmax(150px,1.5fr) 60px}.output-row small{grid-column:3/-1}.fixture-hint{display:none}}
@media(max-width:700px){.wafer-export-dev{padding:12px}.page-head,.panel-title{align-items:flex-start;flex-direction:column}.head-actions,.selection-tools{width:100%;justify-content:flex-start;flex-wrap:wrap}.query-row{align-items:stretch;flex-wrap:wrap}.field{flex:1}.field input{min-width:0;width:100%}.lot-strip{grid-template-columns:1fr 1fr}.config-grid{grid-template-columns:1fr}.selection-tools{font-size:9px}.output-row{grid-template-columns:10px minmax(70px,1fr) minmax(100px,1.2fr) 60px}.output-row code{grid-column:2/4}.output-row small{grid-column:2/5}.activity-row{grid-template-columns:1fr 1fr}.activity-row span{grid-column:1/-1}.detail-footer{align-items:flex-start;flex-direction:column}}
</style>
