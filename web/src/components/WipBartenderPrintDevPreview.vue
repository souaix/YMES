<script setup lang="ts">
import { computed, ref } from 'vue'

type Tab = 'preview' | 'settings' | 'tools'
type LabelOption = { tagNo: string; labelName: string; tagName: string; tagAttribute: string; size: string; dpi: string; defaultCopy: number; isDefault: boolean }
type LabelMap = { tagNo: string; labelName: string; tagAttribute: string }
type PrintRow = { checked: boolean; serialNo: string; lotNo: string; productNo: string; customerNo: string; mfgDate: string; quantity: number; attribute: string }
type LotInfo = { lotNo: string; customerNo: string; customerParentNo: string; productNo: string; mfgBlock: string; quantity: number; createDate: string }

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const lotFixtures: Record<string, LotInfo> = {
  'DEVLOT001': { lotNo: 'DEVLOT001', customerNo: 'CUST-DEMO', customerParentNo: 'CUST-GROUP', productNo: 'PRD-DEMO-01', mfgBlock: 'MB-26A', quantity: 240, createDate: '2026-11-22 08:20:00' },
  'DEVLOT002': { lotNo: 'DEVLOT002', customerNo: 'CUST-DEMO', customerParentNo: 'CUST-GROUP', productNo: 'PRD-DEMO-02', mfgBlock: 'MB-26B', quantity: 120, createDate: '2026-11-22 09:15:00' },
}
const labelFixtures: LabelOption[] = [
  { tagNo: 'TAG-DEV-001', labelName: 'DEV_INNER_4X6.btw', tagName: '內標籤 · 4 × 6', tagAttribute: 'LOT + PRODUCT + DATE', size: '4 × 6 in', dpi: '300', defaultCopy: 1, isDefault: true },
  { tagNo: 'TAG-DEV-002', labelName: 'DEV_INNER_2X1.btw', tagName: '內標籤 · 2 × 1', tagAttribute: 'LOT + SERIAL', size: '2 × 1 in', dpi: '203', defaultCopy: 2, isDefault: false },
  { tagNo: 'TAG-DEV-003', labelName: 'DEV_CARTON_4X3.btw', tagName: '箱標 · 4 × 3', tagAttribute: 'CUSTOMER + QTY', size: '4 × 3 in', dpi: '300', defaultCopy: 1, isDefault: false },
]
const initialMappings: LabelMap[] = [
  { tagNo: 'TAG-DEV-001', labelName: 'DEV_INNER_4X6.btw', tagAttribute: 'LOT + PRODUCT + DATE' },
  { tagNo: 'TAG-DEV-002', labelName: 'DEV_INNER_2X1.btw', tagAttribute: 'LOT + SERIAL' },
  { tagNo: 'TAG-DEV-003', labelName: 'DEV_CARTON_4X3.btw', tagAttribute: 'CUSTOMER + QTY' },
]

const activeTab = ref<Tab>('preview')
const lotNo = ref('DEVLOT001')
const printerName = ref('DEV Zebra 300 dpi')
const availablePrinters = ['DEV Zebra 300 dpi', 'DEV Thermal 203 dpi', 'DEV PDF Preview']
const lot = ref<LotInfo | null>(null)
const tags = ref<LabelOption[]>([])
const selectedTagNo = ref('')
const generatedRows = ref<PrintRow[]>([])
const mappings = ref<LabelMap[]>(initialMappings.map(item => ({ ...item })))
const savedMappings = ref<LabelMap[]>(initialMappings.map(item => ({ ...item })))
const selectedPreviewRows = ref<PrintRow[]>([])
const previewOpen = ref(false)
const printConfirmOpen = ref(false)
const saveConfirmOpen = ref(false)
const previewPage = ref(0)
const resultMessage = ref('')
const errorMessage = ref('')
const printStatus = ref('尚未送出列印工作')
const printLog = ref<{ time: string; label: string; rows: number; printer: string; state: string }[]>([])

const selectedTag = computed(() => tags.value.find(item => item.tagNo === selectedTagNo.value) ?? null)
const selectedRows = computed(() => generatedRows.value.filter(row => row.checked))
const allRowsSelected = computed(() => generatedRows.value.length > 0 && generatedRows.value.every(row => row.checked))
const someRowsSelected = computed(() => selectedRows.value.length > 0 && !allRowsSelected.value)
const currentPreviewRow = computed(() => selectedPreviewRows.value[previewPage.value] ?? null)
const mappingDirty = computed(() => JSON.stringify(mappings.value) !== JSON.stringify(savedMappings.value))
const settingsSummary = computed(() => `${mappings.value.length} 筆標籤檔案對照`)

