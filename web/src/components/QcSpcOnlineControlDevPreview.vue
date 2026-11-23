<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

interface Factor { key: string; name: string; required: boolean }
interface QcItem {
  qcItemNo: string
  name: string
  type: number
  samplePlan: string
  sampleQty: number
  lowerLimit: number
  upperLimit: number
  unit: string
  factors: Factor[]
  members?: QcItem[]
}
interface Reading { serialNo: number; value: string; memo: string }
interface Defect { reasonNo: string; qty: string; area: string; memo: string }
interface MeasureBucket { readings: Reading[]; goodQty: string; scrapQty: string; defects: Defect[] }
interface CollectionLog { spcSerial: string; lotNo: string; qcItemNo: string; qcItemName: string; kind: string; sampleQty: number; result: string; userNo: string; createdAt: string; note: string }

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()

const operations = [
  { opNo: 'OP-ASSY-20', name: '成品組裝檢驗', authorized: true },
  { opNo: 'OP-TEST-30', name: '功能測試', authorized: true },
  { opNo: 'OP-REVIEW-90', name: '品質覆核（無 SPC 權限）', authorized: false },
]
const commonFactors: Factor[] = [
  { key: 'shift', name: '班別', required: true },
  { key: 'equipment', name: '檢測設備', required: true },
  { key: 'operator', name: '操作人員', required: true },
]
const qcItems: QcItem[] = [
  { qcItemNo: 'QC-V-THK-001', name: '產品厚度', type: 1, samplePlan: 'PLAN-V-5', sampleQty: 5, lowerLimit: 0.95, upperLimit: 1.05, unit: 'mm', factors: commonFactors },
  { qcItemNo: 'QC-A-LOOK-002', name: '外觀缺點', type: 0, samplePlan: 'PLAN-AQL-II', sampleQty: 20, lowerLimit: 0, upperLimit: 1, unit: '件', factors: commonFactors },
  { qcItemNo: 'QC-G-PROC-003', name: '製程巡檢群組', type: 2, samplePlan: 'PLAN-GROUP-5', sampleQty: 5, lowerLimit: 0.95, upperLimit: 1.05, unit: '項', factors: commonFactors, members: [
    { qcItemNo: 'QC-V-WIDTH-011', name: '產品寬度', type: 1, samplePlan: 'PLAN-V-5', sampleQty: 5, lowerLimit: 9.8, upperLimit: 10.2, unit: 'mm', factors: commonFactors },
    { qcItemNo: 'QC-V-HEIGHT-012', name: '產品高度', type: 1, samplePlan: 'PLAN-V-5', sampleQty: 5, lowerLimit: 4.8, upperLimit: 5.2, unit: 'mm', factors: commonFactors },
  ] },
  { qcItemNo: 'QC-G-DED-004', name: '製程扣除群組', type: 4, samplePlan: 'PLAN-GROUP-5', sampleQty: 5, lowerLimit: 0.95, upperLimit: 1.05, unit: '項', factors: commonFactors, members: [
    { qcItemNo: 'QC-V-POWER-021', name: '輸出功率', type: 1, samplePlan: 'PLAN-V-5', sampleQty: 5, lowerLimit: 17.5, upperLimit: 18.5, unit: 'W', factors: commonFactors },
    { qcItemNo: 'QC-V-CURRENT-022', name: '工作電流', type: 1, samplePlan: 'PLAN-V-5', sampleQty: 5, lowerLimit: 0.9, upperLimit: 1.1, unit: 'A', factors: commonFactors },
  ] },
]
const qcTypeOptions = [
  { type: 0, label: '計數', helper: '缺點數與良品數' },
  { type: 1, label: '計量', helper: '逐筆量測值' },
  { type: 2, label: '計量群組', helper: '多品管項目分頁收集' },
  { type: 4, label: '群組減項', helper: '依扣除群組資料模擬' },
]
const samplePlans = [
  { planNo: 'PLAN-V-5', planType: 1, name: '計量標準抽樣', sampleQty: 5 },
  { planNo: 'PLAN-AQL-II', planType: 2, name: '計數 AQL 一般檢驗 II', sampleQty: 20 },
  { planNo: 'PLAN-GROUP-5', planType: 1, name: '群組計量抽樣', sampleQty: 5 },
]
const reasons = [
  { reasonNo: 'SCRATCH', name: '刮傷' },
  { reasonNo: 'STAIN', name: '污漬' },
  { reasonNo: 'MISSING', name: '缺件' },
]

