<script setup lang="ts">
import { computed, ref } from 'vue'

interface QcItem { qcItemNo: string; name: string; lower: number; upper: number; unit: string; sampleQty: number; samplePlan: string }
interface Reading { serialNo: number; value: string; memo: string }
interface Entry { id: string; spcSerial: string; lotNo: string; qcItemNo: string; qcItemName: string; sampleQty: number; values: string; result: string; revision: number; savedAt: string; readings: Reading[] }

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const operations = [
  { opNo: 'OP-ASSY-20', name: '成品組裝檢驗', authorized: true },
  { opNo: 'OP-TEST-30', name: '功能測試', authorized: true },
  { opNo: 'OP-REVIEW-90', name: '品質覆核（無 SPC 權限）', authorized: false },
]
const qcItems: QcItem[] = [
  { qcItemNo: 'QC-V-THK-001', name: '產品厚度', lower: 0.95, upper: 1.05, unit: 'mm', sampleQty: 5, samplePlan: 'PLAN-V-5' },
  { qcItemNo: 'QC-V-WIDTH-011', name: '產品寬度', lower: 9.8, upper: 10.2, unit: 'mm', sampleQty: 5, samplePlan: 'PLAN-V-5' },
  { qcItemNo: 'QC-V-POWER-021', name: '輸出功率', lower: 17.5, upper: 18.5, unit: 'W', sampleQty: 3, samplePlan: 'PLAN-V-3' },
]
const plans = [
  { planNo: 'PLAN-V-5', sampleQty: 5, label: '計量連續抽樣 5 點' },
  { planNo: 'PLAN-V-3', sampleQty: 3, label: '計量連續抽樣 3 點' },
]
const query = typeof window === 'undefined' ? new URLSearchParams() : new URLSearchParams(window.location.search)
const selectedOperationNo = ref(operations.some((row) => row.opNo === query.get('OPNo')) ? query.get('OPNo')! : 'OP-ASSY-20')
const selectedQcItemNo = ref(qcItems.some((row) => row.qcItemNo === query.get('QCItemNo')) ? query.get('QCItemNo')! : qcItems[0].qcItemNo)
const lotNo = ref(query.get('LotNo') || 'DEV-LOT-261123-01')
const senderName = ref(query.get('SenderName') || '')
const populationQty = ref('480')
const sampleQty = ref('5')
const samplePlanNo = ref('PLAN-V-5')
const factorValues = ref({ shift: 'DAY', equipment: 'DEV-EQP-101', operator: props.currentUserNo?.trim() || 'DEV-USER' })
const readings = ref<Reading[]>([])
const entries = ref<Entry[]>([])
const pasteText = ref('0.998,1.002,1.004,0.997,1.001')
const prepared = ref(false)
const editEntryId = ref('')
const originalReadings = ref<Reading[] | null>(null)
const message = ref('')
const messageKind = ref<'info' | 'success' | 'warning' | 'error'>('info')
const selectedItem = computed(() => qcItems.find((row) => row.qcItemNo === selectedQcItemNo.value) || qcItems[0])
const selectedOperation = computed(() => operations.find((row) => row.opNo === selectedOperationNo.value))
const selectedPlan = computed(() => plans.find((row) => row.planNo === samplePlanNo.value))
const filledCount = computed(() => readings.value.filter((row) => row.value.trim() !== '').length)
const nextSerial = computed(() => readings.value.find((row) => row.value.trim() === '')?.serialNo || 0)

function setMessage(text: string, kind: typeof messageKind.value = 'info'): void {
  message.value = text
  messageKind.value = kind
}

function changeItem(): void {
  const item = selectedItem.value
  samplePlanNo.value = item.samplePlan
  sampleQty.value = String(item.sampleQty)
  prepared.value = false
  editEntryId.value = ''
  originalReadings.value = null
  readings.value = []
  setMessage('品管項目已切換；請重新確認抽樣參數。')
}

function invalidatePrepared(): void {
  if (!prepared.value) return
  prepared.value = false
  setMessage('收集條件已變更；請重新確認抽樣參數。', 'warning')
}

function changeSamplePlan(): void {
  sampleQty.value = String(selectedPlan.value?.sampleQty || sampleQty.value)
  invalidatePrepared()
}