function setTab(tab: Tab): void {
  activeTab.value = tab
  resultMessage.value = ''
  errorMessage.value = ''
}

function lookupLot(): void {
  errorMessage.value = ''
  resultMessage.value = ''
  const normalized = lotNo.value.trim().toUpperCase()
  const matched = lotFixtures[normalized]
  if (!normalized) {
    lot.value = null
    tags.value = []
    generatedRows.value = []
    errorMessage.value = '請輸入生產批號。'
    return
  }
  if (!matched) {
    lot.value = null
    tags.value = []
    generatedRows.value = []
    errorMessage.value = normalized.startsWith('MISS') ? '查無 LOT 資料。' : 'DEV 範例只包含 DEVLOT001、DEVLOT002。'
    return
  }
  lotNo.value = normalized
  lot.value = { ...matched }
  tags.value = labelFixtures.filter(item => normalized === 'DEVLOT001' || item.tagNo !== 'TAG-DEV-003').map(item => ({ ...item }))
  selectedTagNo.value = tags.value.find(item => item.isDefault)?.tagNo ?? tags.value[0]?.tagNo ?? ''
  generatedRows.value = []
  resultMessage.value = `已取得 ${matched.lotNo} 的內標籤選項；${tags.value.length} 種可選標籤。`
}

function generateData(): void {
  errorMessage.value = ''
  resultMessage.value = ''
  if (!lot.value || lot.value.lotNo !== lotNo.value.trim().toUpperCase()) {
    errorMessage.value = '請先產生標籤選項並確認批號。'
    return
  }
  if (!selectedTag.value) {
    errorMessage.value = '請選擇一種標籤。'
    return
  }
  const chosenTag = selectedTag.value
  const sourceLot = lot.value
  const base = chosenTag.tagNo === 'TAG-DEV-002' ? 3 : 2
  generatedRows.value = Array.from({ length: base }, (_, index) => ({
    checked: false,
    serialNo: `SN-${sourceLot.lotNo.slice(-3)}-${String(index + 1).padStart(3, '0')}`,
    lotNo: sourceLot.lotNo,
    productNo: sourceLot.productNo,
    customerNo: sourceLot.customerNo,
    mfgDate: `2026-11-${String(22 - Math.min(index, 3)).padStart(2, '0')}`,
    quantity: Math.min(120, Math.ceil(sourceLot.quantity / base)),
    attribute: chosenTag.tagAttribute,
  }))
  printStatus.value = '已產生列印資料；尚未預覽或送印。'
  resultMessage.value = `依 ${chosenTag.labelName} 取得 ${generatedRows.value.length} 筆 DEV 標籤資料。`
}

function toggleAllRows(): void {
  const next = !allRowsSelected.value
  generatedRows.value = generatedRows.value.map(row => ({ ...row, checked: next }))
}

function openPreview(): void {
  errorMessage.value = ''
  resultMessage.value = ''
  if (!selectedRows.value.length) {
    errorMessage.value = '請至少勾選一筆標籤資料。'
    return
  }
  selectedPreviewRows.value = selectedRows.value.map(row => ({ ...row }))
  previewPage.value = 0
  previewOpen.value = true
  printStatus.value = `預覽中：${selectedPreviewRows.value.length} 筆 · ${selectedTag.value?.labelName ?? ''}`
}

function movePreview(direction: -1 | 1): void {
  previewPage.value = Math.max(0, Math.min(selectedPreviewRows.value.length - 1, previewPage.value + direction))
}

function askPrint(): void {
  if (!selectedPreviewRows.value.length) return
  printConfirmOpen.value = true
}

function confirmPrint(): void {
  printConfirmOpen.value = false
  const timestamp = new Date().toLocaleString('zh-TW', { hour12: false })
  const rowCount = selectedPreviewRows.value.length
  const labelName = selectedTag.value?.labelName ?? '未知標籤'
  printStatus.value = `DEV 模擬完成：${rowCount} 筆未送至印表機。`
  printLog.value.unshift({ time: timestamp, label: labelName, rows: rowCount, printer: printerName.value, state: 'DEV 模擬完成' })
  previewOpen.value = false
  resultMessage.value = `已模擬 ${rowCount} 筆列印要求；沒有啟動 BarTender 或送到印表機。`
}