const readQueryValue = (name: string): string => {
  if (typeof window === 'undefined') return ''
  return new URLSearchParams(window.location.search).get(name) || new URLSearchParams(window.location.search).get(name.toLowerCase()) || ''
}
const queryOpNo = readQueryValue('OPNo')
const queryLotNo = readQueryValue('LotNo')
const queryQcItemNo = readQueryValue('QCItemNo')
const senderName = readQueryValue('SenderName')
const queryTypeRaw = readQueryValue('QCItemType')
const queryTypeValue = queryTypeRaw === '' ? 1 : Number(queryTypeRaw)
const selectedType = ref([0, 1, 2, 4].includes(queryTypeValue) ? queryTypeValue : 1)
const opNo = ref(operations.some((row) => row.opNo === queryOpNo) ? queryOpNo : 'OP-ASSY-20')
const lotNo = ref(queryLotNo || 'DEV-LOT-261123-01')
const populationQty = ref('480')
const initialItem = qcItems.find((row) => row.type === selectedType.value)
const qcItemNo = ref(qcItems.some((row) => row.qcItemNo === queryQcItemNo && row.type === selectedType.value) ? queryQcItemNo : (initialItem?.qcItemNo || ''))
const originalLotNo = ref('')
const retestLot = ref(false)
const samplePlanNo = ref(initialItem?.samplePlan || 'PLAN-V-5')
const sampleQty = ref(String(initialItem?.sampleQty || 5))
const parameterValues = reactive({ AQL: '1.0', InspectionLevel: 'II', SampleQty: '5', PopulationQty: '480' })
const factorValues = reactive<Record<string, string>>({ shift: 'DAY', equipment: 'DEV-EQP-101', operator: props.currentUserNo?.trim() || 'DEV-USER' })
const measurementBuckets = reactive<Record<string, MeasureBucket>>({})
const activeMeasurementItemNo = ref('')
const collectionOpen = ref(false)
const lookupOpen = ref<'operation' | 'item' | null>(null)
const lookupSearch = ref('')
const prepared = ref(false)
const message = ref(senderName ? 'DEV 已接收入口情境；本頁不會回寫呼叫端檢驗單。' : '')
const messageKind = ref<'info' | 'warning' | 'error' | 'success'>('info')
const logs = ref<CollectionLog[]>([])
const lastSaved = ref<CollectionLog[]>([])

const selectedOperation = computed(() => operations.find((row) => row.opNo === opNo.value) || null)
const visibleItems = computed(() => qcItems.filter((row) => row.type === selectedType.value))
const selectedItem = computed(() => visibleItems.value.find((row) => row.qcItemNo === qcItemNo.value) || null)
const measurementItems = computed(() => selectedItem.value?.members?.length ? selectedItem.value.members : selectedItem.value ? [selectedItem.value] : [])
const activeMeasurementItem = computed(() => measurementItems.value.find((row) => row.qcItemNo === activeMeasurementItemNo.value) || measurementItems.value[0] || null)
const availableSamplePlans = computed(() => samplePlans.filter((row) => row.planType === (selectedType.value === 0 ? 2 : 1)))
const selectedPlan = computed(() => availableSamplePlans.value.find((row) => row.planNo === samplePlanNo.value) || null)
const lookupOptions = computed(() => {
  const needle = lookupSearch.value.trim().toLowerCase()
  if (lookupOpen.value === 'operation') return operations.filter((row) => !needle || (row.opNo + ' ' + row.name).toLowerCase().includes(needle)).map((row) => ({ value: row.opNo, label: row.name }))
  return visibleItems.value.filter((row) => !needle || (row.qcItemNo + ' ' + row.name).toLowerCase().includes(needle)).map((row) => ({ value: row.qcItemNo, label: row.name }))
})
const entryKind = computed(() => selectedType.value === 0 ? '計數' : '計量')

function setType(type: number): void {
  selectedType.value = type
  const first = qcItems.find((row) => row.type === type)
  qcItemNo.value = first?.qcItemNo || ''
  samplePlanNo.value = first?.samplePlan || ''
  sampleQty.value = String(first?.sampleQty || 0)
  parameterValues.SampleQty = sampleQty.value
  prepared.value = false
  message.value = ''
}

function selectItem(item: QcItem): void {
  qcItemNo.value = item.qcItemNo
  samplePlanNo.value = item.samplePlan
  sampleQty.value = String(item.sampleQty)
  parameterValues.SampleQty = sampleQty.value
  parameterValues.PopulationQty = populationQty.value
  prepared.value = false
  lookupOpen.value = null
  message.value = ''
}

function openLookup(kind: 'operation' | 'item'): void {
  lookupOpen.value = kind
  lookupSearch.value = ''
}

function selectOperation(value: string): void {
  const row = operations.find((item) => item.opNo === value)
  if (!row) return
  opNo.value = row.opNo
  lookupOpen.value = null
  if (!row.authorized) {
    message.value = '此 DEV 使用者沒有該作業站 SPC 權限；來源會在選擇作業站時阻擋。'
    messageKind.value = 'error'
  } else {
    message.value = '已選擇作業站。正式環境必須由伺服器重新驗證使用者的 SPC OP 權限。'
    messageKind.value = 'info'
  }
}

function selectLookupOption(value: string): void {
  if (lookupOpen.value === 'operation') {
    selectOperation(value)
    return
  }
  const item = qcItems.find((row) => row.qcItemNo === value)
  if (item) selectItem(item)
}