function prepareCollection(): void {
  if (editEntryId.value) return setMessage('請先 ConfirmEdit 或 Reverse，再開始新的連續收集。', 'warning')
  prepared.value = false
  const op = selectedOperation.value
  const population = Number(populationQty.value)
  const size = Number(sampleQty.value)
  if (!op || !op.authorized) return setMessage('DEV 使用者沒有此作業站的 SPC 權限。', 'error')
  if (!lotNo.value.trim()) return setMessage('請輸入 Lot No。', 'error')
  if (!Number.isInteger(population) || population <= 0 || !Number.isInteger(size) || size <= 0 || size > population) return setMessage('母體數與抽樣數須為正整數，且抽樣數不可大於母體數。', 'error')
  if (!selectedPlan.value) return setMessage('請選擇計量抽樣計畫。', 'error')
  if (Object.values(factorValues.value).some((value) => !value.trim())) return setMessage('班別、設備及操作人員都是必要收集要因。', 'error')
  readings.value = Array.from({ length: size }, (_, index) => readings.value[index] || { serialNo: index + 1, value: '', memo: '' })
  prepared.value = true
  editEntryId.value = ''
  originalReadings.value = null
  setMessage('參數已載入 DEV；可逐筆輸入，或使用假設備／貼上資料模擬連續收集。')
}

function statusFor(value: string): string {
  if (value.trim() === '' || !Number.isFinite(Number(value))) return '待輸入'
  return Number(value) < selectedItem.value.lower || Number(value) > selectedItem.value.upper ? '超出規格' : '規格內'
}

function captureSimulatedReading(): void {
  if (!prepared.value || nextSerial.value === 0) return setMessage('請先確認參數，且尚有空白抽樣位置。', 'warning')
  const item = selectedItem.value
  const offset = [0.01, -0.012, 0.004, 0, -0.006][(nextSerial.value - 1) % 5]
  const value = ((item.lower + item.upper) / 2 + (item.upper - item.lower) * offset).toFixed(3)
  const target = readings.value.find((row) => row.serialNo === nextSerial.value)
  if (target) target.value = value
  setMessage('已模擬填入第 ' + nextSerial.value + ' 點；沒有連線設備或 RS232。')
}

function importPastedValues(): void {
  if (!prepared.value) return setMessage('請先確認抽樣參數。', 'warning')
  const values = pasteText.value.split(/[\s,;\t]+/).map((value) => value.trim()).filter(Boolean)
  if (!values.length || values.some((value) => !Number.isFinite(Number(value)))) return setMessage('貼上資料須為以逗號、分號或換行分隔的數字。', 'error')
  const blanks = readings.value.filter((row) => row.value.trim() === '')
  if (values.length > blanks.length) return setMessage('貼上值超過目前剩餘抽樣位置。', 'error')
  values.forEach((value, index) => { blanks[index].value = value; blanks[index].memo = 'DEV 貼上匯入' })
  setMessage('已將 ' + values.length + ' 個值載入本頁假資料；不會讀取實際檔案或寫入 MES。')
}

function saveCollection(): void {
  if (!prepared.value) return setMessage('請先確認抽樣參數。', 'warning')
  if (!selectedOperation.value?.authorized) {
    prepared.value = false
    return setMessage('DEV 使用者沒有此作業站的 SPC 權限。', 'error')
  }
  if (readings.value.length !== Number(sampleQty.value) || readings.value.some((row) => !row.value.trim() || !Number.isFinite(Number(row.value)))) return setMessage('每一個抽樣位置都必須輸入有效數值。', 'error')
  const item = selectedItem.value
  const outside = readings.value.filter((row) => statusFor(row.value) === '超出規格').length
  const result = outside ? '超出規格 ' + outside + ' 點' : '規格內'
  const values = readings.value.map((row) => row.value).join(', ')
  const savedAt = new Date().toLocaleString('zh-TW', { hour12: false })
  if (editEntryId.value) {
    entries.value = entries.value.map((row) => row.id === editEntryId.value ? { ...row, values, revision: row.revision + 1, savedAt, readings: readings.value.map((reading) => ({ ...reading })) } : row)
    editEntryId.value = ''
    originalReadings.value = null
    prepared.value = false
    setMessage('修改已在 DEV 歷程中確認；正式流程尚須核實 UpdVar_LotData 與稽核契約。', 'success')
    return
  }
  const token = Date.now().toString(36).toUpperCase()
  entries.value.unshift({ id: token, spcSerial: 'DEV-SPC-' + token, lotNo: lotNo.value.trim(), qcItemNo: item.qcItemNo, qcItemName: item.name, sampleQty: readings.value.length, values, result, revision: 0, savedAt, readings: readings.value.map((reading) => ({ ...reading })) })
  prepared.value = false
  setMessage('本批連續量測已存入 DEV 記憶體；沒有呼叫 SPC Service、WIP 或資料庫。', outside ? 'warning' : 'success')
}