function addMapping(): void {
  mappings.value.push({ tagNo: '', labelName: '', tagAttribute: '' })
}

function removeMapping(index: number): void {
  mappings.value.splice(index, 1)
}

function requestSaveMappings(): void {
  errorMessage.value = ''
  resultMessage.value = ''
  const incomplete = mappings.value.findIndex(item => !item.tagNo.trim() || !item.labelName.trim() || !item.tagAttribute.trim())
  if (incomplete >= 0) {
    errorMessage.value = `第 ${incomplete + 1} 列 TAG_NO、LABEL_NAME、TAG_ATTRIBUTE 不可空白。`
    return
  }
  if (!mappings.value.length) {
    errorMessage.value = '沒有資料需要儲存。'
    return
  }
  saveConfirmOpen.value = true
}

function confirmSaveMappings(): void {
  saveConfirmOpen.value = false
  const originalByTag = new Map(savedMappings.value.map(item => [item.tagNo, item]))
  let insertCount = 0
  let updateCount = 0
  for (const item of mappings.value) {
    const old = originalByTag.get(item.tagNo.trim())
    if (!old) insertCount += 1
    else if (old.labelName !== item.labelName.trim() || old.tagAttribute !== item.tagAttribute.trim()) updateCount += 1
  }
  savedMappings.value = mappings.value.map(item => ({ tagNo: item.tagNo.trim(), labelName: item.labelName.trim(), tagAttribute: item.tagAttribute.trim() }))
  mappings.value = savedMappings.value.map(item => ({ ...item }))
  resultMessage.value = `DEV 儲存模擬完成：新增 ${insertCount}、更新 ${updateCount}、略過 0 筆。設定只留在此畫面。`
}

function clearNotices(): void {
  errorMessage.value = ''
  resultMessage.value = ''
}

function pageClass(): string { return dev.value ? 'bartender-page' : 'bartender-page unavailable' }
</script>