function ensureBucket(item: QcItem): MeasureBucket {
  const existing = measurementBuckets[item.qcItemNo]
  if (existing) return existing
  const bucket: MeasureBucket = item.type === 0
    ? { readings: [], goodQty: String(Math.max(0, item.sampleQty - 1)), scrapQty: '1', defects: [{ reasonNo: 'SCRATCH', qty: '1', area: 'EDGE', memo: '' }] }
    : { readings: Array.from({ length: Number(sampleQty.value) || item.sampleQty }, (_, index) => ({ serialNo: index + 1, value: '', memo: '' })), goodQty: '', scrapQty: '', defects: [] }
  measurementBuckets[item.qcItemNo] = bucket
  return bucket
}

function prepareCollection(): void {
  const op = selectedOperation.value
  const item = selectedItem.value
  const qty = Number(populationQty.value)
  const size = Number(sampleQty.value)
  if (!opNo.value || !op || !op.authorized) {
    message.value = '請選擇有 SPC 權限的作業站。'
    messageKind.value = 'error'
    return
  }
  if (!lotNo.value.trim() || !item) {
    message.value = '請輸入 Lot No 並選擇品管項目。'
    messageKind.value = 'error'
    return
  }
  if (!Number.isFinite(qty) || qty <= 0 || !Number.isInteger(qty) || !Number.isFinite(size) || size <= 0 || !Number.isInteger(size)) {
    message.value = '母體數量與抽樣數必須是大於 0 的整數。'
    messageKind.value = 'error'
    return
  }
  if (!retestLot.value && size > qty) {
    message.value = 'DEV 假資料規則：抽樣數不可大於母體數。WinForms 另依參數設定處理此限制。'
    messageKind.value = 'error'
    return
  }
  if (retestLot.value && !originalLotNo.value.trim()) {
    message.value = '複檢批需輸入原始 Lot No。'
    messageKind.value = 'error'
    return
  }
  const missingFactor = item.factors.find((row) => row.required && !String(factorValues[row.key] || '').trim())
  if (missingFactor) {
    message.value = '請輸入必要要因：' + missingFactor.name
    messageKind.value = 'error'
    return
  }
  if (!selectedPlan.value) {
    message.value = '請選擇 DEV 抽樣計畫。'
    messageKind.value = 'error'
    return
  }
  parameterValues.SampleQty = String(size)
  parameterValues.PopulationQty = String(qty)
  measurementItems.value.forEach((measurementItem) => {
    const bucket = ensureBucket(measurementItem)
    if (measurementItem.type !== 0 && bucket.readings.length !== size) {
      bucket.readings = Array.from({ length: size }, (_, index) => bucket.readings[index] || ({ serialNo: index + 1, value: '', memo: '' }))
    }
  })
  prepared.value = true
  activeMeasurementItemNo.value = measurementItems.value[0]?.qcItemNo || ''
  collectionOpen.value = true
  message.value = '抽樣參數已載入 DEV；接續開啟 ' + entryKind.value + ' 收集視窗。'
  messageKind.value = 'info'
}

function fillSampleValues(): void {
  const item = activeMeasurementItem.value
  if (!item) return
  const bucket = ensureBucket(item)
  if (item.type === 0) {
    bucket.goodQty = String(Math.max(0, Number(sampleQty.value) - 1))
    bucket.scrapQty = '1'
    bucket.defects = [{ reasonNo: 'SCRATCH', qty: '1', area: 'EDGE', memo: 'DEV 模擬量測' }]
  } else {
    const center = (item.lowerLimit + item.upperLimit) / 2
    bucket.readings = Array.from({ length: Number(sampleQty.value) || item.sampleQty }, (_, index) => ({ serialNo: index + 1, value: (center + (index - 2) * (item.upperLimit - item.lowerLimit) * 0.08).toFixed(3), memo: 'DEV 模擬量測' }))
  }
  message.value = '已填入 DEV 假量測資料；不會連接 RS232、儀器或資料匯入服務。'
  messageKind.value = 'info'
}

function appendDefect(): void {
  const item = activeMeasurementItem.value
  if (!item) return
  ensureBucket(item).defects.push({ reasonNo: 'STAIN', qty: '1', area: '', memo: '' })
}