function editEntry(entry: Entry): void {
  selectedQcItemNo.value = entry.qcItemNo
  lotNo.value = entry.lotNo
  sampleQty.value = String(entry.sampleQty)
  samplePlanNo.value = entry.sampleQty === 3 ? 'PLAN-V-3' : 'PLAN-V-5'
  readings.value = entry.readings.map((row) => ({ ...row }))
  originalReadings.value = entry.readings.map((row) => ({ ...row }))
  editEntryId.value = entry.id
  prepared.value = true
  setMessage('修改模式：來源允許調整既有檢測值，但不重新判定原檢驗批結果。', 'warning')
}

function reverseEdit(): void {
  if (!originalReadings.value) return
  readings.value = originalReadings.value.map((row) => ({ ...row }))
  originalReadings.value = null
  editEntryId.value = ''
  prepared.value = false
  setMessage('已撤銷本次修改並還原 DEV 原值。')
}

function clearValues(): void {
  if (editEntryId.value) return reverseEdit()
  readings.value = readings.value.map((row) => ({ ...row, value: '', memo: '' }))
  setMessage('已清空尚未儲存的輸入值。')
}
</script>

<template>
  <main class="spc-continue-page" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading">
      <div><div class="eyebrow">SPC · SC102 · DEV fixtures</div><h1>SPC 量測數據收集 <span>計量連續</span></h1><p>連續逐點輸入、檢視規格、確認儲存及修改既有量測。</p></div>
      <button type="button" class="icon-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>不連 MES Service、資料庫、RS232、儀器、ERP 或 ERF；資料只保存在目前頁面。</span></div>

    <section class="setup-card">
      <div class="section-heading"><div><h2>收集條件</h2><span>WinForms：OP／QCItem → Lot／要因 → 計量抽樣參數</span></div><span class="entry-context">{{ senderName ? '入口情境：' + senderName : '獨立收集' }}</span></div>
      <div class="setup-grid">
        <label for="sc102-op">作業站 OP</label><select id="sc102-op" v-model="selectedOperationNo" :disabled="Boolean(editEntryId)" @change="invalidatePrepared"><option v-for="op in operations" :key="op.opNo" :value="op.opNo">{{ op.opNo }} · {{ op.name }}</option></select>
        <label for="sc102-item">品管項目</label><select id="sc102-item" v-model="selectedQcItemNo" :disabled="Boolean(editEntryId)" @change="changeItem"><option v-for="item in qcItems" :key="item.qcItemNo" :value="item.qcItemNo">{{ item.qcItemNo }} · {{ item.name }}</option></select>
        <label for="sc102-lot">Lot No</label><input id="sc102-lot" v-model="lotNo" maxlength="50" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" />
        <label for="sc102-population">母體數量</label><input id="sc102-population" v-model="populationQty" inputmode="numeric" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" />
      </div>
      <div class="factor-grid">
        <label>班別 <input v-model="factorValues.shift" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" /></label><label>檢測設備 <input v-model="factorValues.equipment" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" /></label><label>操作人員 <input v-model="factorValues.operator" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" /></label>
      </div>
      <div class="plan-grid">
        <label>Sampling Plan <select v-model="samplePlanNo" :disabled="Boolean(editEntryId)" @change="changeSamplePlan"><option v-for="plan in plans" :key="plan.planNo" :value="plan.planNo">{{ plan.planNo }} · {{ plan.label }}</option></select></label>
        <label>抽樣數 SampleQty <input v-model="sampleQty" inputmode="numeric" :disabled="Boolean(editEntryId)" @change="invalidatePrepared" /></label>
        <div class="limit-chip"><span>DEV 規格界線</span><strong>{{ selectedItem.lower }} – {{ selectedItem.upper }} {{ selectedItem.unit }}</strong></div>
        <button type="button" class="primary-button" :disabled="Boolean(editEntryId)" @click="prepareCollection">確認參數</button>
      </div>
      <p v-if="selectedOperation && !selectedOperation.authorized" class="feedback feedback-error" role="status">此 DEV 作業站沒有 SPC 權限。</p>
      <p v-if="message" class="feedback" :class="'feedback-' + messageKind" role="status">{{ message }}</p>
    </section>

    <section class="measurement-card">
      <div class="section-heading"><div><h2>計量連續輸入</h2><span>已輸入 {{ filledCount }} / {{ readings.length }} 點<span v-if="nextSerial"> · 下一點 {{ nextSerial }}</span></span></div><span class="count-badge">{{ editEntryId ? '修改模式' : (prepared ? '輸入中' : '待確認') }}</span></div>
      <div class="measurement-actions">
        <button type="button" class="secondary-button" :disabled="!prepared || !nextSerial" @click="captureSimulatedReading">模擬設備填下一點</button>
        <button type="button" class="secondary-button" :disabled="!prepared" @click="importPastedValues">匯入貼上的數值</button>
        <button type="button" class="primary-button" :disabled="!prepared" @click="saveCollection">{{ editEntryId ? 'ConfirmEdit 確認修改' : 'Save 儲存本批' }}</button>
        <button v-if="editEntryId" type="button" class="secondary-button" @click="reverseEdit">Reverse 撤銷修改</button>
        <button v-else type="button" class="secondary-button" :disabled="!readings.length" @click="clearValues">清空未儲存值</button>
      </div>
      <label class="paste-box">貼上 CSV／TXT 數值（逗號、分號或換行分隔）<textarea v-model="pasteText" rows="2" :disabled="!prepared" /></label>
      <div class="table-scroll"><table><thead><tr><th>Serial No</th><th>量測值</th><th>單位</th><th>規格判定</th><th>備註</th></tr></thead><tbody>
        <tr v-for="row in readings" :key="row.serialNo"><td>{{ row.serialNo }}</td><td><input v-model="row.value" inputmode="decimal" :disabled="!prepared" :aria-label="'第 ' + row.serialNo + ' 點量測值'" /></td><td>{{ selectedItem.unit }}</td><td><span class="status-chip" :class="statusFor(row.value) === '規格內' ? 'pass' : statusFor(row.value) === '超出規格' ? 'fail' : ''">{{ statusFor(row.value) }}</span></td><td><input v-model="row.memo" :disabled="!prepared" aria-label="量測備註" /></td></tr>
        <tr v-if="!readings.length"><td colspan="5" class="empty-row">確認抽樣參數後，逐點輸入或載入 DEV 假值。</td></tr>
      </tbody></table></div>
    </section>

    <section class="history-card">
      <div class="section-heading"><div><h2>本頁連續量測歷程</h2><span>工作階段記憶體資料；重新載入後清除</span></div><span class="count-badge">{{ entries.length }} 批</span></div>
      <div class="table-scroll"><table><thead><tr><th>SPC Serial</th><th>Lot No</th><th>QC Item</th><th>點數</th><th>連續讀值</th><th>判定</th><th>版次</th><th>時間</th><th>操作</th></tr></thead><tbody>
        <tr v-for="entry in entries" :key="entry.id"><td class="code-cell">{{ entry.spcSerial }}</td><td>{{ entry.lotNo }}</td><td>{{ entry.qcItemNo }} · {{ entry.qcItemName }}</td><td>{{ entry.sampleQty }}</td><td>{{ entry.values }}</td><td><span class="status-chip" :class="entry.result === '規格內' ? 'pass' : 'fail'">{{ entry.result }}</span></td><td>{{ entry.revision }}</td><td>{{ entry.savedAt }}</td><td><button type="button" class="text-button" :disabled="Boolean(editEntryId)" @click="editEntry(entry)">Edit</button></td></tr>
        <tr v-if="!entries.length"><td colspan="9" class="empty-row">尚無 DEV 量測歷程。</td></tr>
      </tbody></table></div>
    </section>
  </main>
