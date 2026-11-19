<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
type IssueState = -1 | 0 | 1 | 2
type Mode = 'add' | 'edit' | 'copy'
type Tab = 'process' | 'component' | 'property'
interface Mo { no:string; ro:string; item:number; product:string; version:string; customer:string; moType:string; moTypeName:string; state:number; unrelease:number }
interface Process { order:number; psNo:string; processNo:string; version:string; hasComponent:boolean; selected:boolean }
interface Component { no:string; product:string; qty:number; selected:boolean }
interface Property { no:string; value:string; type:'文字'|'數字'|'日期'; system:boolean }
interface Lot {
  baseLotNo:string; moNo:string; roNo:string; itemNo:number; productNo:string; productVersion:string; customerNo:string
  customerLotNo:string; returnNo:string; moTypeNo:string; moTypeName:string; engNo:string; engVersion:string; inputQty:number; unitNo:string; hotLot:boolean; priority:number
  description:string; issueState:IssueState; lotState:0|1; creator:string; createDate:string; reviser:string; reviseDate:string
  processes:Process[]; components:Component[]; skippedOps:string[]; properties:Property[]
}
const props=defineProps<{currentUserNo?:string;developmentPreview?:boolean}>()
const emit=defineEmits<{close:[]}>()
const dev=computed(()=>import.meta.env.DEV&&props.developmentPreview===true)
const {confirmationMessage,requestConfirmation,resolveConfirmation}=useActionConfirmation()
const storageKey='ymes.dev.wp101-lot-basis.v1'
const user=()=>props.currentUserNo||'DEV001'
const now=()=>new Date().toISOString()
const clone=<T,>(v:T):T=>JSON.parse(JSON.stringify(v)) as T
const stateOptions:{value:IssueState;label:string}[]=[{value:0,label:'未簽核'},{value:1,label:'簽核中'},{value:2,label:'已簽核'},{value:-1,label:'不使用'}]
const mos=ref<Mo[]>([
 {no:'DEV-MO-001',ro:'DEV-RO-001',item:1,product:'DEV-PRD-1001',version:'01',customer:'DEV-C01',moType:'DEV-MT-01',moTypeName:'一般工單',state:3,unrelease:100},
 {no:'DEV-MO-002',ro:'DEV-RO-002',item:1,product:'DEV-PRD-1002',version:'02',customer:'DEV-C02',moType:'DEV-MT-02',moTypeName:'樣品工單',state:6,unrelease:0},
])
function makeLot(no:string,mo:Mo,issue:IssueState=0):Lot {
 return {baseLotNo:no,moNo:mo.no,roNo:mo.ro,itemNo:mo.item,productNo:mo.product,productVersion:mo.version,customerNo:mo.customer,
  customerLotNo:'',returnNo:'N/A',moTypeNo:mo.moType,moTypeName:mo.moTypeName,engNo:'N/A',engVersion:'N/A',inputQty:1,unitNo:'PCS',hotLot:false,priority:99,description:'',
  issueState:issue,lotState:0,creator:user(),createDate:now(),reviser:'',reviseDate:'',
  processes:[{order:10,psNo:'DEV-PS-01',processNo:'DEV-PROC-01',version:'01',hasComponent:true,selected:true}],
  components:[{no:'DEV-COMP-001',product:mo.product,qty:1,selected:true}],skippedOps:[],
  properties:[{no:'DEV-LOT-TAG',value:'',type:'文字',system:false}]}
}
function loadLots():Lot[] {
 if(dev.value)try{const value=localStorage.getItem(storageKey);if(value)return JSON.parse(value) as Lot[]}catch{/* restore sample data */}
 return [makeLot('DEV-LOT-001',mos.value[0]!),makeLot('DEV-LOT-002',mos.value[0]!,1)]
}
const lots=ref(loadLots()), issueState=ref<IssueState>(0), moNo=ref(''), moText=ref(''), moSearch=ref('')
const moDialog=ref(false), searchText=ref(''), searchField=ref<'baseLotNo'|'moNo'>('baseLotNo'), appliedSearch=ref('')
const selectedNo=ref(''), mode=ref<Mode|null>(null), draft=ref<Lot|null>(null), tab=ref<Tab>('process')
const formError=ref(''), pageError=ref('')
const eligibleMOs=computed(()=>mos.value.filter(m=>[3,6].includes(m.state)&&(m.unrelease>0||lots.value.some(l=>l.moNo===m.no&&l.lotState===0))&&m.no.includes(moSearch.value.trim().toUpperCase())))
const rows=computed(()=>lots.value.filter(l=>l.issueState===issueState.value&&l.lotState===0&&(!moNo.value||l.moNo===moNo.value)&&(!appliedSearch.value||l[searchField.value].toUpperCase().includes(appliedSearch.value))))
const selected=computed(()=>rows.value.find(l=>l.baseLotNo===selectedNo.value)??rows.value[0]??null)
function persist(value:Lot[]):boolean {if(!dev.value)return false;try{localStorage.setItem(storageKey,JSON.stringify(value));lots.value=value;return true}catch{pageError.value='無法儲存本機假資料。';return false}}
function query():void{appliedSearch.value=searchText.value.trim().toUpperCase();selectedNo.value='';pageError.value=''}
function setState(value:IssueState):void{issueState.value=value;selectedNo.value='';pageError.value=''}
function chooseMO(m:Mo):void{moNo.value=m.no;moText.value=m.no;moDialog.value=false;selectedNo.value=''}
function applyMO():void{const value=moText.value.trim().toUpperCase();if(!value){moNo.value='';return}const mo=mos.value.find(x=>x.no===value&&[3,6].includes(x.state));if(mo)chooseMO(mo);else pageError.value='工單不符合開立條件，請使用查詢視窗選取。'}
function openForm(value:Mode):void {
 if(!dev.value)return
 const row=selected.value
 if(value!=='add'&&!row)return
 if(value==='edit'&&row?.issueState!==0)return
 mode.value=value;tab.value='process';formError.value=''
 if(value==='edit')draft.value=clone(row!)
 else if(value==='copy'){draft.value=clone(row!);draft.value.baseLotNo='';draft.value.issueState=0;draft.value.lotState=0;draft.value.creator=user();draft.value.createDate=now();draft.value.reviser='';draft.value.reviseDate=''}
 else{const mo=mos.value.find(x=>x.no===moNo.value);if(!mo){mode.value=null;pageError.value='請先選擇工單。';return}draft.value=makeLot('',mo);draft.value.inputQty=0;draft.value.components=[];draft.value.properties=[]}
}
function closeForm():void{draft.value=null;mode.value=null}
function validate():string {
 const d=draft.value;if(!d)return '沒有可儲存的資料。'
 if(!d.baseLotNo.trim())return '批號不可空白。'
 if(!d.roNo||!d.moNo||!d.productNo||!d.productVersion)return '訂單、工單、產品及版次不可空白。'
 if(!Number.isFinite(d.inputQty)||d.inputQty<=0)return '投入數量必須大於 0。'
 if(!d.unitNo.trim())return '投入單位不可空白。'
 if(!Number.isInteger(d.priority)||d.priority<0||d.priority>99)return '優先序需介於 0 到 99。'
 const routes=d.processes.filter(x=>x.selected)
 if(!routes.length)return '至少選擇一筆流程。'
 if(new Set(routes.map(x=>x.order)).size!==routes.length)return '流程順序不可重複選取。'
 if(routes[0]!.hasComponent&&d.components.filter(x=>x.selected).length!==Math.floor(d.inputQty))return '元件筆數需與投入數量相符。'
 for(const p of d.properties){if(!p.no.trim())return '屬性編號不可空白。';if(p.type==='數字'&&p.value&&!Number.isFinite(Number(p.value)))return '屬性值必須為數字。';if(p.type==='日期'&&p.value&&Number.isNaN(Date.parse(p.value)))return '屬性值必須為日期。'}
 if(mode.value!=='edit'&&lots.value.some(x=>x.baseLotNo===d.baseLotNo.trim()))return '批號已存在。'
 return ''
}
function save(close:boolean):void {
 if(!dev.value||!draft.value)return
 formError.value=validate();if(formError.value)return
 const row=clone(draft.value);row.baseLotNo=row.baseLotNo.trim();row.reviser=user();row.reviseDate=now()
 const next=mode.value==='edit'?lots.value.map(x=>x.baseLotNo===row.baseLotNo?row:x):[...lots.value,row]
 if(persist(next)){pageError.value='';if(close)closeForm()}
}
async function remove():Promise<void>{
 const row=selected.value;if(!dev.value||!row)return
 if(row.issueState!==0||row.lotState!==0){pageError.value='只有未簽核且未下線批次可以刪除。';return}
 if(!await requestConfirmation('確定刪除 '+row.baseLotNo+'？DEV 僅模擬 CloseLot 關聯清理。'))return
 persist(lots.value.filter(x=>x.baseLotNo!==row.baseLotNo));selectedNo.value=''
}
async function release():Promise<void>{
 const row=selected.value,mo=mos.value.find(x=>x.no===row?.moNo);if(!dev.value||!row)return
 if(!mo||row.issueState!==0||row.lotState!==0||mo.unrelease<row.inputQty){pageError.value='工單狀態或未下線量不符合下線條件。';return}
 if(!await requestConfirmation('確定下線 '+row.baseLotNo+'？DEV 不執行庫存扣料。'))return
 if(persist(lots.value.map(x=>x.baseLotNo===row.baseLotNo?{...x,lotState:1 as const}:x))){mos.value=mos.value.map(x=>x.no===mo.no?{...x,unrelease:x.unrelease-row.inputQty}:x);selectedNo.value=''}
}
function addProcess():void{if(!draft.value)return;draft.value.processes.push({order:Math.max(0,...draft.value.processes.map(x=>x.order))+10,psNo:'DEV-PS-NEW',processNo:'DEV-PROC-NEW',version:'01',hasComponent:false,selected:true})}
</script>
<template>
<section class="lot-page"><header><h2>生產批開立</h2><span>WP101 / CREATE LOT</span></header>
<template v-if="dev">
 <div class="filter"><label v-for="s in stateOptions" :key="s.value"><input type="radio" name="issueState" :checked="issueState===s.value" @change="setState(s.value)">{{s.label}}</label><label>工單編號<input v-model="moText" @keyup.enter="applyMO"><button type="button" @click="moDialog=true;moSearch=''">...</button></label><select v-model="searchField"><option value="baseLotNo">批號</option><option value="moNo">工單</option></select><input v-model="searchText" aria-label="查詢內容" @keyup.enter="query"><button @click="query">查詢</button></div>
 <div class="layout"><main class="panel"><div class="tools"><button @click="openForm('add')">新增(A)</button><button :disabled="!selected||selected.issueState!==0" @click="openForm('edit')">修改(E)</button><button :disabled="!selected" @click="openForm('copy')">複製(C)</button><button :disabled="!selected||selected.issueState!==0" @click="release">下線</button><button :disabled="!selected||selected.issueState!==0" @click="remove">刪除(D)</button></div>
 <div class="grid"><table><thead><tr><th></th><th>批號</th><th>工單</th><th>訂單</th><th>項次</th><th>產品</th><th>版次</th><th>投入量</th><th>單位</th><th>優先序</th><th>狀態</th><th>建立人</th><th>建立日期</th></tr></thead><tbody><tr v-for="l in rows" :key="l.baseLotNo" :class="{chosen:selected?.baseLotNo===l.baseLotNo}" @click="selectedNo=l.baseLotNo" @dblclick="openForm('edit')"><td><input type="radio" name="lot" :checked="selected?.baseLotNo===l.baseLotNo" @click.stop="selectedNo=l.baseLotNo"></td><td>{{l.baseLotNo}}</td><td>{{l.moNo}}</td><td>{{l.roNo}}</td><td>{{l.itemNo}}</td><td>{{l.productNo}}</td><td>{{l.productVersion}}</td><td>{{l.inputQty}}</td><td>{{l.unitNo}}</td><td>{{l.priority}}</td><td>{{stateOptions.find(x=>x.value===l.issueState)?.label}}</td><td>{{l.creator}}</td><td>{{new Date(l.createDate).toLocaleString()}}</td></tr><tr v-if="!rows.length"><td colspan="13">查無資料</td></tr></tbody></table></div><p>資料總筆數 {{rows.length}}</p><p v-if="pageError" class="error" role="alert">{{pageError}}</p></main>
 <aside><button @click="openForm('add')">新增(A)</button><button :disabled="!selected||selected.issueState!==0" @click="openForm('edit')">修改(E)</button><button :disabled="!selected" @click="openForm('copy')">複製(C)</button><button :disabled="!selected||selected.issueState!==0" @click="release">下線</button><button :disabled="!selected||selected.issueState!==0" @click="remove">刪除(D)</button><button @click="emit('close')">關閉</button></aside></div>