function saveCollection(): void {
  const op = selectedOperation.value
  if (!prepared.value || !op || !selectedItem.value) return
  const pendingLogs: CollectionLog[] = []
  for (const item of measurementItems.value) {
    const bucket = ensureBucket(item)
    if (item.type === 0) {
      const sample = Number(sampleQty.value)
      const good = Number(bucket.goodQty)
      const scrap = Number(bucket.scrapQty)
      if (!Number.isInteger(good) || !Number.isInteger(scrap) || good < 0 || scrap < 0 || good + scrap !== sample) {
        message.value = item.name + '：良品數與報廢數必須相加等於抽樣數。'
        messageKind.value = 'error'
        activeMeasurementItemNo.value = item.qcItemNo
        return
      }
      const invalidDefect = bucket.defects.some((row) => !row.reasonNo || !Number.isInteger(Number(row.qty)) || Number(row.qty) <= 0)
      if (invalidDefect) {
        message.value = item.name + '：缺點原因及缺點數必須完整。'
        messageKind.value = 'error'
        activeMeasurementItemNo.value = item.qcItemNo
        return
      }
      const defectCount = bucket.defects.reduce((sum, row) => sum + Number(row.qty), 0)
      pendingLogs.push({ spcSerial: '', lotNo: lotNo.value.trim(), qcItemNo: item.qcItemNo, qcItemName: item.name, kind: '計數', sampleQty: sample, result: defectCount > 0 ? '有缺點' : '通過', userNo: factorValues.operator, createdAt: '', note: bucket.defects.map((row) => row.reasonNo + ':' + row.qty).join('、') || '無缺點' })
    } else {
      const readings = bucket.readings.slice(0, Number(sampleQty.value))
      if (readings.length !== Number(sampleQty.value) || readings.some((row) => row.value.trim() === '' || !Number.isFinite(Number(row.value)))) {
        message.value = item.name + '：每個抽樣位置都需輸入有效數值。'
        messageKind.value = 'error'
        activeMeasurementItemNo.value = item.qcItemNo
        return
      }
      const outside = readings.filter((row) => Number(row.value) < item.lowerLimit || Number(row.value) > item.upperLimit).length
      pendingLogs.push({ spcSerial: '', lotNo: lotNo.value.trim(), qcItemNo: item.qcItemNo, qcItemName: item.name, kind: '計量', sampleQty: readings.length, result: outside ? '超出規格 ' + outside + ' 筆' : '規格內', userNo: factorValues.operator, createdAt: '', note: '規格 ' + item.lowerLimit + '–' + item.upperLimit + ' ' + item.unit })
    }
  }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  const groupToken = Date.now().toString(36).toUpperCase()
  pendingLogs.forEach((row, index) => {
    row.spcSerial = 'DEV-SPC-' + groupToken + '-' + String(index + 1).padStart(2, '0')
    row.createdAt = now
  })
  lastSaved.value = pendingLogs
  logs.value = [...pendingLogs, ...logs.value]
  collectionOpen.value = false
  message.value = pendingLogs.length + ' 筆量測資料已在本頁模擬完成；SPC Serial 與異常結果均為 DEV 假資料，沒有寫入 MES。'
  messageKind.value = pendingLogs.some((row) => row.result !== '規格內' && row.result !== '通過') ? 'warning' : 'success'
  prepared.value = false
}

function startNextCollection(): void {
  measurementItems.value.forEach((item) => { delete measurementBuckets[item.qcItemNo] })
  lastSaved.value = []
  message.value = '已清除本次輸入，可開始下一筆 DEV 收集。'
  messageKind.value = 'info'
}
</script>

