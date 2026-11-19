<script setup lang="ts">
import { computed, ref } from 'vue'

type LotStatus = 0 | 1 | 2 | 3 | 4 | 5
type QueryField = 'areaNo' | 'psNo' | 'opNo'
type GridField = 'componentNo' | 'opNo' | 'brNo' | 'status' | 'baseLotNo' | 'lotNo' | 'moNo'
type SimulatedOutcome = 'success' | 'future-wait' | 'future-hold' | 'future-os' | 'stop'

interface ComponentLotRow {
  componentNo: string
  opNo: string
  brNo: string
  status: LotStatus
  baseLotNo: string
  lotNo: string
  moNo: string
  areaNo: string
  psNo: string
  lotStamp: number
  ruleMethod: 'automatic' | 'manual'
  functionName: string
  nextOpNo: string
  outcome: SimulatedOutcome
}

interface ExecutionRecord {
  lotNo: string
  componentNo: string
  time: string
  message: string
  status: LotStatus
}

interface DevSnapshot {
  version: 1
  rows: ComponentLotRow[]
  records: ExecutionRecord[]
}

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp202-lot-component-execution.v1'
const statusNames: Record<LotStatus, string> = { 0: 'Queue', 1: 'Running', 2: 'Wait', 3: 'Hold', 4: 'Complete', 5: 'OS' }
const employeeNo = ref('')
const componentInput = ref<HTMLInputElement | null>(null)
const scan = ref({ lotNo: '', componentNo: '' })
const filters = ref<Record<QueryField, string>>({ areaNo: '', psNo: '', opNo: '' })
const hasQueried = ref(false)
const selectedComponentNo = ref('')
const activeColumn = ref<GridField>('componentNo')
const lookupField = ref<QueryField | null>(null)
const findOpen = ref(false)
const findText = ref('')
const automaticDialog = ref(false)
const manualDialog = ref(false)
const pendingLotNo = ref('')
const pendingComponentNo = ref('')
const error = ref('')
const message = ref('')
const now = () => new Date().toISOString()

