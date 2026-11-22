<script setup lang="ts">
import { computed, ref } from 'vue'

type ProbingRow = {
  id: string; customerNo: string; componentNo: string; laserMark: string; productNo: string; moNo: string; customerLotNo: string
  slotNo: number; programNo: string; programVersion: string; mesLotNo: string; opNo: string; curVersion: number
  totalTestQty: number; totalPassQty: number; totalFailQty: number; yield: number; startTestTime: string; endTestTime: string
  createDate: string; judgeResult: string; bins: Record<number, number>
}
type Activity = { id: number; time: string; message: string }

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const customers = [
  { customerNo: 'DEV-CUST01', customerName: 'DEV 客戶甲' },
  { customerNo: 'DEV-CUST02', customerName: 'DEV 客戶乙' },
]
const operations = ['DEV-CP10', 'DEV-CP20', 'DEV-CP30']
const fixtures: ProbingRow[] = [
  { id: 'PL-001', customerNo: 'DEV-CUST01', componentNo: 'DEV-COMP-A001', laserMark: 'DEV-LASER-A001', productNo: 'DEV-CP-A01', moNo: 'DEVMO-CP-1001', customerLotNo: 'DEV-CUSTLOT-01', slotNo: 1, programNo: 'DEV-PROG-A', programVersion: '3.2', mesLotNo: 'DEVLOT-CP-1001', opNo: 'DEV-CP10', curVersion: 1, totalTestQty: 4120, totalPassQty: 3994, totalFailQty: 126, yield: 96.9417, startTestTime: '2026-11-22 08:10:00', endTestTime: '2026-11-22 08:42:11', createDate: '2026-11-22 08:45:21', judgeResult: 'PASS', bins: { 1: 3880, 2: 114, 3: 126, 101: 42 } },
  { id: 'PL-002', customerNo: 'DEV-CUST01', componentNo: 'DEV-COMP-A002', laserMark: 'DEV-LASER-A002', productNo: 'DEV-CP-A01', moNo: 'DEVMO-CP-1001', customerLotNo: 'DEV-CUSTLOT-01', slotNo: 2, programNo: 'DEV-PROG-A', programVersion: '3.2', mesLotNo: 'DEVLOT-CP-1001', opNo: 'DEV-CP10', curVersion: 0, totalTestQty: 4108, totalPassQty: 3976, totalFailQty: 132, yield: 96.7877, startTestTime: '2026-11-21 14:20:00', endTestTime: '2026-11-21 14:53:10', createDate: '2026-11-21 14:55:02', judgeResult: 'PASS', bins: { 1: 3810, 2: 166, 3: 132, 101: 38 } },
  { id: 'PL-003', customerNo: 'DEV-CUST01', componentNo: 'DEV-COMP-A003', laserMark: 'DEV-LASER-A003', productNo: 'DEV-CP-A01', moNo: 'DEVMO-CP-1001', customerLotNo: 'DEV-CUSTLOT-01', slotNo: 3, programNo: 'DEV-PROG-B', programVersion: '1.8', mesLotNo: 'DEVLOT-CP-1001', opNo: 'DEV-CP20', curVersion: 1, totalTestQty: 4088, totalPassQty: 3951, totalFailQty: 137, yield: 96.6487, startTestTime: '2026-11-22 09:01:00', endTestTime: '2026-11-22 09:33:54', createDate: '2026-11-22 09:37:05', judgeResult: 'PASS', bins: { 1: 3760, 2: 191, 3: 137, 102: 58 } },
  { id: 'PL-004', customerNo: 'DEV-CUST02', componentNo: 'DEV-COMP-B004', laserMark: 'DEV-LASER-B004', productNo: 'DEV-CP-B02', moNo: 'DEVMO-CP-2001', customerLotNo: 'DEV-CUSTLOT-09', slotNo: 4, programNo: 'DEV-PROG-C', programVersion: '2.0', mesLotNo: 'DEVLOT-CP-2009', opNo: 'DEV-CP30', curVersion: 1, totalTestQty: 4012, totalPassQty: 3882, totalFailQty: 130, yield: 96.7597, startTestTime: '2026-11-20 15:02:00', endTestTime: '2026-11-20 15:37:22', createDate: '2026-11-20 15:41:08', judgeResult: 'FAIL', bins: { 1: 3701, 2: 181, 3: 130, 103: 26 } },
  { id: 'PL-005', customerNo: 'DEV-CUST02', componentNo: 'DEV-COMP-B005', laserMark: 'DEV-LASER-B005', productNo: 'DEV-CP-B02', moNo: 'DEVMO-CP-2001', customerLotNo: 'DEV-CUSTLOT-09', slotNo: 5, programNo: 'DEV-PROG-C', programVersion: '2.0', mesLotNo: 'DEVLOT-CP-2009', opNo: 'DEV-CP30', curVersion: 0, totalTestQty: 3998, totalPassQty: 3863, totalFailQty: 135, yield: 96.6233, startTestTime: '2026-11-19 12:10:00', endTestTime: '2026-11-19 12:45:18', createDate: '2026-11-19 12:49:03', judgeResult: 'RETEST', bins: { 1: 3688, 2: 175, 3: 135, 104: 21 } },
]