<template>
  <section :class="pageClass()">
    <header class="page-header">
      <div><p class="eyebrow">WIP · PACKAGING &amp; LABEL · DEV</p><h1>Bartender print</h1><p class="subtitle">查詢 LOT、選擇標籤檔、產生資料並預覽列印要求</p></div>
      <div class="header-actions"><span class="dev-pill">DEV 假資料</span><button type="button" class="quiet" @click="emit('close')">關閉</button></div>
    </header>
    <div v-if="!dev" class="unavailable-note" role="status">正式標籤資料與列印服務尚未接入；此頁不會讀取或改寫資料。</div>
    <template v-else>
      <div class="mode-strip"><span><b>來源</b> THEIL Bartender Print</span><span><b>DEV 範例 LOT</b> DEVLOT001 · DEVLOT002</span><span><b>列印狀態</b> {{ printStatus }}</span></div>
      <nav class="tabs" aria-label="Bartender print 工作區"><button type="button" :class="{ active: activeTab === 'preview' }" @click="setTab('preview')">PREVIEW</button><button type="button" :class="{ active: activeTab === 'settings' }" @click="setTab('settings')">應用設定 <small>{{ settingsSummary }}</small></button><button type="button" :class="{ active: activeTab === 'tools' }" @click="setTab('tools')">工具</button></nav>
      <div v-if="errorMessage || resultMessage" class="notice" :class="{ error: !!errorMessage }" role="status">{{ errorMessage || resultMessage }}<button type="button" aria-label="清除訊息" @click="clearNotices">×</button></div>

      <section v-if="activeTab === 'preview'" class="workspace">
        <section class="panel query-panel">
          <div class="panel-title"><div><h2>標籤資料查詢</h2><p>先以 LOT 取得客戶／產品，再載入內標籤選項。</p></div><span class="step">01</span></div>
          <div class="query-controls"><label>LOT NO<input v-model="lotNo" autocomplete="off" placeholder="輸入 DEVLOT001 或 DEVLOT002" @keydown.enter="lookupLot"></label><button type="button" class="primary" @click="lookupLot">產生標籤選項</button></div>
          <div v-if="lot" class="lot-summary"><span><small>LOT</small><b>{{ lot.lotNo }}</b></span><span><small>客戶</small><b>{{ lot.customerNo }}</b></span><span><small>產品</small><b>{{ lot.productNo }}</b></span><span><small>工單區塊</small><b>{{ lot.mfgBlock }}</b></span><span><small>建立時間</small><b>{{ lot.createDate }}</b></span></div>
        </section>

        <section class="panel tags-panel">
          <div class="panel-title"><div><h2>可用標籤</h2><p>依 LOT／客戶／產品回傳；預設標籤已先選取。</p></div><span class="count">{{ tags.length }} 個選項</span></div>
          <div class="table-scroll"><table><thead><tr><th>選擇</th><th>TAG NO</th><th>LABEL NAME</th><th>標籤名稱</th><th>屬性</th><th>尺寸</th><th>DPI</th><th>份數</th></tr></thead><tbody><tr v-for="tag in tags" :key="tag.tagNo" :class="{ selected: selectedTagNo === tag.tagNo }" @click="selectedTagNo = tag.tagNo"><td><input v-model="selectedTagNo" type="radio" name="bartender-tag" :value="tag.tagNo" :aria-label="`選擇 ${tag.labelName}`"></td><td>{{ tag.tagNo }}</td><td class="mono">{{ tag.labelName }}</td><td>{{ tag.tagName }} <span v-if="tag.isDefault" class="default-tag">預設</span></td><td>{{ tag.tagAttribute }}</td><td>{{ tag.size }}</td><td>{{ tag.dpi }}</td><td>{{ tag.defaultCopy }}</td></tr><tr v-if="!tags.length"><td colspan="8" class="empty">先輸入 DEV LOT 並產生標籤選項</td></tr></tbody></table></div>
          <div class="section-actions"><label>標籤機<select v-model="printerName"><option v-for="printer in availablePrinters" :key="printer" :value="printer">{{ printer }}</option></select></label><button type="button" class="primary" :disabled="!selectedTag" @click="generateData">產生資料</button></div>
        </section>

        <section class="panel data-panel">
          <div class="panel-title"><div><h2>主檔資料</h2><p>GETXXQCLABEL 假資料結果。</p></div><span class="count">{{ lot ? 1 : 0 }} 筆</span></div>
          <div class="table-scroll"><table><thead><tr><th>LOT NO</th><th>CUSTOMER NO</th><th>PRODUCT NO</th><th>MFG BLOCK</th><th>LOT QTY</th></tr></thead><tbody><tr v-if="lot"><td class="mono">{{ lot.lotNo }}</td><td>{{ lot.customerNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.mfgBlock }}</td><td>{{ lot.quantity.toLocaleString('zh-TW') }}</td></tr><tr v-else><td colspan="5" class="empty">尚未查詢 LOT</td></tr></tbody></table></div>
        </section>

        <section class="panel generated-panel">
          <div class="panel-title"><div><h2>標籤列印資料</h2><p>欄位由所選標籤規則決定；此處以動態欄位假資料呈現。</p></div><span class="count">{{ selectedRows.length }} / {{ generatedRows.length }} 已選</span></div>
          <div class="table-scroll"><table><thead><tr><th><button type="button" class="select-all" :aria-pressed="allRowsSelected" @click="toggleAllRows">{{ allRowsSelected ? '☑' : someRowsSelected ? '◩' : '□' }} 全選</button></th><th>SERIAL NO</th><th>LOT NO</th><th>PRODUCT NO</th><th>CUSTOMER NO</th><th>MFG DATE</th><th>QTY</th><th>TAG ATTRIBUTE</th></tr></thead><tbody><tr v-for="(row, index) in generatedRows" :key="row.serialNo"><td><input v-model="row.checked" type="checkbox" :aria-label="`選擇第 ${index + 1} 筆標籤`"></td><td class="mono">{{ row.serialNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.productNo }}</td><td>{{ row.customerNo }}</td><td>{{ row.mfgDate }}</td><td>{{ row.quantity }}</td><td>{{ row.attribute }}</td></tr><tr v-if="!generatedRows.length"><td colspan="8" class="empty">選擇標籤後按「產生資料」</td></tr></tbody></table></div>
          <div class="section-actions"><span class="printer-target">目的印表機：<b>{{ printerName || '未選擇' }}</b></span><button type="button" class="primary" :disabled="!selectedRows.length" @click="openPreview">預覽</button></div>
        </section>

        <section class="panel history-panel"><div class="panel-title"><div><h2>DEV 列印要求</h2><p>只記錄本次頁面內的模擬結果。</p></div></div><div class="table-scroll"><table><thead><tr><th>時間</th><th>標籤</th><th>筆數</th><th>標籤機</th><th>結果</th></tr></thead><tbody><tr v-for="(item, index) in printLog" :key="item.time + index"><td>{{ item.time }}</td><td class="mono">{{ item.label }}</td><td>{{ item.rows }}</td><td>{{ item.printer }}</td><td><span class="status">{{ item.state }}</span></td></tr><tr v-if="!printLog.length"><td colspan="5" class="empty">尚無模擬列印要求</td></tr></tbody></table></div></section>
      </section>

      <section v-else-if="activeTab === 'settings'" class="settings-workspace">
        <section class="panel settings-panel"><div class="panel-title"><div><h2>標籤檔案對照設定</h2><p>對應 WinForms `TBLINLABEL_TAG_FILE_MAP` Grid；欄位完整後才可模擬 UPDATE。</p></div><span class="count">{{ mappings.length }} 筆</span></div>
          <div class="table-scroll"><table><thead><tr><th>#</th><th>TAG_NO</th><th>LABEL_NAME</th><th>TAG_ATTRIBUTE</th><th>操作</th></tr></thead><tbody><tr v-for="(item, index) in mappings" :key="`${index}-${item.tagNo}`"><td>{{ String(index + 1).padStart(2, '0') }}</td><td><input v-model="item.tagNo" aria-label="TAG_NO"></td><td><input v-model="item.labelName" aria-label="LABEL_NAME"></td><td><input v-model="item.tagAttribute" aria-label="TAG_ATTRIBUTE"></td><td><button type="button" class="link-button danger-link" @click="removeMapping(index)">移除</button></td></tr><tr v-if="!mappings.length"><td colspan="5" class="empty">沒有對照資料</td></tr></tbody></table></div>
          <div class="section-actions"><button type="button" class="quiet" @click="addMapping">新增對照</button><span class="dirty-state" :class="{ dirty: mappingDirty }">{{ mappingDirty ? '有未儲存變更' : '目前資料與載入值一致' }}</span><button type="button" class="primary" :disabled="!mappingDirty" @click="requestSaveMappings">UPDATE</button></div>
        </section>
        <aside class="panel handoff-note"><span class="note-icon">i</span><div><h2>DEV 儲存範圍</h2><p>原程式將完整 TAG_NO／LABEL_NAME／TAG_ATTRIBUTE 清單送到 InLabel.SaveTagFileMap。本頁只在瀏覽器狀態模擬新增／更新數量，不呼叫 API、資料庫或正式設定。</p></div></aside>
      </section>

      <section v-else class="tools-workspace">
        <section class="panel tool-card"><div class="tool-icon">BT</div><div class="tool-copy"><p class="eyebrow">本機列印執行環境</p><h2>BarTenderExe</h2><p>原 WinForms 工具頁可從 SFTP 下載並解壓外部執行檔；DEV 只展示來源依賴，不下載、解壓或執行二進位檔。</p></div><span class="tool-state">未連線</span><button type="button" class="quiet" disabled>下載並解壓 BarTenderExe</button></section>
        <section class="panel tool-card"><div class="tool-icon printer-icon">▤</div><div class="tool-copy"><p class="eyebrow">本機裝置</p><h2>標籤機</h2><p>舊 Form 從 Windows 已安裝印表機清單載入預設裝置。DEV 使用固定的模擬標籤機選項，不讀取本機印表機。</p></div><span class="tool-state ready">模擬中</span></section>
        <p class="tools-footnote">這個頁面不呼叫外部列印程式、SFTP、印表機或標籤檔案渲染服務。</p>
      </section>

      <footer>DEV 僅模擬 LOT／標籤查詢、動態列印資料、設定 UPDATE、預覽及送印確認；不呼叫 MES／InLabel API、資料庫、SFTP、BarTender 或印表機。</footer>

      <section v-if="previewOpen" class="backdrop" role="presentation"><div class="preview-dialog" role="dialog" aria-modal="true" aria-label="標籤預覽">
        <header class="dialog-header"><div><p class="eyebrow">LABEL PREVIEW · {{ selectedTag?.tagNo }}</p><h2>{{ selectedTag?.labelName }}</h2></div><button type="button" class="quiet" @click="previewOpen = false">關閉</button></header>
        <div class="preview-layout"><aside class="preview-source"><h3>標籤資料</h3><dl v-if="currentPreviewRow"><template v-for="(value, key) in currentPreviewRow" :key="key"><template v-if="key !== 'checked'"><dt>{{ key }}</dt><dd>{{ value }}</dd></template></template></dl><p v-else class="empty">無標籤資料</p></aside><div class="label-stage"><div class="label-sheet"><div class="label-brand">YMES · DEV LABEL</div><div class="label-code">{{ currentPreviewRow?.lotNo ?? 'LOT' }}</div><div class="label-line"><span>PRODUCT</span><b>{{ currentPreviewRow?.productNo ?? '—' }}</b></div><div class="label-line"><span>CUSTOMER</span><b>{{ currentPreviewRow?.customerNo ?? '—' }}</b></div><div class="label-line"><span>SERIAL</span><b>{{ currentPreviewRow?.serialNo ?? '—' }}</b></div><div class="label-barcode" aria-label="條碼樣式示意"><i v-for="index in 34" :key="index" :style="{ width: `${index % 5 === 0 ? 3 : index % 3 === 0 ? 1 : 2}px` }"></i></div><small>條碼外觀示意 · 不含正式編碼</small></div></div></div>
        <footer class="preview-footer"><button type="button" class="quiet" :disabled="previewPage <= 0" @click="movePreview(-1)">Previous</button><span>{{ previewPage + 1 }} / {{ selectedPreviewRows.length }}</span><button type="button" class="quiet" :disabled="previewPage >= selectedPreviewRows.length - 1" @click="movePreview(1)">Next</button><span class="spacer"></span><button type="button" class="primary" @click="askPrint">Print</button></footer>
      </div></section>

      <section v-if="printConfirmOpen" class="backdrop confirm-layer" role="presentation"><div class="confirm-dialog" role="dialog" aria-modal="true" aria-label="列印確認"><div class="confirm-mark">!</div><h2>確認模擬列印？</h2><p>將模擬送出 {{ selectedPreviewRows.length }} 筆至「{{ printerName }}」。此操作不會啟動印表機或 BarTender。</p><div class="dialog-actions"><button type="button" class="quiet" @click="printConfirmOpen = false">取消</button><button type="button" class="primary" @click="confirmPrint">確認模擬</button></div></div></section>
      <section v-if="saveConfirmOpen" class="backdrop confirm-layer" role="presentation"><div class="confirm-dialog" role="dialog" aria-modal="true" aria-label="設定儲存確認"><div class="confirm-mark">↻</div><h2>更新標籤對照？</h2><p>共送出 {{ mappings.length }} 筆 TAG_NO／LABEL_NAME／TAG_ATTRIBUTE 對照。DEV 只更新頁面內的臨時資料。</p><div class="dialog-actions"><button type="button" class="quiet" @click="saveConfirmOpen = false">取消</button><button type="button" class="primary" @click="confirmSaveMappings">確認 UPDATE</button></div></div></section>
    </template>
  </section>
