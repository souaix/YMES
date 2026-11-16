<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy' | null
interface StateRow {
  accessoryCategory: string
  accessoryState: number
  accessoryStateName: string
  initialFlag: 0 | 1
  issueState: IssueState
  description: string | null
  creator: string | null
  createDate: string | null
  operationName: string
  interfaceName: string | null
  executionFile: string | null
  dataStamp: number
  reviser: string | null
  reviseDate: string | null
  stateColor: number
}
interface StateOption { accessoryState: number; stateName: string; stateColor: number }
interface InterfaceOption { interfaceName: string; executionFile: string }
interface CategoryOption { accessoryCategory: string; issueState: IssueState }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; initialCategory?: string; compact?: boolean }>(), { initialCategory: '', compact: false })
const emit = defineEmits<{ close: [] }>()
const base = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = import.meta.env.DEV
const rowsKey = 'ymes-dev-mock:accessory-category-states:ep204:v1'
const categoryKey = 'ymes-dev-mock:accessory-categories:ep201:v1'
const stateSeed: StateRow[] = [
  { accessoryCategory: 'JIG', accessoryState: 10, accessoryStateName: '可用', initialFlag: 1, issueState: 0, description: '開發假資料：初始狀態', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', operationName: '入庫確認', interfaceName: 'frmACCStateChangeDescription', executionFile: 'ES_ACCStateChangeDescription.dll', dataStamp: 0, reviser: null, reviseDate: null, stateColor: 13434828 },
  { accessoryCategory: 'JIG', accessoryState: 20, accessoryStateName: '維修中', initialFlag: 0, issueState: 2, description: '開發假資料：送修狀態', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', operationName: '送修登記', interfaceName: null, executionFile: null, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-03T09:00:00+08:00', stateColor: 13421823 },
  { accessoryCategory: 'CONSUMABLE', accessoryState: 10, accessoryStateName: '可用', initialFlag: 1, issueState: 2, description: '開發假資料：耗材初始狀態', creator: 'DEV001', createDate: '2026-11-02T08:00:00+08:00', operationName: '領用確認', interfaceName: 'frmACCStateChangeDescription', executionFile: 'ES_ACCStateChangeDescription.dll', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-04T09:00:00+08:00', stateColor: 13434828 },
  { accessoryCategory: 'JIG', accessoryState: 30, accessoryStateName: '報廢', initialFlag: 0, issueState: -1, description: '開發假資料：不使用狀態範例', creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', operationName: '報廢確認', interfaceName: null, executionFile: null, dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-06T09:00:00+08:00', stateColor: 10066329 },
]
const seedStates: StateOption[] = [
  { accessoryState: 10, stateName: '可用', stateColor: 13434828 },
  { accessoryState: 20, stateName: '維修中', stateColor: 13421823 },
  { accessoryState: 30, stateName: '報廢', stateColor: 10066329 },
]
const reservedOperations = ['CREATE', 'USE', 'REPAIR', 'SCRAP', 'BORROW', 'RETURN', 'DISPOSE', '建立', '使用', '維修', '報廢', '借用', '歸還']
const interfaceSeed: InterfaceOption[] = [{ interfaceName: 'frmACCStateChangeDescription', executionFile: 'ES_ACCStateChangeDescription.dll' }]
const rows = ref<StateRow[]>([])
const categories = ref<CategoryOption[]>([])
const stateOptions = ref<StateOption[]>([])
const interfaceOptions = ref<InterfaceOption[]>([])
const reserved = ref<string[]>([])
const status = ref<IssueState>(0)
const searchField = ref<'AccessoryCategory' | 'AccessoryState' | 'OperationName'>('AccessoryCategory')
const keyword = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<Mode>(null)
const form = ref({ accessoryCategory: '', accessoryState: '', accessoryStateName: '', initialFlag: false, operationName: '', interfaceName: 'frmACCStateChangeDescription', executionFile: 'ES_ACCStateChangeDescription.dll', description: '' })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
function rowKey(row: StateRow): string { return row.accessoryCategory + '|' + row.accessoryState + '|' + row.operationName }
const visible = computed(() => rows.value.filter(row => {
  if (row.issueState !== status.value) return false
  const text = keyword.value.trim().toLowerCase()
  if (!text) return true
  const value = searchField.value === 'AccessoryCategory' ? row.accessoryCategory : searchField.value === 'AccessoryState' ? String(row.accessoryState) : row.operationName
  return value.toLowerCase().includes(text)
}).sort((a,b) => a.accessoryCategory.localeCompare(b.accessoryCategory) || a.accessoryState-b.accessoryState || a.operationName.localeCompare(b.operationName)))
const current = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) || null)
const targets = computed(() => { const chosen=rows.value.filter(row=>selectedKeys.value.includes(rowKey(row)));return chosen.length?chosen:current.value?[current.value]:[] })
const canEdit = computed(() => current.value?.issueState===0)
const canCopy = computed(() => !!current.value)
const canApprove = computed(() => targets.value.length>0&&targets.value.every(row=>row.issueState===0))
const canDelete = computed(() => targets.value.length>0&&targets.value.every(row=>[0,2,-1].includes(row.issueState)))
function readArray<T>(key:string,fallback:T[]):T[]{try{const raw=localStorage.getItem(key);return raw?JSON.parse(raw) as T[]:structuredClone(fallback)}catch{return structuredClone(fallback)}}
function writeArray<T>(key:string,value:T[]):void{localStorage.setItem(key,JSON.stringify(value))}
function userNo():string{return props.currentUserNo||'DEV001'}
function now():string{return new Date().toISOString()}
function dateText(value:string|null):string{if(!value)return'';const date=new Date(value);return Number.isNaN(+date)?value:date.toLocaleString('zh-TW',{hour12:false})}
function colorHex(value:number):string{return '#'+((value>>>0)&0xffffff).toString(16).padStart(6,'0')}
function apiPath(path:string):string{if(!props.apiBaseUrl.trim())throw Error('目前環境尚未設定 API 位址。');return new URL(path.replace(/^\/+/,''),new URL(props.apiBaseUrl.replace(/\/+$/,'')+'/',location.origin)).toString()}
async function api<T>(path:string,init:RequestInit={}):Promise<T>{const r=await fetch(apiPath(path),{credentials:'include',...init});if(r.status===401){location.assign(base+'/');throw Error('登入狀態已逾期，請重新登入。')}if(!r.ok){const body=await r.json().catch(()=>null) as {title?:string;detail?:string;message?:string}|null;if(r.status===404)throw Error('配件分類狀態 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。');throw Error(body?.detail||body?.message||body?.title||'API 回應錯誤 ('+r.status+')。')}return r.status===204?undefined as T:await r.json() as T}
function reconcile():void{selectedKeys.value=selectedKeys.value.filter(key=>visible.value.some(row=>rowKey(row)===key));if(!visible.value.some(row=>rowKey(row)===activeKey.value))activeKey.value=visible.value[0]?rowKey(visible.value[0]):''}
function query():void{reconcile();error.value='';notice.value=''}
async function load():Promise<void>{
  loading.value=true;error.value=''
  try{
    if(mock){
      rows.value=readArray(rowsKey,stateSeed)
      categories.value=readArray(categoryKey,[{accessoryCategory:'JIG',issueState:0},{accessoryCategory:'CONSUMABLE',issueState:2},{accessoryCategory:'MEASURING',issueState:-1}])
      stateOptions.value=structuredClone(seedStates)
      interfaceOptions.value=structuredClone(interfaceSeed)
      reserved.value=[...reservedOperations]
    }else{
      const categoryQuery=props.initialCategory?'?accessoryCategory='+encodeURIComponent(props.initialCategory):''
      const [data,categoryData,stateData,interfaceData,controlData]=await Promise.all([
        api<StateRow[]>('api/equipment/accessory-category-states'+categoryQuery),
        api<CategoryOption[]>('api/equipment/accessory-categories'),
        api<StateOption[]>('api/equipment/accessory-states'),
        api<InterfaceOption[]>('api/system/functions?executionMode=0&accessoryStateChangeInterface=true'),
        api<{controlNo:string;controlName:string}[]>('api/equipment/accessory-state-controls?controlType=EMS')
      ])
      rows.value=data;categories.value=categoryData;stateOptions.value=stateData;interfaceOptions.value=interfaceData;reserved.value=controlData.flatMap(x=>[x.controlNo,x.controlName])
    }
    reconcile()
  }catch(e){rows.value=[];error.value=e instanceof Error?e.message:'配件分類狀態資料載入失敗。'}
  finally{loading.value=false}
}
function choose(row:StateRow):void{activeKey.value=rowKey(row);error.value=''}
function toggle(row:StateRow):void{const key=rowKey(row);selectedKeys.value=selectedKeys.value.includes(key)?selectedKeys.value.filter(x=>x!==key):[...selectedKeys.value,key];activeKey.value=key}
function onStateChange():void{const found=stateOptions.value.find(x=>x.accessoryState===Number(form.value.accessoryState));form.value.accessoryStateName=found?.stateName||''}
function onInterfaceChange():void{form.value.executionFile=interfaceOptions.value.find(x=>x.interfaceName===form.value.interfaceName)?.executionFile||''}
function openForm(next:Exclude<Mode,null>):void{
  const row=next==='add'?null:current.value
  if(next!=='add'&&!row){error.value='請先選取一筆可用狀態資料。';return}
  if(next==='edit'&&row?.issueState!==0){error.value='只有未簽核資料可以修改。';return}
  mode.value=next
  form.value=next==='edit'&&row
    ?{accessoryCategory:row.accessoryCategory,accessoryState:String(row.accessoryState),accessoryStateName:row.accessoryStateName,initialFlag:row.initialFlag===1,operationName:row.operationName,interfaceName:row.interfaceName||'',executionFile:row.executionFile||'',description:row.description||''}
    :next==='copy'&&row
      ?{accessoryCategory:row.accessoryCategory,accessoryState:String(row.accessoryState),accessoryStateName:row.accessoryStateName,initialFlag:row.initialFlag===1,operationName:row.operationName,interfaceName:row.interfaceName||'',executionFile:row.executionFile||'',description:row.description||''}
      :{accessoryCategory:props.initialCategory||'',accessoryState:'',accessoryStateName:'',initialFlag:false,operationName:'',interfaceName:'frmACCStateChangeDescription',executionFile:'ES_ACCStateChangeDescription.dll',description:''}
  audit.value=next==='edit'&&row?{creator:row.creator||'',createDate:dateText(row.createDate),reviser:userNo(),reviseDate:new Date().toLocaleString('zh-TW',{hour12:false})}:{creator:userNo(),createDate:new Date().toLocaleString('zh-TW',{hour12:false}),reviser:'',reviseDate:''}
  error.value=''
}
function validate():string{
  const category=form.value.accessoryCategory.trim().toUpperCase(), state=Number(form.value.accessoryState), operation=form.value.operationName.trim()
  if(!category)return'配件分類為必填。'
  if(!Number.isInteger(state)||state<-99||state>99||!stateOptions.value.some(x=>x.accessoryState===state))return'請選擇有效的配件狀態。'
  if(!form.value.accessoryStateName)return'配件狀態名稱為必填。'
  if(!operation)return'作用名稱為必填。'
  if(reserved.value.some(value=>value.trim().toLowerCase()===operation.toLowerCase()))return'作用名稱不可與「配件狀態控制設定」中的標準動作相同。'
  if(operation.length>50)return'作用名稱不可超過 50 個字元。'
  if(form.value.interfaceName.length>50)return'介面名稱不可超過 50 個字元。'
  if(form.value.executionFile.length>50)return'執行檔不可超過 50 個字元。'
  if(form.value.description.length>255)return'說明不可超過 255 個字元。'
  const key=category+'|'+state+'|'+operation
  if(mode.value!=='edit'&&rows.value.some(row=>rowKey(row).toLowerCase()===key.toLowerCase()))return'相同配件分類、狀態及作用名稱已存在。'
  const editingKey=mode.value==='edit'&&current.value?rowKey(current.value):'';if(form.value.initialFlag&&rows.value.some(row=>row.accessoryCategory.toUpperCase()===category&&row.initialFlag===1&&rowKey(row)!==editingKey))return'同一配件分類只能設定一個初始狀態。'
  return''
}
async function save(keep:boolean):Promise<void>{
  if(!mode.value)return
  const problem=validate();if(problem){error.value=problem;return}
  busy.value=true;error.value=''
  const action=mode.value,old=current.value,category=form.value.accessoryCategory.trim().toUpperCase(),state=Number(form.value.accessoryState),operation=form.value.operationName.trim(),stamp=now(),selectedState=stateOptions.value.find(x=>x.accessoryState===state)
  const payload={accessoryCategory:category,accessoryState:state,accessoryStateName:form.value.accessoryStateName,initialFlag:form.value.initialFlag?1:0,operationName:operation,interfaceName:form.value.interfaceName||null,executionFile:form.value.executionFile||null,description:form.value.description||null,dataStamp:old?.dataStamp}
  try{
    if(mock){
      const all=readArray(rowsKey,stateSeed)
      if(action==='edit'&&old){const i=all.findIndex(x=>rowKey(x)===rowKey(old));if(i<0)throw Error('找不到要修改的資料。');all[i]={...all[i],initialFlag:payload.initialFlag as 0|1,interfaceName:payload.interfaceName,executionFile:payload.executionFile,description:payload.description,dataStamp:all[i].dataStamp+1,reviser:userNo(),reviseDate:stamp}}
      else all.push({accessoryCategory:category,accessoryState:state,accessoryStateName:selectedState?.stateName||form.value.accessoryStateName,initialFlag:payload.initialFlag as 0|1,issueState:0,description:payload.description,creator:userNo(),createDate:stamp,operationName:operation,interfaceName:payload.interfaceName,executionFile:payload.executionFile,dataStamp:0,reviser:null,reviseDate:null,stateColor:selectedState?.stateColor||0})
      writeArray(rowsKey,all)
    }else if(action==='edit')await api('api/equipment/accessory-category-states/edit',{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)})
    else await api('api/equipment/accessory-category-states',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)})
    await load();activeKey.value=category+'|'+state+'|'+operation;selectedKeys.value=[];notice.value=action==='edit'?'可用狀態已修改。':'可用狀態已新增。'
    if(!keep)mode.value=null
    else if(action!=='edit')form.value={accessoryCategory:props.initialCategory||'',accessoryState:'',accessoryStateName:'',initialFlag:false,operationName:'',interfaceName:'frmACCStateChangeDescription',executionFile:'ES_ACCStateChangeDescription.dll',description:''}
  }catch(e){error.value=e instanceof Error?e.message:'儲存失敗。'}finally{busy.value=false}
}
async function approve():Promise<void>{
  const list=targets.value;if(!canApprove.value||!list.length)return
  if(!window.confirm('確定核准選取的配件分類狀態？'))return
  busy.value=true;error.value=''
  try{
    if(mock){const keys=new Set(list.map(row=>rowKey(row))),stamp=now();writeArray(rowsKey,readArray(rowsKey,stateSeed).map(row=>keys.has(rowKey(row))?{...row,issueState:2 as IssueState,dataStamp:row.dataStamp+1,reviser:userNo(),reviseDate:stamp}:row))}
    else for(const row of list)await api('api/equipment/accessory-category-states/approve',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({accessoryCategory:row.accessoryCategory,accessoryState:row.accessoryState,operationName:row.operationName,issueState:row.issueState,dataStamp:row.dataStamp})})
    await load();selectedKeys.value=[];notice.value='已送出核准；正式簽核結果依 API 回傳狀態顯示。'
  }catch(e){error.value=e instanceof Error?e.message:'核准失敗。'}finally{busy.value=false}
}
async function remove():Promise<void>{
  const list=targets.value;if(!canDelete.value||!list.length)return
  if(!window.confirm('確定刪除選取的可用狀態？'))return
  busy.value=true;error.value=''
  try{
    if(mock){const hard=new Set(list.filter(row=>row.issueState!==2).map(row=>rowKey(row))),inactive=new Set(list.filter(row=>row.issueState===2).map(row=>rowKey(row))),stamp=now();writeArray(rowsKey,readArray(rowsKey,stateSeed).filter(row=>!hard.has(rowKey(row))).map(row=>inactive.has(rowKey(row))?{...row,issueState:-1 as IssueState,dataStamp:row.dataStamp+1,reviser:userNo(),reviseDate:stamp}:row))}
    else for(const row of list)await api('api/equipment/accessory-category-states/delete',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({accessoryCategory:row.accessoryCategory,accessoryState:row.accessoryState,operationName:row.operationName,issueState:row.issueState,dataStamp:row.dataStamp})})
    selectedKeys.value=[];await load();notice.value='可用狀態已刪除。'
  }catch(e){error.value=e instanceof Error?e.message:'刪除失敗。'}finally{busy.value=false}
}
function close():void{if(props.compact){emit('close');return}history.pushState({},'',base+'/Equipment/AccessoryCategories');dispatchEvent(new PopStateEvent('popstate'))}
function onStatusChange():void{query()}
onMounted(()=>{void load()})
</script>