const customerEnabled = ref(false)
const lotEnabled = ref(false)
const moEnabled = ref(false)
const opEnabled = ref(false)
const componentEnabled = ref(false)
const fromEnabled = ref(false)
const toEnabled = ref(false)
const customerNo = ref('')
const lotNo = ref('')
const moNo = ref('')
const componentNo = ref('')
const opNos = ref<string[]>([])
const fromDate = ref('2026-11-01T00:00')
const toDate = ref('2026-11-23T23:59')
const resultMode = ref<'final' | 'history'>('final')
const customerDialog = ref(false)
const opDialog = ref(false)
const customerChoice = ref('')
const opChoices = ref<string[]>([])
const rows = ref<ProbingRow[]>([])
const activities = ref<Activity[]>([])
const error = ref('')
const notice = ref('至少勾選 Customer No、Lot No、MO No 其中一項，再按 Query。')
const nextId = ref(1)
const binColumns = computed(() => [...new Set(rows.value.flatMap(row => Object.keys(row.bins).map(Number)))].sort((a, b) => a - b))
const opText = computed(() => opNos.value.join(','))
const currentModeLabel = computed(() => resultMode.value === 'final' ? 'Final Result（CurVersion=1）' : 'History（不限制 CurVersion）')
const totalTestQty = computed(() => rows.value.reduce((sum, row) => sum + row.totalTestQty, 0))
const totalPassQty = computed(() => rows.value.reduce((sum, row) => sum + row.totalPassQty, 0))
const averageYield = computed(() => totalTestQty.value ? totalPassQty.value / totalTestQty.value * 100 : 0)

function timeText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }
function addActivity(message: string): void {
  activities.value.unshift({ id: nextId.value++, time: timeText(), message })
  activities.value = activities.value.slice(0, 20)
}
function toggleCriterion(key: 'customer' | 'lot' | 'mo' | 'op' | 'component', checked: boolean): void {
  if (checked) return
  if (key === 'customer') customerNo.value = ''
  if (key === 'lot') lotNo.value = ''
  if (key === 'mo') moNo.value = ''
  if (key === 'op') opNos.value = []
  if (key === 'component') componentNo.value = ''
}
function openCustomerLookup(): void { customerChoice.value = customerNo.value; customerDialog.value = true }
function saveCustomer(): void { customerNo.value = customerChoice.value; if (customerNo.value) customerEnabled.value = true; customerDialog.value = false }
function openOpLookup(): void { opChoices.value = [...opNos.value]; opDialog.value = true }
function saveOpSelection(): void { opNos.value = [...opChoices.value]; opEnabled.value = opNos.value.length > 0; opDialog.value = false }
function contains(value: string, fragment: string): boolean { return value.toUpperCase().includes(fragment.trim().toUpperCase()) }
function parseTime(value: string): number { return value ? new Date(value).getTime() : Number.NaN }
function query(): void {
  error.value = ''
  if (!customerEnabled.value && !lotEnabled.value && !moEnabled.value) {
    rows.value = []; error.value = '至少勾選 Customer No、Lot No 或 MO No 一項。'; return
  }
  const required: Array<[boolean, string, string]> = [
    [customerEnabled.value, customerNo.value, 'Customer No'], [lotEnabled.value, lotNo.value, 'Lot No'],
    [moEnabled.value, moNo.value, 'MO No'], [opEnabled.value, opText.value, 'OP No'], [componentEnabled.value, componentNo.value, 'Component No'],
  ]
  const missing = required.find(([enabled, value]) => enabled && !value.trim())
  if (missing) { rows.value = []; error.value = '已勾選的 ' + missing[2] + ' 必須填入條件。'; return }
  const from = fromEnabled.value ? parseTime(fromDate.value) : Number.NEGATIVE_INFINITY
  const to = toEnabled.value ? parseTime(toDate.value) : Number.POSITIVE_INFINITY
  rows.value = fixtures.filter(row => {
    if (resultMode.value === 'final' && row.curVersion !== 1) return false
    if (customerEnabled.value && row.customerNo !== customerNo.value) return false
    if (lotEnabled.value && !contains(row.mesLotNo, lotNo.value) && !contains(row.customerLotNo, lotNo.value)) return false
    if (moEnabled.value && !contains(row.moNo, moNo.value)) return false
    if (opEnabled.value && !opNos.value.includes(row.opNo)) return false
    if (componentEnabled.value && !contains(row.componentNo, componentNo.value)) return false
    const endTime = parseTime(row.endTestTime.replace(' ', 'T'))
    if (endTime < from || endTime > to) return false
    return true
  }).map(row => ({ ...row, bins: { ...row.bins } }))
  notice.value = '查詢完成：' + rows.value.length + ' 筆 ' + currentModeLabel.value + '；DEV 參數 P_WAFERMAP_EXTTESTINFO=Y，依結果顯示有資料的 Bin 欄位。'
  addActivity('Query · ' + rows.value.length + ' 筆 · ' + currentModeLabel.value)
}
function resetFilters(): void {
  customerEnabled.value = false; lotEnabled.value = false; moEnabled.value = false; opEnabled.value = false; componentEnabled.value = false
  fromEnabled.value = false; toEnabled.value = false; customerNo.value = ''; lotNo.value = ''; moNo.value = ''; componentNo.value = ''; opNos.value = []
  resultMode.value = 'final'; rows.value = []; error.value = ''; notice.value = '查詢條件已重設。'
}
</script>

