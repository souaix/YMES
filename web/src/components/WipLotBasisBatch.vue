<script setup lang="ts">
import { computed, ref } from 'vue'

type SplitMode = 'count' | 'qty'
interface WorkOrder {
  no: string; ro: string; item: number; product: string; version: string; customer: string
  moType: string; moTypeName: string; state: number; unreleaseQty: number; unit: string; priority: number; lotBaseQty: number
}
interface ProcessRow { id: string; psOrder: number; psNo: string; processNo: string; version: string; hasComponent: boolean; selected: boolean }
interface LotRow { seq: number; baseLotNo: string; inputQty: number }
interface BatchResult { moNo: string; at: string; lots: LotRow[]; releaseQty: number }
interface DevState { version: 1; orders: WorkOrder[]; nextSerial: number; lastResult: BatchResult | null }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp102-lot-basis-batch.v1'
const userNo = computed(() => props.currentUserNo || 'DEV001')
const now = () => new Date().toISOString()
const round4 = (n: number) => Math.round((n + Number.EPSILON) * 10000) / 10000
const fmt = (n: number) => Number.isFinite(n) ? n.toFixed(4).replace(/0+$/, '').replace(/\.$/, '') : ''

function seedOrders(): WorkOrder[] {
  return [
    { no:'DEV-MO-102-001', ro:'DEV-RO-102-001', item:1, product:'DEV-PRD-102-01', version:'01', customer:'DEV-CUST-01', moType:'01', moTypeName:'一般工單', state:3, unreleaseQty:120, unit:'PCS', priority:50, lotBaseQty:25 },
    { no:'DEV-MO-102-002', ro:'DEV-RO-102-002', item:2, product:'DEV-PRD-102-02', version:'02', customer:'DEV-CUST-02', moType:'02', moTypeName:'試產工單', state:3, unreleaseQty:48.5, unit:'PCS', priority:30, lotBaseQty:12.5 },
    { no:'DEV-MO-102-003', ro:'DEV-RO-102-003', item:1, product:'DEV-PRD-102-03', version:'A', customer:'DEV-CUST-03', moType:'01', moTypeName:'一般工單', state:2, unreleaseQty:20, unit:'PCS', priority:80, lotBaseQty:1 },
  ]
}
function readState(): DevState {
  const initial: DevState = { version:1, orders:seedOrders(), nextSerial:1, lastResult:null }
  if (!dev.value) return initial
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<DevState> | null
    if (saved?.version === 1 && Array.isArray(saved.orders) && Number.isInteger(saved.nextSerial) && saved.nextSerial! > 0)
      return { version:1, orders:saved.orders as WorkOrder[], nextSerial:saved.nextSerial!, lastResult:saved.lastResult ?? null }
  } catch { /* invalid DEV snapshot uses seed data */ }
  return initial
}
const saved = readState()
const orders = ref<WorkOrder[]>(saved.orders)
const nextSerial = ref(saved.nextSerial)
const lastResult = ref<BatchResult | null>(saved.lastResult)
const moDialog = ref(false), moSearch = ref('')
const selectedMO = ref<WorkOrder | null>(null)
const splitMode = ref<SplitMode>('qty')
const releaseQty = ref<number | null>(null), lotCount = ref<number | null>(null), lotQty = ref<number | null>(null)
const priority = ref(50), hotLot = ref(false), description = ref('')
const lotRows = ref<LotRow[]>([])
const activeTab = ref<'bom' | 'process'>('bom')
const errorMessage = ref(''), resultMessage = ref('')
const processRows = ref<ProcessRow[]>([]), bomRows = ref<{materialNo:string;opNo:string}[]>([])
const selectedProcessId = ref('')
const processDialog = ref(false), viewDialog = ref(false), tuningDialog = ref(false)
const tuningDraft = ref<ProcessRow | null>(null), processSearch = ref('')

const eligibleOrders = computed(() => orders.value.filter(row => row.state === 3 && row.unreleaseQty > 0)
  .filter(row => row.no.toUpperCase().includes(moSearch.value.trim().toUpperCase())))