<template>
  <div :class="compact?'state-overlay':'state-page'">
    <section class="state-window" :class="{compact}">
      <header class="page-title"><div><h1>配件分類狀態定義設定</h1><p v-if="initialCategory" class="category-context">配件分類：<b>{{initialCategory}}</b></p></div><span v-if="mock" class="source-tag">Development 假資料</span></header>
      <div class="query-bar">
        <div class="status-filter" role="radiogroup" aria-label="簽核狀態"><label><input v-model.number="status" type="radio" :value="0" @change="onStatusChange">未簽核</label><label><input v-model.number="status" type="radio" :value="1" @change="onStatusChange">簽核中</label><label><input v-model.number="status" type="radio" :value="2" @change="onStatusChange">已簽核</label><label><input v-model.number="status" type="radio" :value="-1" @change="onStatusChange">不使用</label></div>
        <select v-model="searchField" class="search-select"><option value="AccessoryCategory">配件分類</option><option value="AccessoryState">配件狀態</option><option value="OperationName">作用名稱</option></select><input v-model="keyword" class="query-input" @keyup.enter="query"><span>%</span><button class="win-btn" @click="query">查詢</button>
      </div>
      <p v-if="mock" class="mock-note">本機 Development 假資料，只存於瀏覽器 localStorage。</p><p v-if="error" class="message error">{{error}}</p><p v-if="notice" class="message success">{{notice}}</p>
      <section class="grid-panel"><div class="actions"><button class="win-btn primary" :disabled="busy" @click="openForm('add')">新增(A)</button><button class="win-btn" :disabled="busy||!canEdit" @click="openForm('edit')">修改(E)</button><button class="win-btn" :disabled="busy||!canCopy" @click="openForm('copy')">複製(C)</button><button class="win-btn" :disabled="busy||!canApprove" @click="approve">核准</button><button class="win-btn" :disabled="busy||!canDelete" @click="remove">刪除(D)</button><button class="win-btn close" @click="close">關閉</button></div>
        <div class="grid-scroll"><table class="state-grid"><thead><tr><th class="check-col"></th><th>配件分類</th><th>配件狀態</th><th>狀態名稱</th><th>作用名稱</th><th>介面名稱</th><th>說明</th><th>建立人</th><th>建立日期</th><th>狀態色彩</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
          <tr v-for="row in visible" :key="rowKey(row)" :class="{selected:activeKey===rowKey(row)}" @click="choose(row)" @dblclick="openForm('edit')"><td class="check-col" @click.stop><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" :aria-label="'選取 '+row.accessoryCategory+' '+row.accessoryState" @change="toggle(row)"></td><td>{{row.accessoryCategory}}</td><td>{{row.accessoryState}}</td><td>{{row.accessoryStateName}}</td><td>{{row.operationName}}</td><td>{{row.interfaceName}}</td><td>{{row.description}}</td><td>{{row.creator}}</td><td>{{dateText(row.createDate)}}</td><td><span class="color-chip" :style="{backgroundColor:colorHex(row.stateColor)}"></span></td><td>{{row.reviser}}</td><td>{{dateText(row.reviseDate)}}</td></tr>
          <tr v-if="!visible.length"><td colspan="12" class="empty">{{loading?'載入中…':'查無資料'}}</td></tr>
        </tbody></table></div><footer class="grid-footer">資料總筆數 <b>{{visible.length}}</b></footer>
      </section>
    </section>
    <div v-if="mode" class="backdrop" @click.self="mode=null"><form class="dialog" role="dialog" aria-modal="true" @submit.prevent="save(false)">
      <header class="dialog-head"><h2>配件分類狀態定義（{{mode==='add'?'新增':mode==='edit'?'修改':'複製'}}）</h2><button type="button" class="x" @click="mode=null">×</button></header>
      <fieldset class="group"><legend>配件分類狀態資料</legend>
        <label class="form-row"><span>配件分類</span><select v-model="form.accessoryCategory" class="kd-input" :disabled="!!initialCategory||mode==='edit'||mode==='copy"><option value="">請選擇</option><option v-for="item in categories" :key="item.accessoryCategory" :value="item.accessoryCategory">{{item.accessoryCategory}}</option></select></label>
        <label class="form-row"><span>配件狀態</span><select v-model="form.accessoryState" class="kd-input" :disabled="mode==='edit'" @change="onStateChange"><option value="">請選擇</option><option v-for="item in stateOptions" :key="item.accessoryState" :value="String(item.accessoryState)">{{item.accessoryState}}｜{{item.stateName}}</option></select></label>
        <label class="form-row"><span>狀態名稱</span><input v-model="form.accessoryStateName" class="kd-input" readonly maxlength="50"></label>
        <label class="form-row"><span>初始狀態</span><span class="check-line"><input v-model="form.initialFlag" type="checkbox">設為此配件分類的初始狀態</span></label>
        <label class="form-row"><span>作用名稱</span><input v-model.trim="form.operationName" class="kd-input" maxlength="50" :readonly="mode==='edit'"></label>
        <label class="form-row"><span>介面名稱</span><select v-model="form.interfaceName" class="kd-input" @change="onInterfaceChange"><option value="">（無）</option><option v-for="item in interfaceOptions" :key="item.interfaceName" :value="item.interfaceName">{{item.interfaceName}}</option></select></label>
        <label class="form-row"><span>執行檔</span><input v-model="form.executionFile" class="kd-input" readonly maxlength="50"></label>
        <label class="form-row"><span>說明</span><textarea v-model="form.description" class="kd-input description" maxlength="255"></textarea></label>
      </fieldset>
      <div class="audit"><label>建立人<input :value="audit.creator" readonly></label><label>建立日期<input :value="audit.createDate" readonly></label><label>修改人<input :value="audit.reviser" readonly></label><label>修改日期<input :value="audit.reviseDate" readonly></label></div>
      <p v-if="error" class="message error">{{error}}</p><footer class="dialog-foot"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" @click="mode=null">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="save(true)">套用</button></footer>
    </form></div>
  </div>