<template>
  <section v-if="dev" class="probing-log-dev" aria-label="晶圓針測紀錄查詢 DEV">
    <header class="page-head"><div><p class="eyebrow">WIP · CP TEST HISTORY</p><h1>晶圓針測紀錄查詢</h1><p class="subtitle">P_PROBING LOG · WPP04 · 僅 DEV 假資料</p></div><div class="head-actions"><span class="dev-badge">DEV PREVIEW</span><button class="btn light" type="button" @click="resetFilters">重設條件</button><button class="btn dark" type="button" @click="emit('close')">Close (X)</button></div></header>
    <div class="notice" role="status"><span class="dot"></span>{{ notice }}<span class="mode-chip">{{ currentModeLabel }}</span></div>

    <section class="panel filter-panel"><div class="panel-title"><div><p class="eyebrow">FILTER</p><h2>查詢條件</h2></div><span class="subtle">Customer／Lot／MO 至少一項；其餘為附加條件</span></div>
      <form class="filter-grid" @submit.prevent="query">
        <div class="filter-item"><label class="check-label"><input v-model="customerEnabled" type="checkbox" @change="toggleCriterion('customer', ($event.target as HTMLInputElement).checked)">Customer No</label><div class="lookup-control"><input :value="customerNo" readonly placeholder="請選擇客戶"><button class="btn light compact" type="button" @click="openCustomerLookup">...</button></div></div>
        <div class="filter-item"><label class="check-label"><input v-model="lotEnabled" type="checkbox" @change="toggleCriterion('lot', ($event.target as HTMLInputElement).checked)">Lot No</label><input v-model="lotNo" :disabled="!lotEnabled" type="text" placeholder="MES Lot / component lot contains" autocomplete="off"></div>
        <div class="filter-item"><label class="check-label"><input v-model="moEnabled" type="checkbox" @change="toggleCriterion('mo', ($event.target as HTMLInputElement).checked)">MO No</label><input v-model="moNo" :disabled="!moEnabled" type="text" placeholder="MO No contains" autocomplete="off"></div>
        <div class="filter-item"><label class="check-label"><input v-model="opEnabled" type="checkbox" @change="toggleCriterion('op', ($event.target as HTMLInputElement).checked)">OP No</label><div class="lookup-control"><input :value="opText" readonly :disabled="!opEnabled" placeholder="可多選 OP"><button class="btn light compact" type="button" :disabled="!opEnabled" @click="openOpLookup">...</button></div></div>
        <div class="filter-item"><label class="check-label"><input v-model="componentEnabled" type="checkbox" @change="toggleCriterion('component', ($event.target as HTMLInputElement).checked)">Component No</label><input v-model="componentNo" :disabled="!componentEnabled" type="text" placeholder="Component No contains" autocomplete="off"></div>
        <fieldset class="date-range"><legend>EndTestTime</legend><label><input v-model="fromEnabled" type="checkbox">From</label><input v-model="fromDate" :disabled="!fromEnabled" type="datetime-local"><label><input v-model="toEnabled" type="checkbox">To</label><input v-model="toDate" :disabled="!toEnabled" type="datetime-local"></fieldset>
        <fieldset class="mode-choice"><legend>Result Type</legend><label><input v-model="resultMode" type="radio" value="final">Final Result</label><label><input v-model="resultMode" type="radio" value="history">History</label></fieldset>
        <div class="filter-actions"><span>勾選欄位會加入查詢；Lot／MO／Component 使用 contains，OP 使用多選精確匹配。</span><button class="btn primary" type="submit">Query</button></div>
      </form>
      <p class="hint">DEV 依 UI 條件模擬查詢；Core 的 ComponentNo 條件組字串有重複前置條件問題，已列於規格。</p>
    </section>

    <section class="summary-strip"><div><span>查詢模式</span><strong>{{ currentModeLabel }}</strong></div><div><span>Record Count</span><strong>{{ rows.length.toLocaleString() }}</strong></div><div><span>Total Test Qty</span><strong>{{ totalTestQty.toLocaleString() }}</strong></div><div><span>Total Pass Qty</span><strong>{{ totalPassQty.toLocaleString() }}</strong></div><div><span>Weighted Yield</span><strong>{{ averageYield.toFixed(2) }}%</strong></div><div><span>Dynamic Bins</span><strong>{{ binColumns.length }}</strong></div></section>

    <section class="panel results-panel"><div class="panel-title"><div><p class="eyebrow">PROBING LOG</p><h2>針測紀錄</h2></div><span class="subtle">{{ rows.length }} rows · upload time is CreateDate</span></div>
      <div class="table-wrap"><table><thead><tr><th>MES Component No</th><th>P Laser Mark</th><th>Product No</th><th>MO No</th><th>Customer Lot No</th><th>P Wafer ID</th><th>Program No</th><th>Program Version</th><th>MES Lot No</th><th>OP No</th><th>Cur Version</th><th>Total Test Qty</th><th>Total Pass Qty</th><th>Total Fail Qty</th><th>Yield</th><th>Start Test Time</th><th>End Test Time</th><th>Upload Time</th><th>P Judge Result</th><th v-for="bin in binColumns" :key="bin">BIN{{ bin }}</th></tr></thead><tbody><tr v-for="row in rows" :key="row.id"><td>{{ row.componentNo }}</td><td>{{ row.laserMark }}</td><td>{{ row.productNo }}</td><td>{{ row.moNo }}</td><td>{{ row.customerLotNo }}</td><td class="numeric">{{ row.slotNo }}</td><td>{{ row.programNo }}</td><td>{{ row.programVersion }}</td><td>{{ row.mesLotNo }}</td><td>{{ row.opNo }}</td><td>{{ row.curVersion ? 'Yes' : 'No' }}</td><td class="numeric">{{ row.totalTestQty.toLocaleString() }}</td><td class="numeric">{{ row.totalPassQty.toLocaleString() }}</td><td class="numeric">{{ row.totalFailQty.toLocaleString() }}</td><td class="numeric">{{ row.yield.toFixed(4) }}</td><td>{{ row.startTestTime }}</td><td>{{ row.endTestTime }}</td><td>{{ row.createDate }}</td><td>{{ row.judgeResult }}</td><td v-for="bin in binColumns" :key="bin" class="numeric">{{ row.bins[bin]?.toLocaleString() ?? '—' }}</td></tr><tr v-if="!rows.length"><td :colspan="19 + binColumns.length" class="empty">{{ error ? '請修正條件後重新 Query。' : '尚無結果；選擇查詢條件後按 Query。' }}</td></tr></tbody></table></div>
      <div class="result-footer"><span>Dynamic BIN 欄位依 fixtures 有資料的 bin 集合產生；含 101+ extension bin 案例。</span><span class="fixture-tag">read-only report fixture</span></div>
    </section>

    <section class="panel activity-panel"><div class="panel-title"><div><p class="eyebrow">DEV ACTIVITY</p><h2>操作紀錄</h2></div><span class="dev-badge">僅頁面記憶體</span></div><div v-if="activities.length" class="activity-list"><article v-for="entry in activities" :key="entry.id"><time>{{ entry.time }}</time><strong>{{ entry.message }}</strong></article></div><p v-else class="empty">尚無操作紀錄。</p></section>
    <p v-if="error" class="error" role="alert">{{ error }}</p>

    <div v-if="customerDialog" class="backdrop" role="presentation" @click.self="customerDialog = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="DEV Customer lookup"><header><div><p class="eyebrow">DEV LOOKUP</p><h2>選擇 Customer No</h2></div><button class="icon" type="button" @click="customerDialog = false">×</button></header><label v-for="item in customers" :key="item.customerNo" class="option"><input v-model="customerChoice" type="radio" :value="item.customerNo"><strong>{{ item.customerNo }}</strong><span>{{ item.customerName }}</span></label><div class="dialog-actions"><button class="btn light" type="button" @click="customerDialog = false">取消</button><button class="btn primary" type="button" @click="saveCustomer">套用</button></div></section></div>
    <div v-if="opDialog" class="backdrop" role="presentation" @click.self="opDialog = false"><section class="dialog" role="dialog" aria-modal="true" aria-label="DEV OP multi select"><header><div><p class="eyebrow">DEV MULTI LOOKUP</p><h2>多選 OP No</h2></div><button class="icon" type="button" @click="opDialog = false">×</button></header><label v-for="op in operations" :key="op" class="option"><input v-model="opChoices" type="checkbox" :value="op"><strong>{{ op }}</strong></label><div class="dialog-actions"><button class="btn light" type="button" @click="opDialog = false">取消</button><button class="btn primary" type="button" @click="saveOpSelection">套用 {{ opChoices.length }} 項</button></div></section></div>
  </section>
  <section v-else class="probing-log-pending"><p>晶圓針測紀錄查詢正式服務尚未接入。</p><button type="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.probing-log-dev,.probing-log-pending{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:18px 22px;background:#f3f2ed;color:#292922;font:inherit}.page-head,.panel-title,.head-actions{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head{margin-bottom:13px}.page-head h1{margin:2px 0 3px;font-size:23px;font-weight:650}.subtitle{margin:0;color:#716f66;font-size:12px}.eyebrow{margin:0 0 4px;color:#7d7869;font-size:10px;font-weight:700;letter-spacing:.12em}.dev-badge,.mode-chip,.fixture-tag{display:inline-flex;align-items:center;border:1px solid #d6d0bf;border-radius:12px;padding:4px 9px;background:#f7f4e9;color:#70694f;font-size:10px;white-space:nowrap}.btn{min-height:29px;border:1px solid #bdb9ad;border-radius:4px;padding:4px 10px;background:#fff;color:#343229;font:inherit;font-size:12px;cursor:pointer;white-space:nowrap}.btn:hover:not(:disabled){filter:brightness(.97)}.btn:disabled{opacity:.5;cursor:not-allowed}.btn.primary{border-color:#465947;background:#465947;color:#fff}.btn.dark{border-color:#3b3930;background:#3b3930;color:#fff}.btn.light{background:#f8f7f2}.btn.compact{min-height:26px;padding:2px 8px;font-size:11px}.notice{display:flex;align-items:center;gap:8px;min-height:34px;margin-bottom:12px;padding:6px 11px;border:1px solid #d7d2c3;border-radius:5px;background:#fbfaf6;color:#49473e;font-size:11px}.notice .dot{width:8px;height:8px;flex:none;border-radius:50%;background:#6a8c58}.mode-chip{margin-left:auto}.panel{margin:0 0 12px;padding:13px 15px;border:1px solid #d9d6cb;border-radius:6px;background:#fffefa;box-shadow:0 1px 2px #312f2610}.panel-title{margin-bottom:10px}.panel-title h2{margin:0;font-size:15px;font-weight:650}.subtle,.hint{color:#807c70;font-size:10px}.filter-grid{display:grid;grid-template-columns:1fr 1fr 1fr;gap:9px 14px}.filter-item{display:grid;align-content:start;gap:5px;min-width:0;padding:8px;border:1px solid #ece9e1;border-radius:4px;background:#fcfbf7}.check-label{display:flex;align-items:center;gap:6px;color:#514e44;font-size:11px;font-weight:600}.filter-item>input,.lookup-control input,.date-range input[type=datetime-local]{height:29px;min-width:0;box-sizing:border-box;border:1px solid #c9c5b9;border-radius:4px;background:#fff;padding:4px 7px;font:inherit;font-size:11px}.filter-item input:disabled,.date-range input:disabled{background:#efeee9;color:#aaa}.lookup-control{display:flex;gap:5px}.lookup-control input{flex:1;width:100%}.date-range,.mode-choice{display:flex;align-items:center;gap:8px;min-width:0;margin:0;padding:7px 8px;border:1px solid #ece9e1;border-radius:4px;background:#fcfbf7}.date-range legend,.mode-choice legend{padding:0 4px;color:#777266;font-size:9px;font-weight:600}.date-range label,.mode-choice label{display:flex;align-items:center;gap:4px;color:#514e44;font-size:10px;white-space:nowrap}.date-range input[type=datetime-local]{flex:1;width:125px}.filter-actions{display:flex;align-items:center;justify-content:space-between;gap:10px;grid-column:1/-1;padding-top:2px;color:#807c70;font-size:10px}.hint{margin:8px 0 0}.summary-strip{display:grid;grid-template-columns:1.3fr .7fr 1fr 1fr .9fr .8fr;gap:1px;margin-bottom:12px;border:1px solid #d9d6cb;border-radius:6px;overflow:hidden;background:#d9d6cb}.summary-strip>div{display:grid;gap:4px;padding:9px 11px;background:#fbfaf6}.summary-strip span{color:#817c70;font-size:9px;text-transform:uppercase;letter-spacing:.04em}.summary-strip strong{font-size:12px;font-weight:650}.table-wrap{overflow:auto;border:1px solid #e3e0d7;border-radius:4px}table{width:max-content;min-width:100%;border-collapse:collapse;font-size:10px;white-space:nowrap}th,td{padding:6px 8px;border-bottom:1px solid #ece9e1;text-align:left}th{position:sticky;top:0;background:#f4f2eb;color:#5f5b50;font-size:9px;font-weight:700;letter-spacing:.03em}tbody tr:hover{background:#faf9f4}tbody tr:last-child td{border-bottom:0}.numeric{text-align:right;font-variant-numeric:tabular-nums}.empty{padding:20px;text-align:center;color:#908b7d;font-size:11px}.result-footer{display:flex;justify-content:space-between;gap:10px;margin-top:9px;color:#807c70;font-size:10px}.activity-list{display:grid}.activity-list article{display:grid;grid-template-columns:140px 1fr;gap:12px;padding:7px 2px;border-bottom:1px solid #eeeae2;font-size:10px}.activity-list time{color:#817c70}.error{margin:0 0 12px;padding:8px 10px;border:1px solid #ddb0a9;border-radius:4px;background:#fff5f3;color:#963f33;font-size:11px}.backdrop{position:fixed;z-index:1200;inset:0;display:flex;align-items:center;justify-content:center;padding:20px;background:#211f1c75}.dialog{width:min(480px,100%);box-sizing:border-box;border:1px solid #d6d2c6;border-radius:7px;background:#fffefa;padding:17px;box-shadow:0 16px 48px #1115}.dialog header{display:flex;justify-content:space-between;align-items:flex-start;gap:10px}.dialog h2{margin:0;font-size:16px}.icon{width:30px;height:30px;border:1px solid #d1cdc1;border-radius:4px;background:white;color:#524f44;font-size:20px;cursor:pointer}.option{display:flex;align-items:center;gap:10px;margin:7px 0;padding:9px;border:1px solid #e4e0d7;border-radius:4px;font-size:11px}.option span{margin-left:auto;color:#817c70}.dialog-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:13px;padding-top:11px;border-top:1px solid #ece9e1}.probing-log-pending{height:auto}.probing-log-pending p{margin:0 0 10px;color:#777366}.probing-log-pending button{min-height:28px;padding:4px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:#fff;font:inherit;cursor:pointer}
@media(max-width:1000px){.filter-grid{grid-template-columns:1fr 1fr}.summary-strip{grid-template-columns:repeat(3,1fr)}.date-range{flex-wrap:wrap}.date-range input[type=datetime-local]{min-width:180px}.filter-actions{flex-wrap:wrap}}
@media(max-width:650px){.probing-log-dev{padding:12px}.page-head,.panel-title{align-items:flex-start;flex-direction:column}.head-actions{width:100%;justify-content:flex-start;flex-wrap:wrap}.filter-grid{grid-template-columns:1fr}.summary-strip{grid-template-columns:1fr 1fr}.date-range{align-items:flex-start;flex-direction:column}.date-range input[type=datetime-local]{width:100%}.result-footer{flex-direction:column}.activity-list article{grid-template-columns:1fr}}
</style>