</template>

<style scoped>
.bartender-page{--ink:#1b2b3c;--muted:#708295;--line:#dbe3ea;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:20px 24px 28px;background:#f6f8fa;font:12px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.page-header{display:flex;align-items:center;justify-content:space-between;gap:14px;margin-bottom:14px}.eyebrow{margin:0;color:#75879a;font-size:9px;font-weight:800;letter-spacing:.13em}.page-header h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:11px}.header-actions{display:flex;align-items:center;gap:8px}.dev-pill,.count,.status{display:inline-flex;align-items:center;padding:4px 8px;border-radius:999px;background:#e6f3ec;color:#28744e;font-size:9px;font-weight:750}.quiet,.primary{min-height:32px;padding:0 11px;border:1px solid #ccd7e0;border-radius:5px;background:white;color:#38526a;font:inherit;cursor:pointer}.primary{border-color:#244e72;background:#244e72;color:white;font-weight:700}.quiet:disabled,.primary:disabled{cursor:not-allowed;opacity:.45}.mode-strip{display:flex;gap:18px;flex-wrap:wrap;padding:8px 11px;margin-bottom:10px;border:1px solid #dbe4ec;border-radius:5px;background:#edf3f8;color:#657b8f;font-size:9px}.mode-strip b{margin-right:4px;color:#38546e}.tabs{display:flex;gap:5px;border-bottom:1px solid #cfd9e2}.tabs button{min-height:34px;padding:0 13px;border:1px solid transparent;border-radius:5px 5px 0 0;background:transparent;color:#657b8e;font:inherit;cursor:pointer}.tabs button.active{border-color:#d4dee7;border-bottom-color:white;background:white;color:#214a6a;font-weight:750}.tabs small{margin-left:4px;color:#8b99a6;font-size:9px;font-weight:400}.notice{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:8px 10px;margin:9px 0;border:1px solid #c5dfce;border-radius:5px;background:#eff8f1;color:#286543}.notice.error{border-color:#e7c1bd;background:#fff3f1;color:#963e37}.notice button{border:0;background:transparent;color:inherit;font-size:16px;cursor:pointer}.workspace,.settings-workspace,.tools-workspace{display:grid;gap:10px;padding-top:10px}.panel{min-width:0;border:1px solid var(--line);border-radius:6px;background:white;box-shadow:0 2px 5px #162a3a08;overflow:hidden}.panel-title{display:flex;align-items:center;justify-content:space-between;gap:10px;padding:10px 12px}.panel-title h2,.tool-copy h2{margin:0;font-size:13px}.panel-title p{margin:2px 0 0;color:var(--muted);font-size:9px}.step{display:grid;place-items:center;width:24px;height:24px;border-radius:50%;background:#e8f0f6;color:#345a78;font-size:10px;font-weight:800}.query-controls{display:flex;align-items:end;gap:9px;padding:0 12px 11px}.query-controls label,.section-actions label{display:flex;flex-direction:column;gap:3px;color:#6c8194;font-size:9px}.query-controls input,.section-actions select{height:29px;min-width:255px;padding:0 8px;border:1px solid #cdd9e2;border-radius:4px;color:#263f55;font:inherit}.lot-summary{display:flex;gap:6px;flex-wrap:wrap;padding:8px 12px;border-top:1px solid #edf1f4;background:#f8fafb}.lot-summary span{display:flex;flex-direction:column;gap:2px;min-width:120px;padding:4px 7px;border-radius:4px;background:white}.lot-summary small{color:#8593a0;font-size:8px}.lot-summary b{color:#3b566f;font-size:10px}.table-scroll{overflow:auto;border-top:1px solid #e7edf1}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:9px}th{position:sticky;top:0;background:#f1f5f8;color:#65798c;font-size:8px;font-weight:800;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #edf1f4}tbody tr.selected{background:#eef6fb}td input[type=radio],td input[type=checkbox]{accent-color:#2b648c}.mono{font-family:Consolas,"Cascadia Mono",monospace;color:#526e87}.default-tag{padding:2px 5px;margin-left:4px;border-radius:3px;background:#fff2d9;color:#8a6427;font-size:8px}.empty{text-align:center;color:#8493a1;padding:16px}.section-actions{display:flex;align-items:center;justify-content:flex-end;gap:8px;padding:9px 11px}.section-actions label{margin-right:auto}.section-actions select{min-width:190px;height:28px}.printer-target{margin-right:auto;color:#788999;font-size:9px}.printer-target b{color:#38566f}.select-all{padding:0;border:0;background:transparent;color:#536c82;font:inherit;cursor:pointer}.settings-workspace{grid-template-columns:minmax(520px,1fr) minmax(220px,.42fr)}.settings-panel .section-actions{min-height:42px}.settings-panel td input{width:100%;min-width:150px;height:26px;box-sizing:border-box;padding:0 6px;border:1px solid #d5dfe7;border-radius:3px;color:#345069;font:inherit}.settings-panel td:nth-child(1){width:38px;color:#8291a0;text-align:center}.settings-panel td:nth-child(5){width:70px}.link-button{border:0;background:transparent;color:#3e6687;font:inherit;cursor:pointer}.danger-link{color:#9c4a43}.dirty-state{margin-right:auto;color:#81909d;font-size:9px}.dirty-state.dirty{color:#a06c25;font-weight:700}.handoff-note{display:flex;align-items:flex-start;gap:10px;padding:12px;height:max-content;background:#f9fbfc}.note-icon{display:grid;place-items:center;flex:none;width:20px;height:20px;border-radius:50%;background:#e6eef5;color:#486a87;font-weight:800}.handoff-note h2{margin:0 0 5px;font-size:12px}.handoff-note p{margin:0;color:#708295;font-size:10px}.tools-workspace{grid-template-columns:1fr 1fr;align-content:start}.tool-card{display:flex;align-items:center;gap:12px;min-height:128px;padding:14px}.tool-icon{display:grid;place-items:center;flex:none;width:43px;height:43px;border-radius:8px;background:#e8eef4;color:#315876;font-size:14px;font-weight:850}.printer-icon{font-size:22px}.tool-copy{flex:1}.tool-copy h2{margin:3px 0}.tool-copy p:last-child{margin:0;color:#708295;font-size:10px}.tool-state{align-self:flex-start;padding:4px 7px;border-radius:999px;background:#f8eddc;color:#8a652d;font-size:8px;font-weight:750}.tool-state.ready{background:#e6f3ec;color:#2c7450}.tool-card button{align-self:flex-end}.tools-footnote{grid-column:1/-1;margin:0;color:#7d8b98;font-size:9px}footer{padding:9px 2px 2px;color:#83919d;font-size:9px}.unavailable-note{padding:12px;border:1px solid #e5cfaa;border-radius:6px;background:#fff8ea;color:#795823;font-size:11px}.backdrop{position:fixed;inset:0;z-index:50;display:grid;place-items:center;padding:18px;background:#142536a8}.preview-dialog{display:flex;flex-direction:column;width:min(1120px,96vw);max-height:92vh;border:1px solid #d4dfe8;border-radius:8px;background:#f6f8fa;box-shadow:0 20px 55px #0e213333;overflow:hidden}.dialog-header{display:flex;align-items:center;justify-content:space-between;padding:11px 14px;border-bottom:1px solid var(--line);background:white}.dialog-header h2{margin:2px 0 0;font-size:14px}.preview-layout{display:grid;grid-template-columns:minmax(220px,.6fr) minmax(400px,1.4fr);min-height:0;overflow:auto}.preview-source{padding:12px;border-right:1px solid var(--line);background:white}.preview-source h3{margin:0 0 8px;font-size:11px}.preview-source dl{display:grid;grid-template-columns:1fr 1.2fr;gap:5px 8px;margin:0;font-size:9px}.preview-source dt{color:#80909f}.preview-source dd{margin:0;color:#405b73;font-weight:650;overflow-wrap:anywhere}.label-stage{display:grid;place-items:center;min-height:430px;padding:18px;background:#dfe5e9}.label-sheet{width:min(390px,90%);min-height:300px;box-sizing:border-box;padding:20px;border:1px solid #c5cdd2;background:white;box-shadow:0 4px 18px #25394b22}.label-brand{padding-bottom:8px;border-bottom:1px solid #243c50;color:#687e91;font-size:8px;letter-spacing:.1em}.label-code{padding:15px 0 11px;color:#172d40;font-size:25px;font-weight:850;letter-spacing:.04em}.label-line{display:flex;justify-content:space-between;gap:10px;padding:5px 0;border-bottom:1px dotted #d7dfe5;font-size:9px}.label-line span{color:#8795a1}.label-line b{color:#334c62}.label-barcode{display:flex;align-items:stretch;justify-content:space-between;height:52px;margin:18px 0 5px;padding:0 10px;overflow:hidden}.label-barcode i{display:block;background:#17232d}.label-sheet>small{display:block;color:#919ba4;font-size:8px;text-align:center}.preview-footer{display:flex;align-items:center;gap:8px;padding:9px 12px;border-top:1px solid var(--line);background:white}.preview-footer>span:not(.spacer){color:#6d8093;font-size:9px}.spacer{flex:1}.confirm-layer{z-index:60}.confirm-dialog{width:min(390px,94vw);padding:20px;border:1px solid #d5dfe7;border-radius:8px;background:white;box-shadow:0 18px 50px #10233540}.confirm-mark{display:grid;place-items:center;width:30px;height:30px;border-radius:50%;background:#fff1dc;color:#8a612a;font-weight:850}.confirm-dialog h2{margin:10px 0 4px;font-size:15px}.confirm-dialog p{margin:0;color:#6f8192;font-size:10px}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding-top:16px}@media(max-width:900px){.bartender-page{padding:13px}.settings-workspace,.tools-workspace{grid-template-columns:1fr}.query-controls{align-items:stretch;flex-direction:column}.query-controls input{min-width:0}.preview-layout{grid-template-columns:1fr}.preview-source{border-right:0;border-bottom:1px solid var(--line)}.label-stage{min-height:300px}}@media(max-width:560px){.page-header{align-items:flex-start}.page-header h1{font-size:19px}.mode-strip{gap:8px}.tabs button{padding:0 7px}.tabs small{display:none}.section-actions{align-items:stretch;flex-wrap:wrap}.section-actions label{flex-basis:100%}.section-actions select{width:100%;box-sizing:border-box}.tool-card{align-items:flex-start;flex-wrap:wrap}.tool-copy{min-width:180px}.tool-card button{margin-left:auto}}
</style>