</template>

<style scoped>
.state-page{min-height:100%;padding:10px;background:#fff;color:#343434;font:14px "Microsoft JhengHei",Arial,sans-serif}.state-window{display:flex;flex-direction:column;gap:10px;min-height:100%;padding:10px;background:#fff}.state-overlay{position:fixed;inset:0;z-index:4000;display:grid;place-items:center;padding:18px;background:rgba(35,33,29,.38)}.state-window.compact{width:min(1280px,98vw);height:min(850px,94vh);min-height:0;overflow:auto;border:1px solid #aaa69d;border-radius:4px;box-shadow:0 8px 30px #0003}.page-title{display:flex;align-items:center;justify-content:space-between;padding:2px 8px 8px;border-bottom:1px solid #d7d2c7}.page-title h1{margin:0;color:#3c3529;font-size:21px;font-weight:600}.category-context{margin:5px 0 0;color:#625942}.category-context b{color:#3b3427}.source-tag{padding:3px 8px;border:1px solid #dfc56c;border-radius:3px;background:#fff4c6;color:#755916;font-size:12px}.query-bar{display:flex;align-items:center;gap:8px;min-height:42px;padding:6px 10px;border:1px solid #d6d2c9;background:#faf9f6}.status-filter{display:flex;align-items:center;gap:11px;margin-right:8px}.status-filter label{white-space:nowrap}.status-filter input{accent-color:#e28a22;margin-right:4px}.query-input,.search-select{height:29px;padding:3px 7px;border:1px solid #8f8b82;background:#fff0ae;color:#25231e;font:14px "Microsoft JhengHei",Arial,sans-serif}.query-input{width:180px}.search-select{min-width:145px}.mock-note{margin:0;padding:5px 9px;border-left:3px solid #d5a535;background:#fff8df;color:#6e5a25;font-size:12px}.grid-panel{min-height:0;padding:6px;border:1px solid #c9c6bf;background:#fff}.actions{display:flex;flex-wrap:wrap;gap:5px;margin-bottom:6px}.win-btn{min-height:29px;padding:4px 11px;border:1px solid #bdb9b0;border-radius:3px;background:linear-gradient(#fff,#f0eee9);color:#35332e;font:14px "Microsoft JhengHei",Arial,sans-serif;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){background:#fff0c1;border-color:#c39b42}.win-btn:disabled{opacity:.48;cursor:not-allowed}.win-btn.primary{border-color:#e9a33b;background:#f8a32d;color:#fff}.win-btn.close{margin-left:auto}.grid-scroll{width:100%;overflow:auto}.state-grid{width:100%;border-collapse:collapse;table-layout:auto}.state-grid th,.state-grid td{height:29px;padding:4px 8px;border:1px solid #c9c6bf;text-align:left;white-space:nowrap}.state-grid th{background:#f5cd64;color:#39342a;font-weight:600}.state-grid tbody tr{cursor:pointer}.state-grid tbody tr:nth-child(even){background:#fbfaf7}.state-grid tbody tr:hover{background:#fff4cf}.state-grid tbody tr.selected{background:#ffe59a}.check-col{width:30px;text-align:center!important}.check-col input{accent-color:#d98b22}.color-chip{display:inline-block;width:64px;height:18px;border:1px solid #777;vertical-align:middle}.grid-footer{padding:7px 2px 0;color:#666;font-size:12px}.grid-footer b{margin-left:10px;font-weight:400}.empty{text-align:center!important;color:#777;padding:18px!important}.message{margin:0;padding:7px 10px;border-radius:3px;font-size:13px}.error{background:#fff1ee;color:#972c1b}.success{background:#edf6e9;color:#426b34}.backdrop{position:fixed;inset:0;z-index:4100;display:grid;place-items:center;padding:18px;background:rgba(35,33,29,.38)}.dialog{width:min(820px,96vw);max-height:94vh;overflow:auto;padding:12px;border:1px solid #aaa69d;border-radius:4px;background:#fff;box-shadow:0 8px 30px #0003}.dialog-head{display:flex;align-items:center;justify-content:space-between;padding:4px 2px 10px;border-bottom:1px solid #d7d2c7}.dialog-head h2{margin:0;color:#40392d;font-size:18px;font-weight:600}.x{width:29px;height:27px;border:1px solid #c5c0b6;border-radius:3px;background:#f5f3ef;color:#565149;font-size:20px;line-height:20px;cursor:pointer}.group{display:grid;grid-template-columns:1fr 1fr;column-gap:18px;margin:13px 0 10px;padding:12px 15px;border:1px solid #bcb8af}.group legend{padding:0 6px;color:#554a35}.form-row{display:grid;grid-template-columns:105px minmax(130px,1fr);align-items:center;gap:8px;margin:7px 0}.form-row>span:first-child{text-align:right}.kd-input{box-sizing:border-box;width:100%;min-height:29px;padding:4px 7px;border:1px solid #8f8b82;border-radius:0;background:#fff0ae;color:#25231e;font:14px "Microsoft JhengHei",Arial,sans-serif}.kd-input:read-only,.kd-input:disabled{background:#efeee9;color:#666}.description{min-height:65px;resize:vertical}.check-line{display:flex;align-items:center;gap:7px}.check-line input{accent-color:#d98b22}.audit{display:grid;grid-template-columns:1fr 1fr;gap:9px;padding:9px;border:1px solid #d2cfc7}.audit label{display:grid;grid-template-columns:85px minmax(0,1fr);align-items:center;gap:8px}.audit input{min-width:0;height:27px;padding:3px 6px;border:1px solid #aaa69d;background:#efeee9;color:#666}.dialog-foot{display:flex;justify-content:flex-end;gap:7px;padding-top:12px}@media(max-width:850px){.query-bar{flex-wrap:wrap}.status-filter{flex-wrap:wrap}.group{grid-template-columns:1fr}.audit{grid-template-columns:1fr}.win-btn.close{margin-left:0}}
</style>