</template><p v-else role="status">此功能目前僅提供 DEV 假資料預覽。</p>
<div v-if="moDialog" class="overlay"><section class="dialog" role="dialog" aria-modal="true" aria-label="選擇工單"><h3>選擇工單</h3><input v-model="moSearch" placeholder="工單編號篩選"><div class="grid"><table><thead><tr><th>工單</th><th>狀態</th><th>訂單</th><th>產品</th><th>未下線量</th><th></th></tr></thead><tbody><tr v-for="m in eligibleMOs" :key="m.no"><td>{{m.no}}</td><td>{{m.state}}</td><td>{{m.ro}}</td><td>{{m.product}}</td><td>{{m.unrelease}}</td><td><button @click="chooseMO(m)">選取</button></td></tr></tbody></table></div><footer><button @click="moDialog=false">關閉</button></footer></section></div>
<div v-if="draft&&mode" class="overlay"><form class="dialog detail" role="dialog" aria-modal="true" aria-label="生產批資料" @submit.prevent="save(true)"><h3>生產批資料（{{mode==='add'?'新增':mode==='edit'?'修改':'複製'}}）</h3>
<div class="fields"><label>批號<input v-model="draft.baseLotNo" :readonly="mode==='edit'" required></label><label>工單編號<input v-model="draft.moNo" readonly></label><label>訂單編號<input v-model="draft.roNo" readonly></label><label>項次<input v-model.number="draft.itemNo" readonly></label><label>產品編號<input v-model="draft.productNo" readonly></label><label>產品版次<input v-model="draft.productVersion" readonly></label><label>客戶編號<input v-model="draft.customerNo" readonly></label><label>工單類別<input v-model="draft.moTypeNo" readonly></label><label>工單類別名稱<input v-model="draft.moTypeName" readonly></label><label>客戶批號<input v-model="draft.customerLotNo"></label><label>回廠單號<input v-model="draft.returnNo"></label><label>投入數量<input v-model.number="draft.inputQty" type="number" min="0.0001" step="0.0001"></label><label>投入單位<input v-model="draft.unitNo"></label><label>工程編號<input v-model="draft.engNo"></label><label>工程版次<input v-model="draft.engVersion"></label><label>優先序<input v-model.number="draft.priority" type="number" min="0" max="99"></label><label>Hot Lot<select v-model="draft.hotLot"><option :value="false">否</option><option :value="true">是</option></select></label><label class="wide">說明<textarea v-model="draft.description"></textarea></label><label>建立人<input v-model="draft.creator" readonly></label><label>建立日期<input :value="new Date(draft.createDate).toLocaleString()" readonly></label><label>修改人<input v-model="draft.reviser" readonly></label><label>修改日期<input :value="draft.reviseDate ? new Date(draft.reviseDate).toLocaleString() : ''" readonly></label></div>
<nav><button type="button" @click="tab='process'">流程</button><button type="button" @click="tab='component'">元件</button><button type="button" @click="tab='property'">Lot Property</button></nav>
<section v-if="tab==='process'" class="tab"><div class="tools"><b>流程</b><button type="button" @click="addProcess">新增</button></div><div class="grid"><table><thead><tr><th>選擇</th><th>順序</th><th>生產區段</th><th>流程編號</th><th>版次</th><th>含元件</th></tr></thead><tbody><tr v-for="p in draft.processes" :key="p.order"><td><input v-model="p.selected" type="checkbox"></td><td>{{p.order}}</td><td>{{p.psNo}}</td><td>{{p.processNo}}</td><td>{{p.version}}</td><td><input v-model="p.hasComponent" type="checkbox"></td></tr></tbody></table></div><div class="tools"><b>略過作業站</b><button type="button" @click="draft!.skippedOps.push('DEV-OP-NEW')">新增</button></div><ul><li v-for="(x,i) in draft.skippedOps" :key="i">{{x}} <button type="button" @click="draft!.skippedOps.splice(i,1)">刪除</button></li></ul></section>
<section v-else-if="tab==='component'" class="tab"><div class="tools"><b>元件</b><button type="button" @click="draft!.components.push({no:'DEV-COMP-NEW',product:'',qty:1,selected:true})">新增</button><button type="button" @click="draft!.components.forEach(x=>x.selected=true)">全選</button><button type="button" @click="draft!.components.forEach(x=>x.selected=false)">全不選</button></div><div class="grid"><table><thead><tr><th>選擇</th><th>元件編號</th><th>產品</th><th>數量</th></tr></thead><tbody><tr v-for="(c,i) in draft.components" :key="i"><td><input v-model="c.selected" type="checkbox"></td><td><input v-model="c.no"></td><td><input v-model="c.product"></td><td><input v-model.number="c.qty" type="number"></td></tr></tbody></table></div></section>
<section v-else class="tab"><div class="tools"><b>Lot Property</b><button type="button" @click="draft!.properties.push({no:'',value:'',type:'文字',system:false})">新增</button></div><div class="grid"><table><thead><tr><th>屬性編號</th><th>屬性值</th><th>型別</th><th>來源</th><th></th></tr></thead><tbody><tr v-for="(p,i) in draft.properties" :key="i"><td><input v-model="p.no"></td><td><input v-model="p.value"></td><td><select v-model="p.type"><option>文字</option><option>數字</option><option>日期</option></select></td><td>{{p.system?'系統':'使用者'}}</td><td><button type="button" @click="draft!.properties.splice(i,1)">刪除</button></td></tr></tbody></table></div></section>
<p v-if="formError" class="error" role="alert">{{formError}}</p><footer><button type="submit">確定</button><button type="button" @click="save(false)">套用</button><button type="button" @click="closeForm">關閉</button></footer></form></div>
<ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation"/></section>
</template>
<style scoped>
.lot-page{display:grid;gap:10px;padding:14px;align-content:start;color:#26313e;min-height:0}header{display:flex;gap:12px;align-items:baseline;border-bottom:1px solid #c4cbd3;padding-bottom:8px}h2,h3,p{margin:0}.filter,.tools,nav,footer{display:flex;align-items:center;gap:7px;flex-wrap:wrap}.filter{border:1px solid #c4cbd3;padding:7px}.filter label{display:flex;align-items:center;gap:4px}.layout{display:grid;grid-template-columns:minmax(0,1fr) 110px;gap:10px}.panel{display:grid;gap:8px;border:1px solid #c4cbd3;padding:7px;min-width:0}.grid{overflow:auto;border:1px solid #bbc4cd;background:#fff;max-height:48vh}table{border-collapse:collapse;width:100%;font-size:13px}th,td{border:1px solid #c9cfd6;padding:5px 8px;white-space:nowrap;text-align:left}th{background:#e9edf1}tr.chosen td{background:#fff0b3}tbody tr:hover td{background:#f6f2df}button,input,select,textarea{font:inherit}button{min-height:29px;padding:3px 10px;border:1px solid #b8c0c8;border-radius:3px;background:#f7f7f7;color:inherit}button:disabled{opacity:.5}aside{display:flex;flex-direction:column;gap:7px}aside button{min-height:36px}.overlay{position:fixed;inset:0;z-index:160;display:grid;place-items:center;background:#26334266;padding:16px}.dialog{width:min(850px,100%);max-height:92vh;overflow:auto;display:grid;gap:12px;background:white;border:1px solid #87939f;padding:16px;box-shadow:0 8px 28px #1113}.detail{width:min(1120px,100%)}.fields{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:8px 12px}.fields label{display:grid;gap:3px;font-size:13px}.fields input,.fields select,.fields textarea{width:100%;min-height:27px}.wide{grid-column:1/-1}.tab{display:grid;gap:8px;min-height:140px}footer{justify-content:flex-end;border-top:1px solid #e0e3e6;padding-top:8px}.error{color:#a12626}@media(max-width:900px){.layout{grid-template-columns:1fr}aside{flex-direction:row;flex-wrap:wrap}.fields{grid-template-columns:repeat(2,minmax(120px,1fr))}}
</style>
