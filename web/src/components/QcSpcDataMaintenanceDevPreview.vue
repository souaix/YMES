<script setup lang="ts">
import { computed, ref } from 'vue'

type QcKind = 'attribute' | 'variable' | 'group'
type ChartState = 'Normal' | 'Excluded' | 'Isolated'
type ResultCode = 0 | 1 | 2 | 3 | 4
interface FactorDefinition { factorNo: string; name: string; options?: string[] }
interface QcItemOption { no: string; name: string; kind: QcKind; issueState: number; chartControl: number; factors: FactorDefinition[]; groupMembers?: string[] }
interface FactorValue { factorNo: string; value: string }
interface DefectRow { reasonNo: string; reasonName: string; reasonType: 0 | 1; areaType: string; qty: number }
interface EditLog { date: string; user: string; description: string; before: string; after: string }
interface MeasureRow {
  key: string; kind: QcKind; spcSerial: string; qcItemNo: string; lotNo: string; lotSerial: string; retestLotNo: string
  creator: string; createDate: string; result: ResultCode; chartState: ChartState; erfNo: string; logGroupSerial: string
  sampleQty: number; goodQty: number; scrapQty: number; defectQty: number; factors: FactorValue[]; defects: DefectRow[]
  testValues: number[]; mean: number; min: number; max: number; stdDev: number; cp?: number; cpk: number; lsl?: number; usl?: number; groupMembers: string[]; logs: EditLog[]; dataStamp: number
}

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const kinds: Array<{ value: QcKind; label: string }> = [{ value: 'attribute', label: '計數品管項目' }, { value: 'variable', label: '計量品管項目' }, { value: 'group', label: '計量品管項目群組' }]
const qcItems: QcItemOption[] = [
  { no: 'ATT-DEV-101', name: '外觀缺陷抽驗', kind: 'attribute', issueState: 2, chartControl: 1, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Shift', name: '班別', options: ['A', 'B', 'C'] }, { factorNo: 'Inspector', name: '檢驗員' }] },
  { no: 'ATT-DEV-102', name: '端子外觀抽驗', kind: 'attribute', issueState: -1, chartControl: 0, factors: [{ factorNo: 'Vendor', name: '供應商', options: ['V-1001', 'V-1002'] }, { factorNo: 'Material', name: '物料', options: ['MTL-01', 'MTL-02'] }] },
  { no: 'VAR-DEV-201', name: '軸心直徑', kind: 'variable', issueState: 2, chartControl: 1, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Machine', name: '設備', options: ['MC-01', 'MC-02'] }, { factorNo: 'Shift', name: '班別', options: ['A', 'B', 'C'] }] },
  { no: 'VAR-DEV-202', name: '壓合高度', kind: 'variable', issueState: 2, chartControl: 1, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Machine', name: '設備', options: ['MC-03', 'MC-04'] }] },
  { no: 'GRP-DEV-301', name: '軸心關鍵尺寸群組', kind: 'group', issueState: 2, chartControl: 1, factors: [{ factorNo: 'Product', name: '產品', options: ['PRD-A100', 'PRD-B220'] }, { factorNo: 'Shift', name: '班別', options: ['A', 'B', 'C'] }], groupMembers: ['VAR-DEV-201', 'VAR-DEV-202'] },
]
const qcTypeLabel = (kind: QcKind): string => kinds.find((item) => item.value === kind)?.label || kind
const resultLabel = (result: ResultCode): string => ['Pass', 'Warning', 'Wait', 'Hold', 'Alert'][result] || 'Unknown'
const timeNow = (): string => new Date().toLocaleString('sv-SE').replace('T', ' ')
const mkFactor = (factorNo: string, value: string): FactorValue => ({ factorNo, value })
const mkDefect = (reasonNo: string, reasonName: string, reasonType: 0 | 1, qty: number, areaType = '產品'): DefectRow => ({ reasonNo, reasonName, reasonType, areaType, qty })
const dataRows = ref<MeasureRow[]>([
  { key: 'att-001', kind: 'attribute', spcSerial: 'DEV-ATT-2001', qcItemNo: 'ATT-DEV-101', lotNo: 'LOT-A-261123-01', lotSerial: 'LS-88101', retestLotNo: '', creator: 'DEV-QC-01', createDate: '2026-11-23 08:35:00', result: 3, chartState: 'Normal', erfNo: '', logGroupSerial: 'LG-ATT-01', sampleQty: 80, goodQty: 74, scrapQty: 2, defectQty: 4, factors: [mkFactor('Product', 'PRD-A100'), mkFactor('Shift', 'A'), mkFactor('Inspector', 'DEV-QC-01')], defects: [mkDefect('D-001', '刮傷', 0, 2), mkDefect('D-015', '尺寸偏差', 1, 4)], testValues: [], mean: 0, min: 0, max: 0, stdDev: 0, cpk: 0, groupMembers: [], logs: [{ date: '2026-11-23 08:35:00', user: 'DEV-QC-01', description: '建立檢驗資料', before: '', after: 'Sample 80 / Hold' }], dataStamp: 1 },
  { key: 'att-002', kind: 'attribute', spcSerial: 'DEV-ATT-2002', qcItemNo: 'ATT-DEV-101', lotNo: 'LOT-A-261122-05', lotSerial: 'LS-88070', retestLotNo: 'LOT-A-261123-R1', creator: 'DEV-QC-02', createDate: '2026-11-22 14:20:00', result: 0, chartState: 'Excluded', erfNo: 'DEV-ERF-010', logGroupSerial: 'LG-ATT-02', sampleQty: 50, goodQty: 48, scrapQty: 1, defectQty: 1, factors: [mkFactor('Product', 'PRD-B220'), mkFactor('Shift', 'B'), mkFactor('Inspector', 'DEV-QC-02')], defects: [mkDefect('D-004', '色差', 0, 1), mkDefect('D-008', '毛邊', 1, 1)], testValues: [], mean: 0, min: 0, max: 0, stdDev: 0, cpk: 0, groupMembers: [], logs: [{ date: '2026-11-22 15:10:00', user: 'DEV-QC-03', description: '調整缺陷數量與排除狀態', before: 'Excluded / defect 2', after: 'Excluded / defect 1' }], dataStamp: 3 },
  { key: 'var-001', kind: 'variable', spcSerial: 'DEV-VAR-3011', qcItemNo: 'VAR-DEV-201', lotNo: 'LOT-V-261123-04', lotSerial: 'LS-99211', retestLotNo: '', creator: 'DEV-QC-05', createDate: '2026-11-23 09:15:00', result: 0, chartState: 'Normal', erfNo: '', logGroupSerial: 'LG-VAR-01', sampleQty: 5, goodQty: 5, scrapQty: 0, defectQty: 0, factors: [mkFactor('Product', 'PRD-A100'), mkFactor('Machine', 'MC-01'), mkFactor('Shift', 'A')], defects: [], testValues: [10.12, 10.08, 10.15, 10.11, 10.09], mean: 10.11, min: 10.08, max: 10.15, stdDev: 0.027, cpk: 1.28, lsl: 10.005, usl: 10.215, groupMembers: [], logs: [{ date: '2026-11-23 09:15:00', user: 'DEV-QC-05', description: '建立計量資料', before: '', after: '5 筆測值' }], dataStamp: 1 },
  { key: 'var-002', kind: 'variable', spcSerial: 'DEV-VAR-3012', qcItemNo: 'VAR-DEV-201', lotNo: 'LOT-V-261122-07', lotSerial: 'LS-99178', retestLotNo: '', creator: 'DEV-QC-06', createDate: '2026-11-22 16:42:00', result: 1, chartState: 'Isolated', erfNo: '', logGroupSerial: 'LG-VAR-02', sampleQty: 5, goodQty: 4, scrapQty: 0, defectQty: 1, factors: [mkFactor('Product', 'PRD-A100'), mkFactor('Machine', 'MC-02'), mkFactor('Shift', 'B')], defects: [], testValues: [10.09, 10.08, 10.11, 10.35, 10.10], mean: 10.146, min: 10.08, max: 10.35, stdDev: 0.118, cpk: 0.72, lsl: 9.892, usl: 10.4, groupMembers: [], logs: [], dataStamp: 2 },
  { key: 'var-003', kind: 'variable', spcSerial: 'DEV-VAR-3021', qcItemNo: 'VAR-DEV-202', lotNo: 'LOT-V-261123-04', lotSerial: 'LS-99211', retestLotNo: '', creator: 'DEV-QC-05', createDate: '2026-11-23 09:17:00', result: 0, chartState: 'Normal', erfNo: '', logGroupSerial: 'LG-VAR-01', sampleQty: 4, goodQty: 4, scrapQty: 0, defectQty: 0, factors: [mkFactor('Product', 'PRD-A100'), mkFactor('Machine', 'MC-03')], defects: [], testValues: [2.03, 2.01, 2.02, 2.04], mean: 2.025, min: 2.01, max: 2.04, stdDev: 0.013, cpk: 1.41, lsl: 1.98, usl: 2.07, groupMembers: [], logs: [], dataStamp: 1 },
  { key: 'group-001', kind: 'group', spcSerial: 'DEV-GSPC-5001', qcItemNo: 'GRP-DEV-301', lotNo: 'LOT-V-261123-04', lotSerial: 'LS-99211', retestLotNo: '', creator: 'DEV-QC-05', createDate: '2026-11-23 09:17:00', result: 0, chartState: 'Normal', erfNo: '', logGroupSerial: 'LG-VAR-01', sampleQty: 9, goodQty: 9, scrapQty: 0, defectQty: 0, factors: [mkFactor('Product', 'PRD-A100'), mkFactor('Shift', 'A')], defects: [], testValues: [10.12, 10.08, 10.15, 10.11, 10.09, 2.03, 2.01, 2.02, 2.04], mean: 6.96, min: 2.01, max: 10.15, stdDev: 4.12, cpk: 0.91, groupMembers: ['VAR-DEV-201', 'VAR-DEV-202'], logs: [], dataStamp: 1 },
])

const selectedKind = ref<QcKind>('attribute')
const qcItemNo = ref('')
const lotQuery = ref('')
const fromDate = ref('')
const endDate = ref('')
const factorValues = ref<Record<string, string>>({})
const searched = ref(false)
const selectedRow = ref<MeasureRow | null>(null)
const detailDraft = ref<MeasureRow | null>(null)
const detailOpen = ref(false)
const detailTab = ref<'basis' | 'raw' | 'history'>('basis')
const editMode = ref(false)
const editDescription = ref('')
const confirmDelete = ref(false)
const notice = ref('請選擇品管項目並設定查詢條件；此頁只使用 DEV 假資料。')
const messageKind = ref<'info' | 'success' | 'error'>('info')
const serialForErf = ref(13)
const canEditAttribute = ref(true)
const canEditVariable = ref(true)
const canDeleteVariable = ref(true)

const qcItemOptions = computed(() => qcItems.filter((item) => item.kind === selectedKind.value && (item.issueState === 2 || item.issueState === -1)))
const selectedItem = computed(() => qcItems.find((item) => item.no === qcItemNo.value) || null)
const factorDefinitions = computed(() => selectedItem.value?.factors || [])
const visibleRows = computed(() => {
  if (!searched.value) return []
  return dataRows.value.filter((row) => {
    if (row.kind !== selectedKind.value || row.qcItemNo !== qcItemNo.value) return false
    if (lotQuery.value.trim() && !row.lotNo.toLowerCase().includes(lotQuery.value.trim().toLowerCase())) return false
    if (fromDate.value && row.createDate.slice(0, 10) < fromDate.value.slice(0, 10)) return false
    if (endDate.value && row.createDate.slice(0, 10) > endDate.value.slice(0, 10)) return false
    return row.factors.every((factor) => !factorValues.value[factor.factorNo] || factor.value.toLowerCase().includes(factorValues.value[factor.factorNo].toLowerCase()))
  }).sort((a, b) => b.createDate.localeCompare(a.createDate))
})
const selectedHasEditPrivilege = computed(() => selectedRow.value?.kind === 'attribute' ? canEditAttribute.value : selectedRow.value?.kind === 'variable' ? canEditVariable.value : false)
const detailResultLabel = computed(() => detailDraft.value ? resultLabel(detailDraft.value.result) : '')

function setMessage(text: string, kind: 'info' | 'success' | 'error' = 'info'): void { notice.value = text; messageKind.value = kind }
function setKind(kind: QcKind): void { selectedKind.value = kind; qcItemNo.value = ''; factorValues.value = {}; searched.value = false; selectedRow.value = null }
function onItemChange(): void { factorValues.value = {}; searched.value = false }
function runQuery(): void {
  if (!qcItemNo.value) return setMessage('請先選取品管項目。', 'error')
  if (selectedKind.value === 'group' && !selectedItem.value?.groupMembers?.length) return setMessage('此群組沒有可查詢的 QC Item。', 'error')
  searched.value = true
  selectedRow.value = null
  setMessage('查詢完成：' + visibleRows.value.length + ' 筆 DEV 假資料。')
}
function openDetail(row: MeasureRow): void {
  selectedRow.value = row
  detailDraft.value = JSON.parse(JSON.stringify(row)) as MeasureRow
  detailTab.value = 'basis'
  editMode.value = false
  editDescription.value = ''
  confirmDelete.value = false
  detailOpen.value = true
}
function closeDetail(): void { detailOpen.value = false; editMode.value = false; confirmDelete.value = false; detailDraft.value = null }
function cancelEdit(): void {
  if (selectedRow.value) detailDraft.value = JSON.parse(JSON.stringify(selectedRow.value)) as MeasureRow
  editMode.value = false
  editDescription.value = ''
}
function startEdit(): void {
  if (!detailDraft.value || !selectedHasEditPrivilege.value || detailDraft.value.kind === 'group') return
  editMode.value = true
  confirmDelete.value = false
  setMessage('已進入 DEV 編輯；來源不會重新判斷 SPC 批次結果。')
}
function saveEdit(): void {
  const draft = detailDraft.value
  const original = selectedRow.value
  if (!draft || !original || !editMode.value) return
  if (!selectedHasEditPrivilege.value) return setMessage('目前 DEV 使用者沒有此類型的編修權限。', 'error')
  if (!editDescription.value.trim()) return setMessage('編修說明為必填。', 'error')
  if (!Number.isFinite(Date.parse(draft.createDate.replace(' ', 'T')))) return setMessage('CreateDate 格式無效。', 'error')
  if (draft.kind === 'attribute') {
    if (draft.defects.some((row) => row.qty === null || String(row.qty).trim() === '' || !Number.isFinite(Number(row.qty)) || Number(row.qty) < 0)) return setMessage('缺陷數量必須是零或正數。', 'error')
    const scrap = draft.defects.filter((row) => row.reasonType === 0).reduce((sum, row) => sum + Number(row.qty || 0), 0)
    const defect = draft.defects.filter((row) => row.reasonType === 1).reduce((sum, row) => sum + Number(row.qty || 0), 0)
    if (scrap > draft.sampleQty) return setMessage('報廢總數不得大於 SampleQty。', 'error')
    draft.scrapQty = scrap
    draft.defectQty = defect
    draft.goodQty = Math.max(0, draft.sampleQty - scrap)
  } else if (draft.testValues.length === 0 || draft.testValues.some((value) => String(value).trim() === '' || !Number.isFinite(Number(value)))) return setMessage('至少需要一筆有效計量值。', 'error')
  if (draft.kind === 'variable') recalculateStatistics(draft)
  const before = original.kind === 'attribute'
    ? 'ChartState=' + original.chartState + '; RetestLotNo=' + original.retestLotNo + '; defect=' + original.defectQty + '; factors=' + original.factors.map((factor) => factor.factorNo + ':' + factor.value).join('|')
    : 'ChartState=' + original.chartState + '; RetestLotNo=' + original.retestLotNo + '; values=' + original.testValues.join(',')
  const after = draft.kind === 'attribute'
    ? 'ChartState=' + draft.chartState + '; RetestLotNo=' + draft.retestLotNo + '; defect=' + draft.defectQty + '; factors=' + draft.factors.map((factor) => factor.factorNo + ':' + factor.value).join('|')
    : 'ChartState=' + draft.chartState + '; RetestLotNo=' + draft.retestLotNo + '; values=' + draft.testValues.join(',')
  const log: EditLog = { date: timeNow(), user: props.currentUserNo || 'DEV-USER', description: editDescription.value.trim(), before, after }
  Object.assign(original, JSON.parse(JSON.stringify(draft)), { dataStamp: original.dataStamp + 1, logs: [log, ...original.logs] })
  detailDraft.value = JSON.parse(JSON.stringify(original)) as MeasureRow
  editMode.value = false
  editDescription.value = ''
  setMessage('DEV 假資料已儲存；不會重新運算 SPC 判定或寫入 MES。', 'success')
}
function requestDelete(): void {
  if (!selectedRow.value || !selectedHasEditPrivilege.value) return
  if (selectedRow.value.kind === 'variable' && !canDeleteVariable.value) return setMessage('目前 DEV 使用者沒有 SPC DATA 刪除群組權限。', 'error')
  confirmDelete.value = true
}
function deleteWarning(): string {
  const row = selectedRow.value
  if (!row) return ''
  const factorKey = (item: MeasureRow): string => item.factors.map((factor) => factor.factorNo + '=' + factor.value.toLowerCase()).sort().join('|')
  const sameChartGroup = dataRows.value.filter((item) => item.kind === row.kind && item.qcItemNo === row.qcItemNo && factorKey(item) === factorKey(row)).sort((a, b) => b.createDate.localeCompare(a.createDate))
  const warnings: string[] = []
  if (sameChartGroup[0]?.key !== row.key && row.kind !== 'group') warnings.push('DEV 依相同 QC Item／要因日期近似判斷：這不是最新管制圖批次，刪除可能影響原判定')
  if (row.erfNo) warnings.push('此資料有 ERF ' + row.erfNo + '；來源刪除還會處理 ERF 子表及關聯 SPC/WIP 明細')
  return warnings.join('；') || '將刪除此 SPC 批次及相關 SPC/WIP 明細。'
}
function confirmDeleteRow(): void {
  const row = selectedRow.value
  if (!row) return
  if (!selectedHasEditPrivilege.value) return setMessage('目前 DEV 使用者沒有此類型的刪除權限。', 'error')
  if (row.kind === 'variable' && !canDeleteVariable.value) return setMessage('尚未取得 SPC DATA 刪除群組權限。', 'error')
  dataRows.value = dataRows.value.filter((item) => item.key !== row.key)
  detailOpen.value = false
  selectedRow.value = null
  confirmDelete.value = false
  setMessage('已在 DEV 假資料刪除 SPC 批次；不會操作 WIP／ERF／Oracle。', 'success')
}
function createErf(): void {
  const draft = detailDraft.value
  const row = selectedRow.value
  if (!draft || !row || draft.result !== 3 || draft.erfNo) return
  const erfNo = 'DEV-ERF-' + String(serialForErf.value++).padStart(3, '0')
  row.erfNo = erfNo
  draft.erfNo = erfNo
  row.dataStamp += 1
  setMessage('DEV 已模擬補開異常單 ' + erfNo + '；正式流程由 WP_BRERFCreate 處理。', 'success')
}
function recalculateStatistics(row: MeasureRow): void {
  const values = row.testValues.map(Number)
  const count = values.length
  const sum = values.reduce((total, value) => total + value, 0)
  const squareSum = values.reduce((total, value) => total + value * value, 0)
  const mean = sum / count
  const variance = count > 1 ? Math.abs(squareSum - count * mean * mean) / (count - 1) : 0
  const stdDev = Math.sqrt(variance)
  row.sampleQty = count
  row.mean = Number(mean.toFixed(4))
  row.min = Math.min(...values)
  row.max = Math.max(...values)
  row.stdDev = Number(stdDev.toFixed(4))
  row.cp = stdDev > 0 && row.lsl !== undefined && row.usl !== undefined
    ? Number(((row.usl - row.lsl) / (6 * stdDev)).toFixed(4))
    : 0
  row.cpk = stdDev > 0 && row.lsl !== undefined && row.usl !== undefined
    ? Number(Math.min((mean - row.lsl) / (3 * stdDev), (row.usl - mean) / (3 * stdDev)).toFixed(4)) : 0
}
function factorValue(row: MeasureRow, factorNo: string): string { return row.factors.find((factor) => factor.factorNo === factorNo)?.value || '—' }
</script>

<template>
  <section v-if="props.developmentPreview" class="spc-data-page" aria-label="SPC 資料維護">
    <header class="page-head"><div><p class="eyebrow">SPC / SC201</p><h1>SPC 資料維護</h1><p class="subtitle">查詢計數／計量檢測批次；依 DEV 權限模擬原始資料維護與刪除。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>
    <section class="query panel">
      <fieldset class="dev-permissions"><legend>DEV 權限模擬</legend><label><input v-model="canEditAttribute" type="checkbox"> 計數編修（SPCDATAEDITATT）</label><label><input v-model="canEditVariable" type="checkbox"> 計量編修（SPCDATAEDIT）</label><label><input v-model="canDeleteVariable" type="checkbox"> 計量刪除群組權限</label></fieldset>
      <div class="query-top"><div class="kind-switch" role="group" aria-label="品管項目類型"><button v-for="item in kinds" :key="item.value" type="button" :class="{ active: selectedKind === item.value }" @click="setKind(item.value)">{{ item.label }}</button></div><span class="permission">DEV 權限：SPCDATAEDITATT／SPCDATAEDIT 已授權 · 計量刪除群組已授權</span></div>
      <div class="query-grid"><label>QC Item<select v-model="qcItemNo" @change="onItemChange"><option value="">選擇品管項目</option><option v-for="item in qcItemOptions" :key="item.no" :value="item.no">{{ item.no }} — {{ item.name }}</option></select></label><label>LotNo<input v-model="lotQuery" placeholder="可留空"></label><label>建立日期起<input v-model="fromDate" type="datetime-local"></label><label>建立日期迄<input v-model="endDate" type="datetime-local"></label><button type="button" class="button primary search-button" @click="runQuery">Query</button></div>
      <div v-if="factorDefinitions.length" class="factor-filters"><strong>Collect Factor</strong><label v-for="factor in factorDefinitions" :key="factor.factorNo">{{ factor.name }}<select v-if="factor.options?.length" v-model="factorValues[factor.factorNo]"><option value="">全部</option><option v-for="option in factor.options" :key="option" :value="option">{{ option }}</option></select><input v-else v-model="factorValues[factor.factorNo]" placeholder="可留空"></label></div>
      <p class="query-note">QC Item lookup 只顯示 IssueState 2／-1；輸入 LotNo 時來源另走 LoadLogQCItem。群組以成員 QC Item 的 factor 聯集查詢。</p>
    </section>

    <section class="results panel"><header class="panel-head"><h2>檢測批次</h2><span>{{ visibleRows.length }} 筆</span></header>
      <div class="table-scroll"><table><thead><tr><th>SPCSerial</th><th>QCItemNo</th><th>LotNo</th><th>CreateDate</th><th>Result</th><th>ChartState</th><th>Qty / Mean</th><th>ERFNo</th><th>Creator</th></tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.key" :class="{ selected: selectedRow?.key === row.key }" @click="openDetail(row)"><td>{{ row.spcSerial }}</td><td>{{ row.qcItemNo }}</td><td>{{ row.lotNo }}</td><td>{{ row.createDate }}</td><td><span class="result" :data-result="row.result">{{ resultLabel(row.result) }}</span></td><td>{{ row.chartState }}</td><td>{{ row.kind === 'attribute' ? row.sampleQty : row.mean }}</td><td>{{ row.erfNo || '—' }}</td><td>{{ row.creator }}</td></tr>
        <tr v-if="!visibleRows.length"><td colspan="9" class="empty">{{ searched ? '沒有符合條件的 DEV 假資料' : '選取 QC Item 後按 Query 查詢' }}</td></tr>
      </tbody></table></div>
      <footer class="grid-foot">資料查詢使用 `LoadSPCMeasureData`；計量／群組明細使用 `LoadSPCMeasureData2`。此頁不呼叫服務或資料庫。</footer>
    </section>
    <p class="message" :class="messageKind" role="status">{{ notice }}</p>

    <div v-if="detailOpen && detailDraft" class="modal-backdrop" @click.self="closeDetail"><section class="detail-dialog" role="dialog" aria-modal="true" :aria-label="detailDraft.kind === 'attribute' ? '計數批次明細' : '計量批次明細'">
      <header class="dialog-head"><div><p class="eyebrow">{{ detailDraft.kind === 'attribute' ? 'ATTRIBUTE SAMPLE' : detailDraft.kind === 'group' ? 'GROUP SAMPLE' : 'VARIABLE SAMPLE' }}</p><h2>{{ detailDraft.spcSerial }} <small>{{ detailDraft.lotNo }}</small></h2></div><button class="button" @click="closeDetail">Close</button></header>
      <div class="access-note">目前視窗模擬單筆 SPCSerial；修改說明為必填。計量編修會重算統計值，但不會重新判定 SPC Result。</div>
      <nav class="detail-tabs"><button :class="{ active: detailTab === 'basis' }" @click="detailTab = 'basis'">批次資料</button><button :class="{ active: detailTab === 'raw' }" @click="detailTab = 'raw'">{{ detailDraft.kind === 'attribute' ? '缺陷明細' : 'Raw Data / Statistic' }}</button><button :class="{ active: detailTab === 'history' }" @click="detailTab = 'history'">修改歷程</button></nav>
      <div v-if="detailTab === 'basis'" class="detail-body">
        <dl class="basis-grid"><div><dt>QCItemNo</dt><dd>{{ detailDraft.qcItemNo }}</dd></div><div><dt>QCItemName</dt><dd>{{ selectedItem?.name || qcItems.find((item) => item.no === detailDraft?.qcItemNo)?.name }}</dd></div><div><dt>LotSerial</dt><dd>{{ detailDraft.lotSerial || '—' }}</dd></div><div><dt>SPCSerial / DataStamp</dt><dd>{{ detailDraft.spcSerial }} / {{ detailDraft.dataStamp }}</dd></div><div><dt>Result</dt><dd>{{ detailResultLabel }}</dd></div><div><dt>ERFNo</dt><dd>{{ detailDraft.erfNo || '—' }}</dd></div><div><dt>Creator / CreateDate</dt><dd>{{ detailDraft.creator }} / {{ detailDraft.createDate }}</dd></div><div><dt>RetestLotNo</dt><dd><input v-model="detailDraft.retestLotNo" :disabled="!editMode"></dd></div></dl>
        <section class="factor-card"><h3>Collect Factor</h3><div class="factor-values"><label v-for="factor in detailDraft.factors" :key="factor.factorNo">{{ factor.factorNo }}<input v-model="factor.value" :disabled="!editMode"></label><p v-if="!detailDraft.factors.length" class="empty">沒有要因</p></div></section>
        <section class="state-card"><h3>管制狀態</h3><label>ChartState<select v-model="detailDraft.chartState" :disabled="!editMode"><option>Normal</option><option>Excluded</option><option v-if="detailDraft.kind !== 'attribute'">Isolated</option></select></label><label v-if="editMode">CreateDate<input v-model="detailDraft.createDate"></label><span>來源不會因修改測值而重新判定 Result。</span></section>
        <section v-if="detailDraft.kind === 'attribute'" class="qty-cards"><div><strong>Sample</strong>{{ detailDraft.sampleQty }}</div><div><strong>Good</strong>{{ detailDraft.goodQty }}</div><div><strong>Scrap</strong>{{ detailDraft.scrapQty }}</div><div><strong>Defect</strong>{{ detailDraft.defectQty }}</div></section>
        <label v-if="editMode" class="edit-description">Edit Description <textarea v-model="editDescription" rows="2" placeholder="必填；記入本次修改原因"></textarea></label>
      </div>
      <div v-else-if="detailTab === 'raw'" class="detail-body">
        <template v-if="detailDraft.kind === 'attribute'"><div class="table-scroll inner-table"><table><thead><tr><th>ReasonNo</th><th>Reason</th><th>Type</th><th>AreaType</th><th>Quantity</th></tr></thead><tbody><tr v-for="defect in detailDraft.defects" :key="defect.reasonNo"><td>{{ defect.reasonNo }}</td><td>{{ defect.reasonName }}</td><td>{{ defect.reasonType === 0 ? 'Scrap' : 'Defect' }}</td><td>{{ defect.areaType }}</td><td><input v-model.number="defect.qty" type="number" min="0" :disabled="!editMode"></td></tr><tr v-if="!detailDraft.defects.length"><td colspan="5" class="empty">沒有缺陷明細</td></tr></tbody></table></div><div class="quantity-total">Sample {{ detailDraft.sampleQty }} · Good {{ detailDraft.goodQty }} · Scrap {{ detailDraft.scrapQty }} · Defect {{ detailDraft.defectQty }}</div></template>
        <template v-else><div class="statistics"><div><strong>SampleSize</strong>{{ detailDraft.testValues.length }}</div><div><strong>Mean</strong>{{ detailDraft.mean }}</div><div><strong>Min / Max</strong>{{ detailDraft.min }} / {{ detailDraft.max }}</div><div><strong>StdDev</strong>{{ detailDraft.stdDev }}</div><div><strong>Cpk</strong>{{ detailDraft.cpk }}</div></div><div class="table-scroll inner-table"><table><thead><tr><th>SerialNo</th><th>QCItemNo</th><th>TestValue</th></tr></thead><tbody><tr v-for="(value, index) in detailDraft.testValues" :key="index"><td>{{ index + 1 }}</td><td>{{ detailDraft.qcItemNo }}</td><td><input v-model.number="detailDraft.testValues[index]" type="number" step="0.001" :disabled="!editMode || detailDraft.kind === 'group'"></td></tr><tr v-if="!detailDraft.testValues.length"><td colspan="3" class="empty">沒有測值</td></tr></tbody></table></div><div v-if="detailDraft.kind === 'group'" class="group-members">群組成員：{{ detailDraft.groupMembers.join('、') }}；來源群組視窗不提供 Edit／Delete。</div></template>
      </div>
      <div v-else class="detail-body"><div class="table-scroll inner-table"><table><thead><tr><th>RecordDate</th><th>Editor</th><th>Description</th><th>UpdateDataOld</th><th>UpdateDataNew</th></tr></thead><tbody><tr v-for="(log, index) in detailDraft.logs" :key="index"><td>{{ log.date }}</td><td>{{ log.user }}</td><td>{{ log.description }}</td><td>{{ log.before }}</td><td>{{ log.after }}</td></tr><tr v-if="!detailDraft.logs.length"><td colspan="5" class="empty">沒有修改歷程</td></tr></tbody></table></div></div>
      <div v-if="confirmDelete" class="delete-confirm"><strong>刪除前確認</strong><p>{{ deleteWarning() }}</p><div><button class="button danger" @click="confirmDeleteRow">確認刪除 DEV 批次</button><button class="button" @click="confirmDelete = false">取消</button></div></div>
      <footer class="dialog-actions"><button v-if="detailDraft.result === 3 && !detailDraft.erfNo && detailDraft.kind !== 'group'" class="button" :disabled="editMode" @click="createErf">補開 ERF</button><button v-if="!editMode && selectedHasEditPrivilege && detailDraft.kind !== 'group'" class="button" @click="startEdit">Edit</button><button v-if="editMode" class="button primary" @click="saveEdit">Save</button><button v-if="!editMode && selectedHasEditPrivilege" class="button danger" @click="requestDelete">Delete</button><button v-if="editMode" class="button" @click="cancelEdit">Cancel Edit</button></footer>
    </section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.dev-permissions{display:flex;gap:10px 18px;flex-wrap:wrap;margin:0 0 9px;padding:5px 8px 7px;border:1px dashed #d5c77d;color:#65591c;font-size:11px}.dev-permissions legend{padding:0 4px}.dev-permissions label{display:flex;align-items:center;gap:4px}.dev-permissions input{accent-color:#c6a800}
.spc-data-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:12px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:30px;padding:5px 11px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#f9edbd;border-color:#c8a300}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.button.danger{color:#a33127}.panel{margin-bottom:10px;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.query{padding:9px 10px}.query-top{display:flex;justify-content:space-between;align-items:center;gap:12px;margin-bottom:9px}.kind-switch{display:flex;gap:4px;flex-wrap:wrap}.kind-switch button,.detail-tabs button{padding:6px 10px;border:1px solid #d4d1c7;background:#faf9f6;color:#555246;font:inherit;cursor:pointer}.kind-switch button.active,.detail-tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.permission{color:#56805a;font-size:11px}.query-grid{display:grid;grid-template-columns:1.4fr 1fr 1fr 1fr auto;align-items:end;gap:8px}.query-grid label,.factor-filters label{display:flex;flex-direction:column;gap:3px;color:#625f55;font-size:11px}.query-grid input,.query-grid select,.factor-filters input,.factor-filters select,.basis-grid input,.factor-values input,.state-card input,.state-card select,.inner-table input{min-height:29px;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:4px 7px;background:#fff;color:#292821;font:inherit}.query-grid label:first-child{min-width:235px}.search-button{min-width:78px}.factor-filters{display:flex;align-items:end;gap:8px;flex-wrap:wrap;margin-top:10px;padding-top:9px;border-top:1px solid #e9e6dc}.factor-filters strong{padding:6px 0;color:#777366;font-size:11px}.factor-filters select,.factor-filters input{min-width:130px}.query-note{margin:8px 0 0;color:#777366;font-size:11px}.panel-head{display:flex;justify-content:space-between;align-items:center;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.panel-head h2{margin:0;font-size:14px}.panel-head span{color:#777366;font-size:11px}.table-scroll{overflow:auto;max-height:390px}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left}th{position:sticky;top:0;background:#f3f1ea;color:#5e5b50;font-weight:600}tbody tr{cursor:pointer}tbody tr:hover{background:#fcf7df}tbody tr.selected{background:#fff0ad}.empty{text-align:center!important;color:#888!important;padding:18px!important}.result{display:inline-block;min-width:52px;padding:2px 6px;border-radius:9px;background:#e4f0e3;color:#36663a;text-align:center;font-size:10px}.result[data-result="1"]{background:#fff3ce;color:#846600}.result[data-result="2"]{background:#ece9fb;color:#53488c}.result[data-result="3"],.result[data-result="4"]{background:#ffebe7;color:#9b3429}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:11px}.message{position:sticky;bottom:0;margin:0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.message.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.message.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:15px;background:#20201866}.detail-dialog{width:min(960px,97vw);max-height:92vh;overflow:auto;padding:13px;border:1px solid #c5c1b4;border-radius:5px;background:#fff;box-shadow:0 16px 48px #0003}.dialog-head{display:flex;justify-content:space-between;align-items:center;padding-bottom:8px;border-bottom:1px solid #e6e3da}.dialog-head h2{margin:2px 0;font-size:18px}.dialog-head h2 small{font-size:13px;font-weight:400;color:#777366}.access-note{margin:8px 0;padding:6px 8px;background:#f7f5ea;color:#777366;font-size:11px}.detail-tabs{display:flex;gap:4px;border-bottom:1px solid #d4d1c7}.detail-body{min-height:300px;padding:10px 1px}.basis-grid{display:grid;grid-template-columns:1fr 1fr;gap:0 16px;margin:0}.basis-grid div{display:grid;grid-template-columns:155px minmax(0,1fr);gap:8px;padding:6px 2px;border-bottom:1px solid #f0eee8}.basis-grid dt{color:#777366;font-size:11px}.basis-grid dd{margin:0;overflow-wrap:anywhere}.basis-grid input{width:100%}.factor-card,.state-card{margin-top:10px;padding:8px;border:1px solid #e4e1d8;border-radius:3px}.factor-card h3,.state-card h3{margin:0 0 6px;font-size:12px}.factor-values{display:flex;gap:7px;flex-wrap:wrap}.factor-values label{display:flex;flex-direction:column;gap:3px;color:#777366;font-size:10px}.factor-values input{min-width:150px}.factor-values .empty{width:100%;padding:6px!important}.state-card{display:flex;align-items:end;gap:10px;flex-wrap:wrap}.state-card h3{width:100%}.state-card label{display:flex;flex-direction:column;gap:3px;color:#777366;font-size:10px}.state-card span{color:#777366;font-size:11px}.qty-cards,.statistics{display:flex;gap:8px;flex-wrap:wrap;margin-top:9px}.qty-cards div,.statistics div{min-width:95px;padding:7px;border:1px solid #e4e1d8;border-radius:3px;background:#faf9f6}.qty-cards strong,.statistics strong{display:block;color:#777366;font-size:10px}.edit-description{display:flex;flex-direction:column;gap:4px;margin-top:9px;color:#777366;font-size:11px}.edit-description textarea{border:1px solid #c8c5bb;border-radius:3px;padding:6px;font:inherit}.inner-table{max-height:330px}.inner-table input{width:110px}.quantity-total,.group-members{margin-top:9px;padding:7px;background:#f7f5ea;color:#625f55}.delete-confirm{margin:8px 0;padding:10px;border:1px solid #dcaaa1;border-radius:3px;background:#fff0ed;color:#842a21}.delete-confirm p{margin:5px 0 9px}.delete-confirm>div{display:flex;gap:6px}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding-top:9px;border-top:1px solid #e6e3da}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
@media(max-width:1000px){.query-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.query-top{align-items:flex-start;flex-direction:column}.table-scroll{max-height:260px}.basis-grid{grid-template-columns:1fr}.detail-dialog{max-height:96vh}}
</style>