const totalLotQty = computed(() => round4(lotRows.value.reduce((sum, row) => sum + (Number(row.inputQty) || 0), 0)))
const processList = computed(() => [...processRows.value].sort((a,b) => a.psOrder-b.psOrder || Number(b.selected)-Number(a.selected)))
const selectedProcess = computed(() => processRows.value.find(row => row.id === selectedProcessId.value) ?? null)
const processChoices = computed<ProcessRow[]>(() => [
  { id:'CHOICE-A', psOrder:20, psNo:'DEV-PS-02', processNo:'DEV-PROC-02A', version:'01', hasComponent:false, selected:true },
  { id:'CHOICE-B', psOrder:20, psNo:'DEV-PS-02', processNo:'DEV-PROC-02B', version:'01', hasComponent:false, selected:true },
  { id:'CHOICE-C', psOrder:30, psNo:'DEV-PS-03', processNo:'DEV-PROC-03', version:'02', hasComponent:false, selected:true },
].filter(row => row.processNo.toUpperCase().includes(processSearch.value.trim().toUpperCase())))

function makeProcesses(mo: WorkOrder): ProcessRow[] {
  return [
    { id:mo.no+'-P10', psOrder:10, psNo:'DEV-PS-01', processNo:'DEV-PROC-01', version:'01', hasComponent:false, selected:true },
    { id:mo.no+'-P20', psOrder:20, psNo:'DEV-PS-02', processNo:'DEV-PROC-02', version:'01', hasComponent:false, selected:true },
  ]
}
function selectMO(row: WorkOrder): void {
  selectedMO.value=row; releaseQty.value=row.unreleaseQty; priority.value=row.priority; lotQty.value=row.lotBaseQty
  lotCount.value=null; splitMode.value='qty'; hotLot.value=false; description.value=''; lotRows.value=[]
  processRows.value=makeProcesses(row); selectedProcessId.value=''
  bomRows.value=[
    {materialNo:'DEV-MTL-'+row.product.slice(-2)+'-01',opNo:'DEV-OP-10'},
    {materialNo:'DEV-MTL-'+row.product.slice(-2)+'-02',opNo:'DEV-OP-20'},
  ]
  moDialog.value=false; moSearch.value=''; errorMessage.value=''; resultMessage.value=''; activeTab.value='bom'
}
function changeSplitMode(value: SplitMode): void { splitMode.value=value; lotRows.value=[]; errorMessage.value='' }
function makeRows(qtys: number[]): LotRow[] {
  return qtys.map((inputQty,index)=>({seq:index+1,baseLotNo:'DEV-WP102-'+String(nextSerial.value+index).padStart(6,'0'),inputQty:round4(inputQty)}))
}
function expandLots(): void {
  errorMessage.value=''; resultMessage.value=''
  const mo=selectedMO.value, qty=Number(releaseQty.value)
  if(!mo){errorMessage.value='請先選擇工單。';return}
  if(!Number.isFinite(qty)||qty<=0){errorMessage.value='Release Qty 必須大於 0。';return}
  if(qty>mo.unreleaseQty){errorMessage.value='Release Qty 不可大於 UnRelease Qty。';return}
  let qtys:number[]=[]
  if(splitMode.value==='count'){
    const count=Number(lotCount.value)
    if(!Number.isInteger(count)||count<=0){errorMessage.value='Lot Count 必須是大於 0 的整數。';return}
    const each=Math.floor(qty/count)
    qtys=Array.from({length:count},()=>each)
    qtys[count-1]=round4(qty-each*(count-1))
  }else{
    const each=Number(lotQty.value)
    if(!Number.isFinite(each)||each<=0){errorMessage.value='Lot Qty 必須大於 0。';return}
    const count=Math.floor(qty/each), remainder=round4(qty-count*each)
    qtys=Array.from({length:count},()=>each)
    if(remainder>0)qtys.push(remainder)
    if(!qtys.length)qtys.push(qty)
  }
  lotRows.value=makeRows(qtys)
}
function validate(): string {
  const mo=selectedMO.value, qty=Number(releaseQty.value)
  if(!mo)return '請先選擇工單。'
  if(!Number.isFinite(qty)||qty<=0)return 'Release Qty 必須大於 0。'
  if(qty>mo.unreleaseQty)return 'Release Qty 不可大於 UnRelease Qty。'
  if(!Number.isInteger(priority.value)||priority.value<0||priority.value>99)return 'Priority 必須介於 0 到 99。'
  if(!processRows.value.length)return 'Process 不可為空。'
  if(processList.value[0]?.hasComponent)return '第一個流程含元件，此功能不支援批次開立。'
  const chosen=processRows.value.filter(row=>row.selected)
  if(!chosen.length)return '至少選取一筆 Process。'
  const byStep=new Map<number,number>()
  for(const row of chosen)byStep.set(row.psOrder,(byStep.get(row.psOrder)||0)+1)
  const duplicated=[...byStep].find(([,count])=>count>1)
  if(duplicated)return '同一個 PSOrder 不可選取多筆流程：'+duplicated[0]
  if(!lotRows.value.length)return '請先按 Expand 產生批次明細。'
  for(const row of lotRows.value){
    if(!row.baseLotNo.trim())return 'BaseLotNo 不可空白。'
    if(!Number.isFinite(Number(row.inputQty))||row.inputQty<=0)return '每筆 Input Qty 必須大於 0：'+row.baseLotNo
  }
  if(Math.abs(round4(totalLotQty.value)-round4(qty))>=0.00005)return '批次 Input Qty 加總必須等於 Release Qty。'
  return ''
}
function submitBatch(): void {
  errorMessage.value=validate(); resultMessage.value=''
  if(errorMessage.value||!dev.value||!selectedMO.value)return
  const mo=selectedMO.value
  const nextOrders=orders.value.map(row=>row.no===mo.no?{...row,unreleaseQty:round4(row.unreleaseQty-Number(releaseQty.value))}:row)
  const result:BatchResult={moNo:mo.no,at:now(),lots:lotRows.value.map(row=>({...row})),releaseQty:round4(Number(releaseQty.value))}
  const next:DevState={version:1,orders:nextOrders,nextSerial:nextSerial.value+lotRows.value.length,lastResult:result}
  try{
    localStorage.setItem(storageKey,JSON.stringify(next))
    orders.value=nextOrders;nextSerial.value=next.nextSerial;lastResult.value=result
    selectedMO.value=nextOrders.find(row=>row.no===mo.no)??null
    releaseQty.value=selectedMO.value?.unreleaseQty??0;lotRows.value=[]
    resultMessage.value='DEV 模擬完成：已更新本機假工單餘量，未呼叫 API 或寫入資料庫。'
  }catch{errorMessage.value='無法保存 DEV 假資料，請確認瀏覽器允許本機儲存。'}
}
function chooseProcess(row: ProcessRow): void {
  const existing=processRows.value.find(item=>item.processNo===row.processNo&&item.version===row.version)
  if(existing)existing.selected=true
  else processRows.value.push({...row,id:'DEV-ADD-'+Date.now()})
  processDialog.value=false;processSearch.value=''
}
function openTuning(): void {
  if(!selectedProcess.value)return
  tuningDraft.value={...selectedProcess.value,processNo:selectedProcess.value.processNo+'-T',version:'01'}
  tuningDialog.value=true
}
function applyTuning(): void {
  const draft=tuningDraft.value,current=selectedProcess.value
  if(!draft||!current)return
  if(processRows.value.some(row=>row.id!==current.id&&row.processNo===draft.processNo&&row.version===draft.version)){errorMessage.value='流程編號與版本已存在。';return}
  const i=processRows.value.findIndex(row=>row.id===current.id)
  if(i>=0)processRows.value[i]={...draft}
  selectedProcessId.value=draft.id;tuningDialog.value=false;tuningDraft.value=null
}
</script>