<template>
  <main class="spc-page" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-header">
      <div><div class="eyebrow">SPC · SC101 · DEV fixtures</div><h1>SPC 量測數據收集 <span>SPC ONLINE CONTROL</span></h1><p>依作業站、品管項目與抽樣計畫啟動計量或計數收集。</p></div>
      <button type="button" class="icon-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>不連接 MES Service、資料庫、檢測儀器或 ERP；本頁操作只保存在瀏覽器記憶體。</span></div>

    <section class="setup-card">
      <div class="section-heading"><div><h2>收集條件</h2><span>WinForms：作業站 → 品管項目 → 批號／要因 → 抽樣參數</span></div><span class="entry-context">{{ senderName ? '入口情境：' + senderName : '獨立收集' }}</span></div>
      <div class="setup-grid">
        <label for="spc-op">作業站 OP</label><div class="lookup-control"><input id="spc-op" v-model="opNo" maxlength="50" /><button type="button" aria-label="查找作業站" @click="openLookup('operation')">…</button></div>
        <label for="spc-lot">Lot No</label><input id="spc-lot" v-model="lotNo" maxlength="50" />
        <label for="spc-population">母體數量</label><input id="spc-population" v-model="populationQty" inputmode="numeric" />
        <label for="spc-qc-item">品管項目</label><div class="lookup-control"><input id="spc-qc-item" :value="selectedItem ? selectedItem.qcItemNo + ' · ' + selectedItem.name : ''" readonly placeholder="依類型選擇品管項目" /><button type="button" aria-label="查找品管項目" @click="openLookup('item')">…</button></div>
        <label class="retest-toggle"><input v-model="retestLot" type="checkbox" />複檢批</label><div class="lookup-control" :class="{ inactive: !retestLot }"><input v-model="originalLotNo" :disabled="!retestLot" placeholder="原始 Lot No" /><span class="soft-label">Org Lot</span></div>
      </div>
      <div class="type-selector" role="radiogroup" aria-label="品管項目類型">
        <button v-for="option in qcTypeOptions" :key="option.type" type="button" :class="{ active: selectedType === option.type }" @click="setType(option.type)"><strong>{{ option.label }}</strong><span>{{ option.helper }}</span></button>
      </div>
      <p v-if="selectedOperation" class="op-hint" :class="{ denied: !selectedOperation.authorized }">{{ selectedOperation.name }}{{ selectedOperation.authorized ? ' · DEV 使用者有 SPC 權限' : ' · DEV 使用者無 SPC 權限' }}</p>
      <p v-if="message" class="feedback" :class="'feedback-' + messageKind" role="status">{{ message }}</p>
    </section>

    <div class="content-grid">
      <section class="factor-card">
        <div class="section-heading"><div><h2>收集要因</h2><span>Factor Online 欄位由品管項目假資料提供</span></div><button type="button" class="text-button" @click="factorValues.shift = 'NIGHT'; factorValues.equipment = 'DEV-EQP-201'">載入另一組</button></div>
        <div class="factor-list"><label v-for="factor in (selectedItem?.factors || [])" :key="factor.key"><span>{{ factor.name }}<b v-if="factor.required">*</b></span><input v-model="factorValues[factor.key]" :placeholder="factor.name" /></label></div>
      </section>

      <section class="plan-card">
        <div class="section-heading"><div><h2>抽樣計畫與參數</h2><span>批號母體、計畫型別及抽樣值</span></div><span class="count-badge">{{ selectedPlan?.planType === 2 ? '計數' : '計量' }}</span></div>
        <div class="plan-row"><label for="spc-plan">Sampling Plan</label><select id="spc-plan" v-model="samplePlanNo" @change="sampleQty = String(selectedPlan?.sampleQty || sampleQty); parameterValues.SampleQty = sampleQty"><option v-for="plan in availableSamplePlans" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }} · {{ plan.name }}</option></select></div>
        <div class="parameter-table"><div class="parameter-header"><span>Parameter No</span><span>Parameter Value</span><span>來源</span></div><label><span>SampleQty</span><input v-model="sampleQty" inputmode="numeric" @input="parameterValues.SampleQty = sampleQty" /><span>DEV 抽樣值</span></label><label><span>PopulationQty</span><input :value="populationQty" readonly /><span>批號母體</span></label><label v-if="selectedType === 0"><span>AQL</span><input v-model="parameterValues.AQL" /><span>計畫參數</span></label><label v-else><span>InspectionLevel</span><input v-model="parameterValues.InspectionLevel" /><span>計畫參數</span></label></div>
        <div class="form-actions"><button type="button" class="primary-button" @click="prepareCollection">確認參數並開始收集</button><button type="button" class="secondary-button" @click="emit('close')">Close</button></div>
      </section>
    </div>

    <section v-if="lastSaved.length" class="result-banner"><div><strong>本次 DEV 收集完成</strong><span>{{ lastSaved.map((row) => row.qcItemName + '：' + row.result).join('；') }}</span></div><button type="button" class="secondary-button" @click="startNextCollection">下一筆收集</button></section>

    <section class="history-card">
      <div class="section-heading"><div><h2>本頁收集歷程</h2><span>本次工作階段記憶體資料，重新載入後清除</span></div><span class="count-badge">{{ logs.length }} 筆</span></div>
      <div class="table-scroll"><table><thead><tr><th>SPC Serial</th><th>Lot No</th><th>QC Item</th><th>類型</th><th>抽樣數</th><th>結果</th><th>操作人</th><th>時間</th><th>說明</th></tr></thead><tbody>
        <tr v-for="row in logs" :key="row.spcSerial"><td class="code-cell">{{ row.spcSerial }}</td><td>{{ row.lotNo }}</td><td>{{ row.qcItemNo }} · {{ row.qcItemName }}</td><td>{{ row.kind }}</td><td>{{ row.sampleQty }}</td><td><span class="result-chip" :class="row.result === '規格內' || row.result === '通過' ? 'pass' : 'flag'">{{ row.result }}</span></td><td>{{ row.userNo }}</td><td>{{ row.createdAt }}</td><td>{{ row.note }}</td></tr>
        <tr v-if="!logs.length"><td colspan="9" class="empty-row">完成一筆 DEV 收集後，結果會顯示在此。</td></tr>
      </tbody></table></div>
    </section>

    <div v-if="lookupOpen" class="modal-backdrop" @click.self="lookupOpen = null"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupOpen === 'operation' ? '查找作業站' : '查找品管項目'"><header><div><strong>{{ lookupOpen === 'operation' ? '選擇作業站' : '選擇品管項目' }}</strong><span>DEV 假資料查找</span></div><button type="button" class="icon-button" aria-label="關閉查找視窗" @click="lookupOpen = null">×</button></header><input v-model="lookupSearch" class="search-input" type="search" placeholder="輸入編號或名稱" /><div class="lookup-options"><button v-for="option in lookupOptions" :key="option.value" type="button" @click="selectLookupOption(option.value)"><strong>{{ option.value }}</strong><span>{{ option.label }}</span></button><p v-if="!lookupOptions.length" class="empty-row">沒有符合項目。</p></div></section></div>

    <div v-if="collectionOpen" class="modal-backdrop collection-backdrop"><section class="collection-dialog" role="dialog" aria-modal="true" aria-label="SPC 資料收集">
      <header class="collection-header"><div><div class="eyebrow">{{ entryKind }} · MEASUREMENT INPUT</div><h2>{{ selectedItem?.name }} <span>{{ selectedItem?.qcItemNo }}</span></h2><p>{{ lotNo }} · {{ selectedOperation?.opNo }} · {{ samplePlanNo }}</p></div><button type="button" class="icon-button" aria-label="關閉收集視窗" @click="collectionOpen = false">×</button></header>
      <div class="group-tabs" v-if="measurementItems.length > 1"><button v-for="item in measurementItems" :key="item.qcItemNo" type="button" :class="{ active: (activeMeasurementItem?.qcItemNo || '') === item.qcItemNo }" @click="activeMeasurementItemNo = item.qcItemNo">{{ item.qcItemNo }}<span>{{ item.name }}</span></button><b v-if="selectedType === 4">扣除群組 DEV 模擬</b></div>
      <div v-if="activeMeasurementItem" class="measurement-body">
        <div class="measurement-meta"><span>抽樣數 <strong>{{ sampleQty }}</strong></span><span>規格下限 <strong>{{ activeMeasurementItem.lowerLimit }} {{ activeMeasurementItem.unit }}</strong></span><span>規格上限 <strong>{{ activeMeasurementItem.upperLimit }} {{ activeMeasurementItem.unit }}</strong></span><span>操作人 <strong>{{ factorValues.operator }}</strong></span></div>
        <template v-if="activeMeasurementItem.type !== 0"><div class="measurement-toolbar"><div><h3>逐筆量測值</h3><span>超出規格的讀值會保留並顯示提醒</span></div><div><button type="button" class="secondary-button" @click="fillSampleValues">模擬儀器輸入</button><button type="button" class="secondary-button" @click="fillSampleValues">模擬檔案匯入</button></div></div><div class="table-scroll measurement-table"><table><thead><tr><th>序號</th><th>量測值 ({{ activeMeasurementItem.unit }})</th><th>判定</th><th>Memo</th></tr></thead><tbody><tr v-for="reading in ensureBucket(activeMeasurementItem).readings" :key="reading.serialNo"><td>{{ reading.serialNo }}</td><td><input v-model="reading.value" inputmode="decimal" :aria-label="'第 ' + reading.serialNo + ' 筆量測值'" /></td><td><span v-if="reading.value !== ''" :class="Number(reading.value) < activeMeasurementItem.lowerLimit || Number(reading.value) > activeMeasurementItem.upperLimit ? 'reading-fail' : 'reading-pass'">{{ Number(reading.value) < activeMeasurementItem.lowerLimit || Number(reading.value) > activeMeasurementItem.upperLimit ? '超規' : '規格內' }}</span><span v-else>待輸入</span></td><td><input v-model="reading.memo" /></td></tr></tbody></table></div></template>
        <template v-else><div class="measurement-toolbar"><div><h3>計數檢驗摘要</h3><span>抽樣數 {{ sampleQty }}；依原因記錄缺點</span></div></div><div class="count-inputs"><label>良品數<input v-model="ensureBucket(activeMeasurementItem).goodQty" inputmode="numeric" /></label><label>報廢數<input v-model="ensureBucket(activeMeasurementItem).scrapQty" inputmode="numeric" /></label><label>不良數<input :value="ensureBucket(activeMeasurementItem).defects.reduce((sum, row) => sum + (Number(row.qty) || 0), 0)" readonly /></label></div><div class="defect-heading"><h3>缺點明細</h3><button type="button" class="secondary-button" @click="appendDefect">新增缺點</button></div><div class="defect-list"><div v-for="(defect, index) in ensureBucket(activeMeasurementItem).defects" :key="index" class="defect-row"><select v-model="defect.reasonNo"><option v-for="reason in reasons" :key="reason.reasonNo" :value="reason.reasonNo">{{ reason.reasonNo }} · {{ reason.name }}</option></select><input v-model="defect.qty" inputmode="numeric" aria-label="缺點數" /><input v-model="defect.area" placeholder="Area" /><input v-model="defect.memo" placeholder="Memo" /></div><p v-if="!ensureBucket(activeMeasurementItem).defects.length" class="empty-row">沒有缺點。</p></div></template>
      </div>
      <footer class="collection-footer"><span>Data is not persisted · DEV only</span><div><button type="button" class="secondary-button" @click="collectionOpen = false">取消</button><button type="button" class="primary-button" @click="saveCollection">DEV 模擬儲存</button></div></footer>
    </section></div>
  </main>