function seedRows(): ComponentLotRow[] {
  const row = (overrides: Partial<ComponentLotRow> & Pick<ComponentLotRow, 'componentNo' | 'lotNo' | 'baseLotNo' | 'moNo' | 'status' | 'areaNo' | 'psNo' | 'opNo'>): ComponentLotRow => ({
    componentNo: overrides.componentNo,
    lotNo: overrides.lotNo,
    baseLotNo: overrides.baseLotNo,
    moNo: overrides.moNo,
    status: overrides.status,
    areaNo: overrides.areaNo,
    psNo: overrides.psNo,
    opNo: overrides.opNo,
    brNo: 'DEV-BR-AUTO',
    lotStamp: 1,
    ruleMethod: 'automatic',
    functionName: '',
    nextOpNo: 'DEV-OP-02',
    outcome: 'success',
    ...overrides,
  })
  return [
    row({ componentNo: 'DEV-COMP-202-001', lotNo: 'DEV-WP202-LOT-001', baseLotNo: 'DEV-WP202-BASE-001', moNo: 'DEV-MO-202-001', status: 0, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-01' }),
    row({ componentNo: 'DEV-COMP-202-002', lotNo: 'DEV-WP202-LOT-001', baseLotNo: 'DEV-WP202-BASE-001', moNo: 'DEV-MO-202-001', status: 0, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-01' }),
    row({ componentNo: 'DEV-COMP-202-003', lotNo: 'DEV-WP202-LOT-002', baseLotNo: 'DEV-WP202-BASE-002', moNo: 'DEV-MO-202-002', status: 1, areaNo: 'DEV-AREA-01', psNo: 'DEV-PS-01', opNo: 'DEV-OP-02', brNo: 'DEV-BR-MANUAL', ruleMethod: 'manual', nextOpNo: 'DEV-OP-03' }),
    row({ componentNo: 'DEV-COMP-202-004', lotNo: 'DEV-WP202-LOT-003', baseLotNo: 'DEV-WP202-BASE-003', moNo: 'DEV-MO-202-003', status: 0, areaNo: 'DEV-AREA-02', psNo: 'DEV-PS-02', opNo: 'DEV-OP-01', outcome: 'future-wait' }),
    row({ componentNo: 'DEV-COMP-202-005', lotNo: 'DEV-WP202-LOT-004', baseLotNo: 'DEV-WP202-BASE-004', moNo: 'DEV-MO-202-004', status: 4, areaNo: 'DEV-AREA-02', psNo: 'DEV-PS-02', opNo: 'DEV-OP-04', brNo: 'PASS', nextOpNo: '' }),
    row({ componentNo: 'DEV-COMP-202-006', lotNo: 'DEV-WP202-LOT-005', baseLotNo: 'DEV-WP202-BASE-005', moNo: 'DEV-MO-202-005', status: 5, areaNo: 'DEV-AREA-03', psNo: 'DEV-PS-03', opNo: 'DEV-OP-03', brNo: 'OS', nextOpNo: '' }),
    row({ componentNo: 'DEV-COMP-202-007', lotNo: 'DEV-WP202-LOT-006', baseLotNo: 'DEV-WP202-BASE-006', moNo: 'DEV-MO-202-006', status: 2, areaNo: 'DEV-AREA-03', psNo: 'DEV-PS-03', opNo: 'DEV-OP-03', brNo: 'DEV-BR-WAIT', nextOpNo: '' }),
    row({ componentNo: 'DEV-COMP-202-008', lotNo: 'DEV-WP202-LOT-007', baseLotNo: 'DEV-WP202-BASE-007', moNo: 'DEV-MO-202-007', status: 3, areaNo: 'DEV-AREA-04', psNo: 'DEV-PS-04', opNo: 'DEV-OP-02', brNo: 'DEV-BR-HOLD', nextOpNo: '' }),
  ]
}

function readSnapshot(): DevSnapshot {
  const initial = { version: 1 as const, rows: seedRows(), records: [] }
  if (!dev.value) return initial
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<DevSnapshot> | null
    if (stored?.version === 1 && Array.isArray(stored.rows) && Array.isArray(stored.records))
      return { version: 1, rows: stored.rows as ComponentLotRow[], records: stored.records as ExecutionRecord[] }
  } catch { /* invalid DEV snapshot uses the seed data */ }
  return initial
}

const initial = readSnapshot()
const rows = ref(initial.rows)
const records = ref(initial.records)
const queryRows = computed(() => {
  if (!hasQueried.value) return []
  return rows.value
    .filter(item => item.status === 0 || item.status === 1 || item.status === 4 || item.status === 5)
    .filter(item => (Object.keys(filters.value) as QueryField[]).every(key => !filters.value[key].trim() || item[key].toUpperCase() === filters.value[key].trim().toUpperCase()))
    .sort((a, b) => a.componentNo.localeCompare(b.componentNo))
})
const pendingRow = computed(() => rows.value.find(item => item.lotNo === pendingLotNo.value && item.componentNo === pendingComponentNo.value) ?? null)
const lookupOptions = computed(() => {
  switch (lookupField.value) {
    case 'areaNo': return ['DEV-AREA-01', 'DEV-AREA-02', 'DEV-AREA-03', 'DEV-AREA-04']
    case 'psNo': return ['DEV-PS-01', 'DEV-PS-02', 'DEV-PS-03', 'DEV-PS-04']
    case 'opNo': return [...new Set(rows.value.filter(item => !filters.value.psNo || item.psNo === filters.value.psNo).map(item => item.opNo))].sort()
    default: return []
  }
})

function saveSnapshot(nextRows = rows.value, nextRecords = records.value): void {
  if (dev.value) localStorage.setItem(storageKey, JSON.stringify({ version: 1, rows: nextRows, records: nextRecords } satisfies DevSnapshot))
}
function query(): void {
  error.value = ''; message.value = ''
  if (!Object.values(filters.value).some(value => value.trim())) {
    hasQueried.value = false
    error.value = '請先輸入 Area No、PS No 或 OP No，再按 Query。'
    return
  }
  hasQueried.value = true
}
function chooseLookup(value: string): void {
  if (lookupField.value) filters.value[lookupField.value] = value
  lookupField.value = null
}
function selectRow(item: ComponentLotRow, column: GridField = 'componentNo'): void {
  selectedComponentNo.value = item.componentNo
  scan.value.componentNo = item.componentNo
  activeColumn.value = column
}
function openFind(): void {
  error.value = ''; message.value = ''
  if (!selectedComponentNo.value) { error.value = '請先選取一筆元件資料，再按 Find。'; return }
  findText.value = ''
  findOpen.value = true
}
function findRow(): void {
  const target = findText.value.trim().toUpperCase()
  if (!target) { error.value = '請輸入要尋找的值。'; return }
  const found = queryRows.value.find(item => String(item[activeColumn.value]).toUpperCase() === target)
  findOpen.value = false
  if (!found) { error.value = `目前清單找不到 ${activeColumn.value} = ${target}。`; return }
  selectRow(found, activeColumn.value)
}
function statusName(value: LotStatus): string { return statusNames[value] }
function focusComponentInput(): void { componentInput.value?.focus() }
function execute(): void {
  error.value = ''; message.value = ''
  const lotText = scan.value.lotNo.trim().toUpperCase()
  const componentText = scan.value.componentNo.trim().toUpperCase()
  if (!employeeNo.value.trim()) { error.value = 'Employee No 不可空白。'; return }
  if (!lotText && !componentText) { error.value = 'Lot No 與 Component No 至少輸入一項。'; return }

  // WinForms gives Lot No priority; Component No is used only when Lot No is blank.
  const matched = lotText
    ? rows.value.find(item => item.lotNo.toUpperCase() === lotText)
    : rows.value.find(item => item.componentNo.toUpperCase() === componentText)
  if (!matched) { error.value = `DEV 假資料找不到 ${lotText ? 'Lot No' : 'Component No'}。`; return }
  pendingLotNo.value = matched.lotNo
  pendingComponentNo.value = matched.componentNo
  selectedComponentNo.value = matched.componentNo

  if (matched.status === 2) { error.value = `Lot ${matched.lotNo} 目前為 Wait，無法由此畫面執行。`; return }
  if (matched.status === 3) { error.value = `Lot ${matched.lotNo} 目前為 Hold，請先解除異常。`; return }
  if (matched.status === 5) { error.value = `Lot ${matched.lotNo} 為 OS，請使用臨時外包功能。`; return }
  if (matched.ruleMethod === 'automatic' || matched.functionName) automaticDialog.value = true
  else manualDialog.value = true
}
function completeExecution(): void {
  const current = pendingRow.value
  if (!current) { error.value = '選取的 DEV 元件資料已不存在。'; automaticDialog.value = false; manualDialog.value = false; return }
  const nextRows = rows.value.map(item => ({ ...item }))
  const parentRows = nextRows.filter(item => item.lotNo === current.lotNo)
  const updated = parentRows[0]
  let outcomeMessage = ''
  let changed = true
  if (updated.outcome === 'future-wait') {
    parentRows.forEach(item => { item.status = 2; item.brNo = 'DEV-BR-WAIT' })
    outcomeMessage = 'DEV 模擬 Future Wait；此 Lot 的元件列已不再出現在 0/1/4/5 查詢清單。'
  } else if (updated.outcome === 'future-hold') {
    parentRows.forEach(item => { item.status = 3; item.brNo = 'DEV-BR-HOLD' })
    outcomeMessage = 'DEV 模擬 Future Hold；此 Lot 的元件列已標示 Hold。'
  } else if (updated.outcome === 'future-os') {
    parentRows.forEach(item => { item.status = 5; item.brNo = 'OS' })
    outcomeMessage = 'DEV 模擬 Future OS；後續需使用臨時外包功能。'
  } else if (updated.outcome === 'stop') {
    changed = false
    outcomeMessage = 'DEV 模擬 StopFlag；後續流程停止，Lot 狀態不變。'
  } else if (updated.status === 0) {
    parentRows.forEach(item => { item.status = 1 })
    outcomeMessage = 'DEV 模擬 Trigger OP；Lot 由 Queue 轉為 Running。'
  } else if (updated.nextOpNo) {
    parentRows.forEach(item => { item.status = 0; item.opNo = updated.nextOpNo; item.brNo = 'DEV-BR-AUTO'; item.ruleMethod = 'automatic'; item.functionName = '' })
    outcomeMessage = `DEV 模擬轉站至 ${updated.nextOpNo}；Lot 回到 Queue。`
  } else {
    parentRows.forEach(item => { item.status = 4; item.brNo = 'PASS' })
    outcomeMessage = 'DEV 模擬完成最後站；Lot 狀態為 Complete。'
  }
  if (changed) parentRows.forEach(item => { item.lotStamp += 1 })
  const nextRecords = [{ lotNo: current.lotNo, componentNo: current.componentNo, time: now(), message: outcomeMessage, status: parentRows[0].status }, ...records.value].slice(0, 15)
  try {
    saveSnapshot(nextRows, nextRecords)
    rows.value = nextRows
    records.value = nextRecords
    message.value = `${current.componentNo} / ${current.lotNo}：${outcomeMessage}`
    hasQueried.value = true
  } catch { error.value = '無法保存 DEV 假資料，請確認瀏覽器允許本機儲存。' }
  automaticDialog.value = false
  manualDialog.value = false
  employeeNo.value = ''
  scan.value = { lotNo: '', componentNo: '' }
}
</script>

<template>
  <section v-if="dev" class="component-execution-page">
    <header class="page-head">
      <div><small>在製品管理 / 生產報工 / WP202</small><h2>生產批執行_元件 <span class="dev-tag">DEV 假資料</span></h2></div>
    </header>

    <fieldset class="mes-group execute-group">
      <legend>Execute</legend>
      <label>Employee No<input v-model.trim="employeeNo" class="required" autocomplete="off" @keydown.enter.prevent="focusComponentInput"></label>
      <label>Component No<input ref="componentInput" v-model.trim="scan.componentNo" autocomplete="off" @blur="scan.componentNo = scan.componentNo.toUpperCase()" @keydown.enter.prevent="execute"></label>
      <label>Lot No<input v-model.trim="scan.lotNo" autocomplete="off" @blur="scan.lotNo = scan.lotNo.toUpperCase()" @keydown.enter.prevent="execute"></label>
    </fieldset>

    <fieldset class="mes-group query-group">
      <legend>Query</legend>
      <label>Area No<span class="lookup"><input v-model.trim="filters.areaNo" @keydown.enter.prevent="query"><button type="button" aria-label="查找 Area No" @click="lookupField='areaNo'">...</button></span></label>
      <label>PS No<span class="lookup"><input v-model.trim="filters.psNo" @keydown.enter.prevent="query"><button type="button" aria-label="查找 PS No" @click="lookupField='psNo'">...</button></span></label>
      <label>OP No<span class="lookup"><input v-model.trim="filters.opNo" @keydown.enter.prevent="query"><button type="button" aria-label="查找 OP No" @click="lookupField='opNo'">...</button></span></label>
      <button class="query-button" type="button" @click="query">Query</button>
    </fieldset>

    <fieldset class="mes-group component-group">
      <legend>Component</legend>
      <div class="grid-wrap"><table>
        <thead><tr><th>ComponentNo</th><th>OPNo</th><th>BRNo</th><th>StatusShow</th><th>BaseLotNo</th><th>LotNo</th><th>MoNo</th></tr></thead>
        <tbody>
          <tr v-for="item in queryRows" :key="item.componentNo" :class="[{selected:item.componentNo===selectedComponentNo}, 'status-'+item.status]" @click="selectRow(item)">
            <td @click.stop="selectRow(item,'componentNo')">{{item.componentNo}}</td>
            <td @click.stop="selectRow(item,'opNo')">{{item.opNo}}</td>
            <td @click.stop="selectRow(item,'brNo')">{{item.brNo}}</td>
            <td @click.stop="selectRow(item,'status')"><span class="status-pill">{{statusName(item.status)}}</span></td>
            <td @click.stop="selectRow(item,'baseLotNo')">{{item.baseLotNo}}</td>
            <td @click.stop="selectRow(item,'lotNo')">{{item.lotNo}}</td>
            <td @click.stop="selectRow(item,'moNo')">{{item.moNo}}</td>
          </tr>
          <tr v-if="!queryRows.length"><td colspan="7" class="empty">{{hasQueried?'查無符合條件的元件／批次':'輸入 Area No、PS No 或 OP No 後按 Query'}}</td></tr>
        </tbody>
      </table></div>
      <div class="component-actions"><button class="primary" type="button" @click="execute">Execute (E)</button><button type="button" @click="openFind">Find (F)</button></div>
    </fieldset>

    <p v-if="error" class="notice error" role="alert">{{error}}</p>
    <p v-if="message" class="notice success" role="status">{{message}}</p>
    <section v-if="records.length" class="history-panel"><header>DEV 執行結果</header><ul><li v-for="(entry,index) in records.slice(0,5)" :key="entry.time+index"><b>{{entry.componentNo}}</b> / {{entry.lotNo}} · {{statusName(entry.status)}} · {{entry.message}} <small>{{new Date(entry.time).toLocaleString()}}</small></li></ul></section>
    <footer class="page-actions"><button type="button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="lookupField" class="overlay" @click.self="lookupField=null"><section class="dialog" role="dialog" aria-modal="true" aria-label="查找條件">
      <h3>選擇 {{lookupField}}</h3><div class="option-list"><button v-for="option in lookupOptions" :key="option" type="button" @click="chooseLookup(option)">{{option}}</button><p v-if="!lookupOptions.length">沒有可選 DEV 假資料</p></div><footer><button type="button" @click="lookupField=null">Close</button></footer>
    </section></div>
    <div v-if="findOpen" class="overlay" @click.self="findOpen=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="Find">
      <h3>Find：{{activeColumn}}</h3><label>請輸入 {{activeColumn}}<input v-model.trim="findText" autofocus @keydown.enter.prevent="findRow"></label><footer><button class="primary" type="button" @click="findRow">Find</button><button type="button" @click="findOpen=false">Cancel</button></footer>
    </section></div>
    <div v-if="automaticDialog&&pendingRow" class="overlay" @click.self="automaticDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="自動規則確認">
      <h3>Automatic Business Rule</h3><p>{{pendingRow.brNo}} 使用自動執行方式，是否繼續？</p><div class="read-fields"><label>Component No<input :value="pendingRow.componentNo" readonly></label><label>Lot No<input :value="pendingRow.lotNo" readonly></label><label>Employee No<input :value="employeeNo" readonly></label></div><footer><button class="primary" type="button" @click="completeExecution">Yes</button><button type="button" @click="automaticDialog=false">No</button></footer>
    </section></div>
    <div v-if="manualDialog&&pendingRow" class="overlay" @click.self="manualDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="人工規則確認">
      <h3>Manual Business Rule <small>DEV 簡化預覽</small></h3><p>WinForms 依目前 Lot 的 BRNo 載入對應規則 DLL／Form；此頁以 DEV 確認視窗呈現共用執行入口。</p><div class="read-fields"><label>Component No<input :value="pendingRow.componentNo" readonly></label><label>Lot No<input :value="pendingRow.lotNo" readonly></label><label>BR No<input :value="pendingRow.brNo" readonly></label><label>Employee No<input :value="employeeNo" readonly></label></div><footer><button class="primary" type="button" @click="completeExecution">DEV 模擬完成</button><button type="button" @click="manualDialog=false">Cancel</button></footer>
    </section></div>
  </section>
  <p v-else role="status">此功能目前僅提供 DEV 假資料預覽。</p>
</template>

<style scoped>
.component-execution-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;padding:12px;color:#292929;background:#fff;font:13px "Microsoft JhengHei",Arial,sans-serif}.page-head{display:flex;align-items:center;min-height:42px;padding:0 9px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-head small{color:#777;font-size:11px}.page-head h2{margin:2px 0 0;font-size:19px;font-weight:500}.dev-tag{margin-left:5px;padding:2px 7px;border-radius:2px;background:#fff0b2;color:#795b00;font-size:11px;font-weight:400;vertical-align:middle}.mes-group{position:relative;display:flex;align-items:center;gap:14px;margin:0;padding:10px;border:1px solid #aaa;min-width:0}.mes-group legend{padding:0 5px;color:#666}.mes-group label{display:flex;align-items:center;gap:7px;white-space:nowrap}.mes-group input{box-sizing:border-box;width:170px;height:26px;padding:3px 6px;border:1px solid #999;border-radius:0;background:#ffffc0;color:#222;font:inherit}.execute-group{min-height:55px;flex:none}.execute-group label:first-of-type input{width:145px}.execute-group label:nth-of-type(2) input{width:190px}.execute-group label:nth-of-type(3) input{width:190px}.required{background:#fff0b2!important}.query-group{min-height:55px;flex:none}.query-group input{width:130px}.lookup{display:inline-flex;align-items:center;gap:3px}.lookup button{min-width:27px;height:26px;padding:0 4px}.query-button{margin-left:auto;min-width:72px}.component-group{flex:1;min-height:220px;display:flex;flex-direction:column;align-items:stretch;padding-top:8px}.grid-wrap{flex:1;min-height:0;overflow:auto;border:1px solid #c5c5c5}.component-group table{width:100%;min-width:760px;border-collapse:collapse;white-space:nowrap;font-size:12px}.component-group th,.component-group td{height:24px;padding:3px 7px;border:1px solid #c9c9c9;text-align:left}.component-group th{position:sticky;top:0;z-index:1;background:#e8e6df;font-weight:500}.component-group tbody tr{cursor:pointer}.component-group tbody tr:hover td{background:#fff7df}.component-group tbody tr.selected td{background:#ffefb2}.status-pill{display:inline-block;min-width:74px;padding:2px 5px;border:1px solid #c5c5c5;background:#f4f3ef;text-align:center}.status-1 .status-pill{background:#e8f2e4}.status-4 .status-pill{background:#eee}.status-5 .status-pill{background:#e9eef5}.empty{text-align:center;color:#777;padding:18px!important}.component-actions{display:flex;justify-content:flex-end;gap:7px;flex:none}.page-actions{display:flex;justify-content:flex-end;flex:none;padding-top:1px}.primary{background:#ffad32!important;border-color:#eb9b24!important;color:#fff!important;font-weight:600}.notice{flex:none;margin:0;padding:7px 9px;border:1px solid #ccc}.notice.error{color:#962c20;background:#fff0ed;border-color:#d59a90}.notice.success{color:#315a25;background:#edf6e8;border-color:#a9c79a}.history-panel{flex:none;border:1px solid #c9c9c9}.history-panel header{padding:5px 8px;border-bottom:1px solid #ddd;background:#f2f0e9}.history-panel ul{display:grid;gap:4px;margin:0;padding:7px 24px;font-size:12px}.history-panel small{margin-left:5px;color:#777}button{min-height:28px;padding:3px 10px;border:1px solid #b8b8b8;border-radius:3px;background:#f6f6f6;color:inherit;font:inherit;cursor:pointer}button:hover{background:#fff4cf}.overlay{position:fixed;inset:0;z-index:160;display:grid;place-items:center;padding:16px;background:#26334266}.dialog{display:grid;gap:12px;width:min(600px,100%);max-height:90vh;overflow:auto;padding:16px;border:1px solid #87939f;background:#fff;box-shadow:0 8px 28px #1113}.dialog h3,.dialog p{margin:0}.dialog label{display:grid;gap:5px}.dialog input{min-height:28px;padding:4px 7px;border:1px solid #aaa;background:#ffffc0;font:inherit}.dialog footer{display:flex;justify-content:flex-end;gap:7px}.option-list{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:6px;max-height:45vh;overflow:auto}.option-list p{grid-column:1/-1;color:#777}.read-fields{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:9px 12px}.read-fields input{background:#ecebe7;color:#666}@media(max-width:760px){.component-execution-page{overflow:auto}.mes-group{align-items:stretch;flex-wrap:wrap}.mes-group label{flex:1;min-width:190px;justify-content:space-between}.mes-group input{flex:1;min-width:90px}.query-button{margin-left:0}.component-group{min-height:310px}.read-fields{grid-template-columns:1fr}}
</style>