<template>
  <section v-if="dev" class="batch-page">
    <header class="page-head"><div><small>在製品管理 / 在製品 / WP102</small><h2>生產批開立_批次 <span class="dev-tag">DEV 假資料</span></h2></div><button type="button" @click="emit('close')">關閉</button></header>
    <section class="form-card">
      <h3>MO Release Define</h3>
      <div class="fields">
        <label>MO No<span class="with-button"><input :value="selectedMO?.no||''" readonly placeholder="請選擇工單"><button type="button" @click="moSearch='';moDialog=true">...</button></span></label>
        <label>MOType No<input :value="selectedMO?.moType||''" readonly></label><label>MOType Name<input :value="selectedMO?.moTypeName||''" readonly></label><label>Customer No<input :value="selectedMO?.customer||''" readonly></label>
        <label>RO No<input :value="selectedMO?.ro||''" readonly></label><label>Item No<input :value="selectedMO?.item??''" readonly></label><label>Product No<input :value="selectedMO?.product||''" readonly></label><label>Product Version<input :value="selectedMO?.version||''" readonly></label>
        <label>Release Qty<input v-model.number="releaseQty" type="number" min="0" step="0.0001"></label><label>UnRelease Qty<input :value="fmt(selectedMO?.unreleaseQty??0)" readonly></label><label>Release UnitNo<input :value="selectedMO?.unit||''" readonly></label><label>Priority<input v-model.number="priority" type="number" min="0" max="99" step="1"></label>
        <fieldset class="hot-lot"><legend>Hot Lot</legend><label><input v-model="hotLot" type="radio" :value="true">Yes</label><label><input v-model="hotLot" type="radio" :value="false">No</label></fieldset>
        <label class="description">Description<textarea v-model="description" rows="2"></textarea></label>
      </div>
      <div class="work-area">
        <section class="split-panel">
          <fieldset class="split-controls"><legend>Create Type</legend>
            <div class="split-row"><label class="radio-line"><input type="radio" :checked="splitMode==='count'" @change="changeSplitMode('count')">by Count</label><label class="split-value">Lot Count<input v-model.number="lotCount" type="number" min="1" step="1" :disabled="splitMode!=='count'"></label></div>
            <div class="split-row"><label class="radio-line"><input type="radio" :checked="splitMode==='qty'" @change="changeSplitMode('qty')">by Qty</label><label class="split-value">Lot Qty<input v-model.number="lotQty" type="number" min="0.0001" step="0.0001" :disabled="splitMode!=='qty'"></label></div>
            <div class="split-row"><button class="expand" type="button" @click="expandLots">Expand</button><label class="split-value">Lot Count<input :value="lotRows.length" readonly></label></div>
          </fieldset>
          <div class="grid lot-grid"><table><thead><tr><th>BaseLotNo</th><th>InputQty</th></tr></thead><tbody>
            <tr v-for="row in lotRows" :key="row.seq"><td><input v-model="row.baseLotNo" aria-label="BaseLotNo"></td><td><input v-model.number="row.inputQty" type="number" min="0" step="0.0001" aria-label="InputQty"></td></tr>
            <tr v-if="!lotRows.length"><td colspan="2" class="empty">按 Expand 產生批次</td></tr>
          </tbody></table></div>
          <div class="lot-total">已展開 {{lotRows.length}} 批　數量合計 {{fmt(totalLotQty)}}</div>
        </section>
        <section class="tab-panel">
          <nav class="tabs"><button type="button" :class="{active:activeTab==='bom'}" @click="activeTab='bom'">BOM</button><button type="button" :class="{active:activeTab==='process'}" @click="activeTab='process'">Process</button></nav>
          <div v-if="activeTab==='bom'" class="tab-body"><div class="grid"><table><thead><tr><th>MaterialNo</th><th>OPNo</th></tr></thead><tbody><tr v-for="(row,i) in bomRows" :key="i"><td>{{row.materialNo}}</td><td>{{row.opNo}}</td></tr><tr v-if="!bomRows.length"><td colspan="2" class="empty">請先選擇工單</td></tr></tbody></table></div></div>
          <div v-else class="tab-body">
            <div class="toolbar"><b>Execute Process</b><div><button type="button" @click="processSearch='';processDialog=true">Add</button><button type="button" :disabled="!selectedProcess" @click="viewDialog=true">View</button><button type="button" :disabled="!selectedProcess" @click="openTuning">Tuning</button></div></div>
            <div class="grid process-grid"><table><thead><tr><th>Check</th><th>PSOrder</th><th>PSNo</th><th>ProcessNo</th><th>Version</th><th>HaveComponent</th></tr></thead><tbody>
              <tr v-for="row in processList" :key="row.id" :class="{selected:row.id===selectedProcessId}" @click="selectedProcessId=row.id"><td><input v-model="row.selected" type="checkbox" @click.stop></td><td>{{row.psOrder}}</td><td>{{row.psNo}}</td><td>{{row.processNo}}</td><td>{{row.version}}</td><td>{{row.hasComponent?'Yes':'No'}}</td></tr>
              <tr v-if="!processList.length"><td colspan="6" class="empty">請先選擇工單</td></tr>
            </tbody></table></div>
          </div>
        </section>
      </div>
      <div class="audit-row"><label>Creator<input :value="userNo" readonly></label><label>Create Date<input :value="new Date().toLocaleString()" readonly></label></div>
      <p v-if="errorMessage" class="message error" role="alert">{{errorMessage}}</p><p v-if="resultMessage" class="message success" role="status">{{resultMessage}}</p>
      <div v-if="lastResult" class="last-result">最近一次 DEV 模擬：{{lastResult.moNo}}，{{lastResult.lots.length}} 批，{{fmt(lastResult.releaseQty)}}（{{new Date(lastResult.at).toLocaleString()}}）</div>
      <footer><button class="primary" type="button" @click="submitBatch">Confirm</button><button type="button" @click="emit('close')">Close</button></footer>
    </section>

    <div v-if="moDialog" class="overlay" @click.self="moDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇工單">
      <h3>選擇工單</h3><input v-model="moSearch" placeholder="輸入 MO No 篩選"><div class="grid"><table><thead><tr><th>MO No</th><th>RO No</th><th>Product</th><th>MOState</th><th>UnRelease Qty</th><th></th></tr></thead><tbody>
        <tr v-for="row in eligibleOrders" :key="row.no"><td>{{row.no}}</td><td>{{row.ro}}</td><td>{{row.product}} / {{row.version}}</td><td>{{row.state}}</td><td>{{fmt(row.unreleaseQty)}}</td><td><button type="button" @click="selectMO(row)">選取</button></td></tr>
        <tr v-if="!eligibleOrders.length"><td colspan="6" class="empty">無符合 MOState=3 且 UnReleaseQty&gt;0 的假工單</td></tr>
      </tbody></table></div><footer><button type="button" @click="moDialog=false">Close</button></footer>
    </section></div>

    <div v-if="processDialog" class="overlay" @click.self="processDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="Add Process">
      <h3>Add Process</h3><input v-model="processSearch" placeholder="ProcessNo"><div class="grid"><table><thead><tr><th>PSOrder</th><th>PSNo</th><th>ProcessNo</th><th>Version</th><th></th></tr></thead><tbody>
        <tr v-for="row in processChoices" :key="row.id"><td>{{row.psOrder}}</td><td>{{row.psNo}}</td><td>{{row.processNo}}</td><td>{{row.version}}</td><td><button type="button" @click="chooseProcess(row)">選取</button></td></tr>
      </tbody></table></div><footer><button type="button" @click="processDialog=false">Close</button></footer>
    </section></div>

    <div v-if="viewDialog&&selectedProcess" class="overlay" @click.self="viewDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="View Process">
      <h3>View Process</h3><div class="read-fields"><label>PSOrder<input :value="selectedProcess.psOrder" readonly></label><label>PSNo<input :value="selectedProcess.psNo" readonly></label><label>ProcessNo<input :value="selectedProcess.processNo" readonly></label><label>Version<input :value="selectedProcess.version" readonly></label></div><footer><button type="button" @click="viewDialog=false">Close</button></footer>
    </section></div>

    <div v-if="tuningDialog&&tuningDraft" class="overlay" @click.self="tuningDialog=false"><section class="dialog" role="dialog" aria-modal="true" aria-label="Process Tuning">
      <h3>Process Tuning（DEV 簡化）</h3><div class="read-fields"><label>PSOrder<input :value="tuningDraft.psOrder" readonly></label><label>PSNo<input :value="tuningDraft.psNo" readonly></label><label>New ProcessNo<input v-model="tuningDraft.processNo"></label><label>New Version<input v-model="tuningDraft.version"></label></div><footer><button type="button" @click="applyTuning">Apply</button><button type="button" @click="tuningDialog=false">Close</button></footer>
    </section></div>
  </section>
  <p v-else role="status">此功能目前僅提供 DEV 假資料預覽。</p>