</template>

<style scoped>
.spc-page { min-height: 0; height: 100%; overflow: auto; padding: 16px; box-sizing: border-box; background: #f5f4ef; color: #26251f; font: inherit; }
.page-header,.section-heading,.form-actions,.lookup-dialog header,.collection-header,.collection-footer,.measurement-toolbar,.defect-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header { max-width: 1160px; margin: 0 auto 12px; align-items: flex-start; }.eyebrow,.section-heading span,.lookup-dialog header span,.measurement-toolbar span { color: #777366; font-size: 12px; }
h1 { margin: 3px 0; font-size: 22px; }h1 span { margin-left: 8px; color: #777366; font-size: 13px; font-weight: 500; }.page-header p { margin: 3px 0 0; color: #777366; font-size: 12px; }
.icon-button { width: 32px; height: 32px; border: 1px solid #c9c6b9; border-radius: 4px; background: white; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner,.setup-card,.factor-card,.plan-card,.history-card,.result-banner { max-width: 1160px; margin: 0 auto 12px; border: 1px solid #d9d6ca; border-radius: 6px; background: white; }.fixture-banner { display: flex; align-items: center; gap: 10px; padding: 9px 12px; background: #fff9df; color: #655612; font-size: 13px; }.fixture-banner strong { padding: 2px 7px; border-radius: 10px; background: #efdb87; font-size: 11px; white-space: nowrap; }
.setup-card,.factor-card,.plan-card,.history-card { padding: 14px; }.section-heading { margin-bottom: 12px; align-items: flex-start; }.section-heading h2 { display: inline; margin: 0 9px 0 0; font-size: 15px; }.section-heading > div > span { color: #777366; font-size: 12px; }.entry-context,.count-badge { padding: 3px 8px; border-radius: 12px; background: #efeee8; color: #625f55; font-size: 11px; white-space: nowrap; }
.setup-grid { display: grid; grid-template-columns: 120px minmax(180px,1fr) 110px minmax(180px,1fr); align-items: center; gap: 9px 12px; max-width: 900px; }.setup-grid > label { text-align: right; color: #565349; font-size: 13px; }.setup-grid input:not([type=checkbox]),.lookup-control input,.factor-list input,.plan-row select,.parameter-table input,.search-input,.measurement-table input,.count-inputs input,.defect-row input,.defect-row select { width: 100%; min-height: 32px; padding: 5px 8px; box-sizing: border-box; border: 1px solid #c9c6b9; border-radius: 3px; background: #fbfaf7; color: #292821; font: inherit; }.lookup-control { display: flex; align-items: center; gap: 5px; min-width: 0; }.lookup-control input { flex: 1; min-width: 0; }.lookup-control button { width: 34px; height: 32px; border: 1px solid #aaa696; border-radius: 3px; background: #f4f2e9; font: inherit; cursor: pointer; }.lookup-control.inactive { opacity: .55; }.soft-label { color: #777366; font-size: 11px; white-space: nowrap; }.retest-toggle { display: flex; align-items: center; justify-content: flex-end; gap: 7px; }
.type-selector { display: grid; grid-template-columns: repeat(4,minmax(125px,1fr)); gap: 8px; margin-top: 15px; }.type-selector button { display: grid; gap: 4px; padding: 9px 11px; border: 1px solid #d7d3c6; border-radius: 4px; background: #faf9f5; color: #343228; text-align: left; cursor: pointer; }.type-selector button.active { border-color: #8e7d31; background: #fff8dc; box-shadow: inset 3px 0 #b59a31; }.type-selector span { color: #777366; font-size: 11px; }
.op-hint { margin: 10px 0 0; color: #477147; font-size: 12px; }.op-hint.denied { color: #9a322a; }.feedback { margin: 10px 0 0; padding: 8px 10px; border-radius: 3px; font-size: 13px; }.feedback-info { background: #edf3f6; color: #315163; }.feedback-warning { background: #fff3d6; color: #765a12; }.feedback-error { background: #fde9e7; color: #8c2721; }.feedback-success { background: #e6f2e9; color: #27633a; }
.content-grid { display: grid; grid-template-columns: minmax(280px,.85fr) minmax(430px,1.15fr); gap: 12px; max-width: 1160px; margin: 0 auto 12px; }.content-grid > section { min-width: 0; margin: 0; }.text-button { border: 0; background: transparent; color: #6e5e21; font: inherit; font-size: 12px; text-decoration: underline; cursor: pointer; }.factor-list { display: grid; gap: 9px; }.factor-list label { display: grid; grid-template-columns: 125px 1fr; align-items: center; gap: 8px; color: #565349; font-size: 12px; }.factor-list b { margin-left: 3px; color: #b03931; }.plan-row { display: grid; grid-template-columns: 120px 1fr; align-items: center; gap: 8px; margin-bottom: 10px; color: #565349; font-size: 12px; }.parameter-table { display: grid; border: 1px solid #e2dfd5; border-radius: 3px; overflow: hidden; }.parameter-header,.parameter-table label { display: grid; grid-template-columns: 1fr 1fr 1fr; align-items: center; gap: 8px; padding: 7px 9px; border-bottom: 1px solid #e7e4da; font-size: 12px; }.parameter-header { background: #f2f0e8; color: #555246; font-weight: 650; }.parameter-table label > span:last-child { color: #777366; }.parameter-table input { min-height: 28px; padding-block: 3px; }.form-actions { justify-content: flex-end; margin-top: 12px; }.primary-button,.secondary-button { min-height: 31px; padding: 5px 13px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; font-size: 12px; cursor: pointer; }.primary-button { background: #3b3930; color: white; }.secondary-button { background: white; color: #333; }
.result-banner { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 14px; background: #eef5e9; }.result-banner > div { display: grid; gap: 4px; }.result-banner span { color: #53664e; font-size: 12px; }
.table-scroll { overflow: auto; border: 1px solid #e2dfd5; border-radius: 3px; }.table-scroll table { width: 100%; border-collapse: collapse; font-size: 12px; }.table-scroll th,.table-scroll td { padding: 8px 9px; border-bottom: 1px solid #e7e4da; text-align: left; white-space: nowrap; }.table-scroll th { position: sticky; top: 0; background: #f2f0e8; color: #555246; font-weight: 650; }.code-cell { font-family: Consolas,monospace; font-weight: 650; }.empty-row { padding: 24px !important; color: #777366; text-align: center !important; white-space: normal !important; }.result-chip { padding: 3px 7px; border-radius: 10px; font-size: 11px; }.result-chip.pass { background: #e7eee2; color: #40583d; }.result-chip.flag { background: #f5e2de; color: #8a4338; }
.modal-backdrop { position: fixed; z-index: 1200; inset: 0; display: grid; place-items: center; padding: 20px; background: rgb(20 20 18 / 42%); }.lookup-dialog,.collection-dialog { width: min(640px,100%); max-height: min(88vh,780px); overflow: auto; border: 1px solid #d5d1c3; border-radius: 6px; background: white; box-shadow: 0 18px 54px rgb(0 0 0 / 22%); }.lookup-dialog { padding: 14px; }.lookup-dialog header > div { display: grid; gap: 3px; }.search-input { margin: 12px 0 8px; }.lookup-options { display: grid; gap: 5px; max-height: 55vh; overflow: auto; }.lookup-options button { display: grid; gap: 3px; padding: 9px 10px; border: 1px solid #e2dfd5; border-radius: 3px; background: #faf9f5; text-align: left; cursor: pointer; }.lookup-options button span { color: #777366; font-size: 12px; }.collection-backdrop { z-index: 1250; }.collection-dialog { width: min(1040px,100%); }.collection-header { padding: 16px; border-bottom: 1px solid #e4e1d8; align-items: flex-start; }.collection-header h2 { margin: 3px 0; font-size: 18px; }.collection-header h2 span,.collection-header p { color: #777366; font-size: 12px; font-weight: 400; }.collection-header p { margin: 4px 0 0; }.group-tabs { display: flex; gap: 6px; padding: 9px 14px; background: #f6f4ed; }.group-tabs button { display: grid; gap: 2px; padding: 7px 9px; border: 1px solid #d9d6ca; border-radius: 3px; background: white; cursor: pointer; text-align: left; font: inherit; font-size: 11px; }.group-tabs button.active { border-color: #8e7d31; background: #fff8dc; }.group-tabs span { color: #777366; }.group-tabs b { align-self: center; margin-left: auto; color: #8e6920; font-size: 11px; }.measurement-body { padding: 14px 16px; }.measurement-meta { display: flex; flex-wrap: wrap; gap: 8px 18px; padding: 9px 11px; border: 1px solid #e3e0d6; border-radius: 3px; background: #faf9f5; color: #777366; font-size: 11px; }.measurement-meta strong { margin-left: 4px; color: #343228; }.measurement-toolbar { margin: 14px 0 9px; }.measurement-toolbar h3,.defect-heading h3 { margin: 0 8px 3px 0; font-size: 14px; }.measurement-toolbar > div:first-child { display: grid; gap: 2px; }.measurement-toolbar > div:last-child { display: flex; gap: 6px; }.measurement-table input { min-width: 110px; }.reading-pass { color: #397045; }.reading-fail { color: #a53029; font-weight: 650; }.count-inputs { display: grid; grid-template-columns: repeat(3,1fr); gap: 10px; }.count-inputs label { display: grid; gap: 4px; color: #565349; font-size: 12px; }.defect-heading { margin-top: 14px; }.defect-list { display: grid; gap: 6px; margin-top: 8px; }.defect-row { display: grid; grid-template-columns: 1.2fr .55fr .8fr 1fr; gap: 6px; }.collection-footer { padding: 11px 16px; border-top: 1px solid #e4e1d8; color: #777366; font-size: 11px; }.collection-footer > div { display: flex; gap: 7px; }
@media (max-width: 800px) { .setup-grid,.content-grid { grid-template-columns: 1fr; }.setup-grid > label { text-align: left; }.type-selector { grid-template-columns: repeat(2,minmax(130px,1fr)); }.factor-list label { grid-template-columns: 105px 1fr; }.parameter-row { grid-template-columns: 1fr; }.measurement-toolbar { align-items: flex-start; flex-direction: column; }.collection-footer { align-items: flex-start; flex-direction: column; } }
</style>