</template>

<style scoped>
.spc-continue-page { display: grid; gap: 12px; min-height: 100%; padding: 14px; background: #f7f5ee; color: #302f29; font: inherit; box-sizing: border-box; }
.page-heading,.section-heading { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.page-heading h1 { margin: 3px 0; font-size: 21px; }.page-heading h1 span { color: #8b7625; font-size: 14px; }.page-heading p,.section-heading span,.entry-context { margin: 0; color: #777366; font-size: 12px; }
.eyebrow { color: #8b7625; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; }
.icon-button { width: 30px; height: 30px; border: 1px solid #d5d1c3; border-radius: 4px; background: white; font-size: 20px; cursor: pointer; }
.fixture-banner { display: flex; gap: 10px; padding: 9px 11px; border: 1px solid #e4d9a7; border-radius: 4px; background: #fff9dc; color: #625521; font-size: 12px; }.fixture-banner strong { white-space: nowrap; }
.setup-card,.measurement-card,.history-card { padding: 12px; border: 1px solid #dfdcd2; border-radius: 4px; background: white; }.section-heading { margin-bottom: 10px; }.section-heading h2 { margin: 0 0 3px; font-size: 15px; }.count-badge { padding: 4px 8px; border-radius: 12px; background: #f1eee4; color: #635d4b; white-space: nowrap; }
.setup-grid { display: grid; grid-template-columns: 100px minmax(130px,1fr) 100px minmax(130px,1fr); align-items: center; gap: 8px; font-size: 12px; }.setup-grid > label,.factor-grid > label,.plan-grid > label { color: #565349; }
input,select,textarea { width: 100%; min-width: 0; min-height: 30px; padding: 4px 7px; border: 1px solid #d8d5ca; border-radius: 3px; background: white; color: #302f29; font: inherit; font-size: 12px; box-sizing: border-box; }input:disabled,textarea:disabled { background: #f5f3ed; color: #8a877d; }
.factor-grid,.plan-grid { display: grid; grid-template-columns: repeat(3,minmax(0,1fr)); gap: 10px; margin-top: 10px; }.factor-grid label,.plan-grid label { display: grid; gap: 4px; font-size: 11px; }
.plan-grid { grid-template-columns: 1.2fr .7fr 1fr auto; align-items: end; }.limit-chip { display: grid; gap: 3px; padding: 6px 9px; border: 1px solid #e7e3d7; border-radius: 3px; background: #faf9f5; color: #777366; font-size: 10px; }.limit-chip strong { color: #343228; font-size: 12px; }
.primary-button,.secondary-button { min-height: 30px; padding: 5px 10px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; font-size: 12px; cursor: pointer; }.primary-button { background: #3b3930; color: white; }.secondary-button { background: white; color: #333; }.primary-button:disabled,.secondary-button:disabled { opacity: .45; cursor: not-allowed; }.feedback { margin: 9px 0 0; color: #5d5a50; font-size: 12px; }.feedback-success { color: #397045; }.feedback-warning { color: #8a6920; }.feedback-error { color: #a53029; }
.measurement-actions { display: flex; flex-wrap: wrap; gap: 7px; margin-bottom: 9px; }.paste-box { display: grid; gap: 4px; margin-bottom: 9px; color: #777366; font-size: 11px; }
.table-scroll { overflow: auto; border: 1px solid #e2dfd5; border-radius: 3px; }.table-scroll table { width: 100%; border-collapse: collapse; font-size: 12px; }.table-scroll th,.table-scroll td { padding: 6px 8px; border-bottom: 1px solid #e7e4da; text-align: left; white-space: nowrap; }.table-scroll th { position: sticky; top: 0; background: #f2f0e8; color: #555246; font-weight: 650; }.table-scroll tbody tr:last-child td { border-bottom: 0; }.table-scroll td input { min-width: 100px; }.status-chip { padding: 3px 7px; border-radius: 10px; background: #f1eee4; color: #777366; font-size: 11px; }.status-chip.pass { background: #e7eee2; color: #40583d; }.status-chip.fail { background: #f5e2de; color: #8a4338; }.code-cell { font-family: Consolas,monospace; font-weight: 650; }.empty-row { padding: 20px !important; color: #777366; text-align: center !important; white-space: normal !important; }.text-button { border: 0; background: transparent; color: #6e5e21; font: inherit; font-size: 12px; text-decoration: underline; cursor: pointer; }.text-button:disabled { color: #aaa79c; cursor: not-allowed; }
@media (max-width: 760px) { .setup-grid { grid-template-columns: 1fr; }.setup-grid > label { margin-bottom: -5px; }.factor-grid,.plan-grid { grid-template-columns: 1fr 1fr; }.page-heading { align-items: flex-start; }.fixture-banner { align-items: flex-start; flex-direction: column; gap: 3px; } }
</style>