</template>

<style scoped>
.batch-page{display:grid;gap:10px;padding:14px;align-content:start;color:#26313e;min-height:0}.page-head{display:flex;justify-content:space-between;align-items:center;border-bottom:1px solid #c4cbd3;padding-bottom:8px}.page-head small{color:#687582}.page-head h2{margin:2px 0 0;font-size:21px}.dev-tag{font-size:12px;font-weight:400;color:#795b00;background:#fff0b2;padding:3px 8px;border-radius:3px;vertical-align:middle}
.form-card{display:grid;gap:12px;border:1px solid #c4cbd3;background:#fff;padding:12px;min-width:0}.form-card h3{font-size:16px;margin:0;border-bottom:1px solid #e0e3e6;padding-bottom:7px}.fields{display:grid;grid-template-columns:repeat(4,minmax(130px,1fr));gap:9px 12px}.fields label,.audit-row label,.read-fields label{display:grid;gap:4px;font-size:13px}
input,select,textarea,button{font:inherit}input,select,textarea{min-width:0;min-height:28px;border:1px solid #9ba4ad;border-radius:2px;padding:3px 6px;color:#26313e;background:#fff}input[readonly]{background:#f0f0f0;color:#5d6670}.fields input:not([readonly]),.fields textarea,.split-controls input:not([readonly]),.lot-grid input{background:#fff0b2}.with-button{display:flex;gap:4px}.with-button input{flex:1}
button{min-height:29px;padding:3px 11px;border:1px solid #b8c0c8;border-radius:3px;background:#f6f6f6;color:inherit;cursor:pointer}button:disabled{opacity:.5;cursor:not-allowed}button:hover:not(:disabled){background:#fff4cf}fieldset{border:1px solid #c4cbd3;margin:0;padding:8px}legend{padding:0 5px;color:#4d5965}.hot-lot{display:flex;align-items:center;gap:14px}.hot-lot label,.radio-line{display:flex;align-items:center;gap:6px}.hot-lot input,.radio-line input{min-height:unset}.description{grid-column:span 2}.description textarea{resize:vertical}
.work-area{display:grid;grid-template-columns:minmax(300px,.78fr) minmax(430px,1.22fr);gap:12px;min-height:290px}.split-panel,.tab-panel{display:grid;align-content:start;gap:8px;min-width:0}.split-controls{display:grid;gap:7px}.split-row{display:grid;grid-template-columns:minmax(100px,1fr) minmax(150px,1.2fr);gap:10px;align-items:center}.split-value{display:grid;grid-template-columns:75px minmax(0,1fr);align-items:center;gap:6px;font-size:13px}.expand{justify-self:start;background:#fff0b2}
.grid{overflow:auto;border:1px solid #bbc4cd;background:#fff;max-height:42vh;min-width:0}table{border-collapse:collapse;width:100%;font-size:13px}th,td{border:1px solid #c9cfd6;padding:5px 7px;white-space:nowrap;text-align:left}th{background:#e9edf1;font-weight:600}tr.selected td{background:#fff0b2}tbody tr:hover td{background:#fbf5e3}.lot-grid{min-height:160px}.lot-grid input{width:100%;min-width:95px}.empty{text-align:center;color:#78838d;padding:16px}.lot-total{font-size:13px;color:#4e5964}
.tabs{display:flex;border-bottom:1px solid #bfc7cf;gap:3px}.tabs button{border-radius:3px 3px 0 0}.tabs button.active{background:#fff0b2;border-bottom-color:#fff;font-weight:600}.tab-body{display:grid;gap:8px;min-height:240px}.toolbar{display:flex;justify-content:space-between;align-items:center;gap:8px}.toolbar>div{display:flex;gap:6px}.process-grid{max-height:290px}
.audit-row{display:flex;gap:14px;justify-content:flex-end;border-top:1px solid #d5d9dd;padding-top:9px}.audit-row label{grid-template-columns:auto minmax(100px,180px);align-items:center}.audit-row input{width:100%}.message{margin:0;padding:8px 10px;border:1px solid #c4cbd3}.error{color:#962c20;background:#fff0ed;border-color:#d59a90}.success{color:#315a25;background:#edf6e8;border-color:#a9c79a}.last-result{font-size:12px;color:#596571}footer{display:flex;justify-content:flex-end;gap:8px;border-top:1px solid #e0e3e6;padding-top:9px}.primary{background:#ffad32;border-color:#eb9b24;color:#fff;font-weight:600}
.overlay{position:fixed;inset:0;z-index:160;display:grid;place-items:center;background:#26334266;padding:16px}.dialog{width:min(760px,100%);max-height:90vh;overflow:auto;display:grid;gap:12px;background:#fff;border:1px solid #87939f;padding:16px;box-shadow:0 8px 28px #1113}.dialog h3{margin:0}.read-fields{display:grid;grid-template-columns:repeat(2,minmax(130px,1fr));gap:9px 12px}
@media(max-width:1050px){.fields{grid-template-columns:repeat(2,minmax(130px,1fr))}.work-area{grid-template-columns:1fr}.description{grid-column:1/-1}}@media(max-width:650px){.fields{grid-template-columns:1fr}.audit-row{flex-direction:column}.audit-row label{grid-template-columns:110px 1fr}.toolbar{align-items:flex-start;flex-direction:column}}
</style>
