<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'

type BasisRow = Record<string, string | number | null> & { qrsNo: string; issueState: number; dataStamp: number; erfNo: string }
type Field = { key: string; label: string; kind?: 'text' | 'number' | 'date' | 'textarea'; lookup?: boolean; linked?: boolean }
type FormMode = 'add' | 'edit' | 'copy'
type LinkedErf = { erfNo: string; lotNo: string; productNo: string; opNo: string; customerNo: string; lotQty: number; errorQty: number; holdDescription: string }

const props = defineProps<{ developmentPreview?: boolean; currentUserNo?: string; linkedErf?: LinkedErf | null }>()
const emit = defineEmits<{ close: []; openErfInfo: [erfNo: string] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview !== false)
const storageKey = 'ymes-dev-mock:wip-8d-basis-factory:state:v1'
const linkKey = 'ymes-dev-mock:wip-8d-erf-links:v1'
const issueLabels: Record<number, string> = { 0: 'Unfrozen', 1: 'Pending', 2: 'Active', [-1]: 'Unused' }
const qrsClasses: Record<number, string> = { 1: 'In Line QC', 2: 'Off Line QC', 3: 'WIPERF', 4: 'Other' }
const gridColumns = [
  ['qrsNo','QRSNo'],['qrsClass','QRSClass'],['qrsSummary','QRSSummary'],['lotNo','LotNo'],['lotQty','LotQty'],['sampleQty','SampleQty'],['errorQty','ErrorQty'],['factoryNo','FactoryNo'],['areaNo','AreaNo'],['moNo','MONo'],['customerNo','CustomerNo'],['productNo','ProductNo'],['productVersion','ProductVersion'],['roNo','RONo'],['opNo','OPNo'],['equipmentNo','EquipmentNo'],['erfNo','ERFNo'],['holdDescription','HoldDescription'],['description','Description'],['projectDepartment','ProjectDepartment'],['projectManager','ProjectManager'],['projectManagerName','ProjectManagerName'],['curStage','CurStage'],['planCloseDate','PlanCloseDate'],['lastUpdateDate','LastUpdateDate'],['summaryDescription','SummaryDescription'],['dispHour','DispHour'],['dispCost','DispCost'],['summaryDepartment','SummaryDepartment'],['summaryUserNo','SummaryUserNo'],['summaryUserName','SummaryUserName'],['closeType','CloseType'],['closeDepartment','CloseDepartment'],['closeUserNo','CloseUserNo'],['closeUserName','CloseUserName'],['closeDate','CloseDate'],['creator','Creator'],['createDate','CreateDate'],['reviser','Reviser'],['reviseDate','ReviseDate'],
] as const
const formFields: Field[] = [
  {key:'qrsSummary',label:'QRSSummary',kind:'textarea'}, {key:'factoryNo',label:'FactoryNo',lookup:true}, {key:'areaNo',label:'AreaNo',lookup:true},
  {key:'projectDepartment',label:'ProjectDepartment',lookup:true}, {key:'projectManager',label:'ProjectManager',lookup:true}, {key:'projectManagerName',label:'ProjectManagerName'},
  {key:'curStage',label:'CurStage'}, {key:'planCloseDate',label:'PlanCloseDate',kind:'date'},
  {key:'erfNo',label:'ERFNo',lookup:true,linked:true}, {key:'lotNo',label:'LotNo',lookup:true,linked:true}, {key:'productNo',label:'ProductNo',lookup:true,linked:true}, {key:'productVersion',label:'ProductVersion',linked:true},
  {key:'opNo',label:'OPNo',lookup:true,linked:true}, {key:'equipmentNo',label:'EquipmentNo',lookup:true,linked:true}, {key:'moNo',label:'MONo',lookup:true,linked:true},
  {key:'customerNo',label:'CustomerNo',lookup:true,linked:true}, {key:'roNo',label:'RONo',lookup:true,linked:true},
  {key:'lotQty',label:'LotQty',kind:'number',linked:true}, {key:'sampleQty',label:'SampleQty',kind:'number',linked:true}, {key:'errorQty',label:'ErrorQty',kind:'number',linked:true},
  {key:'holdDescription',label:'HoldDescription',kind:'textarea'}, {key:'description',label:'Description',kind:'textarea'},
]
const lookupValues: Record<string, string[]> = {
  factoryNo:['FACTORY-A','FACTORY-B'], areaNo:['A01','A02'], projectDepartment:['QA','ENG'], projectManager:['DEV-MGR','DEV-QA'],
  lotNo:['DEV-LOT-001','DEV-LOT-002'], productNo:['PRD-A','PRD-B'], opNo:['OP-CUT','OP-TEST'], equipmentNo:['EQ-DEV-01'],
  moNo:['MO-DEV-01'], customerNo:['CUS-A','CUS-B'], roNo:['RO-DEV-01'], erfNo:['DEV-ERF-001','DEV-ERF-002'],
}
const erfLookups: Record<string, LinkedErf> = {
  'DEV-ERF-001': {erfNo:'DEV-ERF-001',lotNo:'DEV-LOT-001',productNo:'PRD-A',opNo:'OP-CUT',customerNo:'CUS-A',lotQty:8,errorQty:0,holdDescription:'外觀檢查異常'},
  'DEV-ERF-002': {erfNo:'DEV-ERF-002',lotNo:'DEV-LOT-002',productNo:'PRD-B',opNo:'OP-TEST',customerNo:'CUS-B',lotQty:5,errorQty:1,holdDescription:'設備點檢異常'},
}
const seed: BasisRow[] = [
  {qrsNo:'QRS-DEV-001',qrsClass:4,qrsSummary:'示範矯正單',lotNo:'',lotQty:0,sampleQty:0,errorQty:0,factoryNo:'FACTORY-A',areaNo:'A01',moNo:'',customerNo:'',productNo:'',productVersion:'',roNo:'',opNo:'',equipmentNo:'',erfNo:'',holdDescription:'',description:'DEV 主檔',projectDepartment:'QA',projectManager:'DEV-MGR',projectManagerName:'示範經理',curStage:'N/A',planCloseDate:'2026-11-27',lastUpdateDate:'2026-11-20 09:00:00',summaryDescription:'',dispHour:0,dispCost:0,summaryDepartment:'',summaryUserNo:'',summaryUserName:'',closeType:null,closeDepartment:'',closeUserNo:'',closeUserName:'',closeDate:null,creator:'DEV',createDate:'2026-11-20 09:00:00',reviser:'',reviseDate:null,issueState:0,dataStamp:0},
  {qrsNo:'QRS-DEV-002',qrsClass:3,qrsSummary:'設備異常 8D 示範',lotNo:'DEV-LOT-002',lotQty:5,sampleQty:0,errorQty:1,factoryNo:'FACTORY-A',areaNo:'A01',moNo:'MO-DEV-01',customerNo:'CUS-B',productNo:'PRD-B',productVersion:'1',roNo:'RO-DEV-01',opNo:'OP-TEST',equipmentNo:'EQ-DEV-01',erfNo:'DEV-ERF-002',holdDescription:'設備點檢異常',description:'完成處置',projectDepartment:'ENG',projectManager:'DEV-MGR',projectManagerName:'示範經理',curStage:'N/A',planCloseDate:'2026-11-27',lastUpdateDate:'2026-11-19 13:00:00',summaryDescription:'',dispHour:0,dispCost:0,summaryDepartment:'',summaryUserNo:'',summaryUserName:'',closeType:null,closeDepartment:'',closeUserNo:'',closeUserName:'',closeDate:null,creator:'DEV',createDate:'2026-11-19 13:00:00',reviser:'DEV',reviseDate:'2026-11-19 13:10:00',issueState:2,dataStamp:1},
]

function blankForm(): Record<string, string | number> {
  const result: Record<string, string | number> = {qrsNo:'',qrsClass:4}
  for (const field of formFields) result[field.key] = field.kind === 'number' ? 0 : ''
  result.curStage = 'N/A'
  const date = new Date(); date.setDate(date.getDate() + 7); result.planCloseDate = date.toISOString().slice(0,10)
  return result
}
const state = ref<BasisRow[]>([])
const issueState = ref(0); const qrsSearch = ref(''); const searched = ref(false)
const queriedState = ref(0); const queriedNo = ref('')
const activeNo = ref(''); const selectedNos = ref<string[]>([])
const active = computed(() => state.value.find(row => row.qrsNo === activeNo.value) || null)
const rows = computed(() => searched.value ? state.value.filter(row => row.issueState === queriedState.value && (!queriedNo.value || row.qrsNo.toUpperCase().includes(queriedNo.value))).sort((a,b) => a.qrsNo.localeCompare(b.qrsNo)) : [])
const targets = computed(() => selectedNos.value.length ? state.value.filter(row => selectedNos.value.includes(row.qrsNo)) : active.value ? [active.value] : [])
const mode = ref<FormMode | null>(null); const form = reactive<Record<string, string | number>>(blankForm()); const editStamp = ref(0)
const linkedErfNo = ref('')
const lookupField = ref<string | null>(null); const pendingAction = ref<'delete' | 'approve' | null>(null)
const mockIssueJob = ref<'direct' | 'pending'>('direct')
const errorText = ref(''); const message = ref('選擇狀態後按 Query。')

function load(): void {
  if (!dev.value) return
  try {
    const data = JSON.parse(window.localStorage.getItem(storageKey) || 'null') as unknown
    if (Array.isArray(data) && data.every(item => item && typeof item === 'object' && typeof item.qrsNo === 'string' && typeof item.issueState === 'number' && typeof item.dataStamp === 'number')) { state.value = data as BasisRow[]; return }
  } catch { /* seed DEV records */ }
  state.value = seed.map(row => ({...row}))
}
function persist(): void { if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(state.value)) }
function loadLinks(): Record<string, string> { try { const value = JSON.parse(window.localStorage.getItem(linkKey) || '{}') as unknown; return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, string> : {} } catch { return {} } }
function saveLinks(links: Record<string, string>): void { if (dev.value) window.localStorage.setItem(linkKey, JSON.stringify(links)) }
function selectErf(erf: LinkedErf): void { Object.assign(form, { qrsClass:3, erfNo:erf.erfNo, lotNo:erf.lotNo, productNo:erf.productNo, opNo:erf.opNo, customerNo:erf.customerNo, lotQty:erf.lotQty, errorQty:erf.errorQty, holdDescription:erf.holdDescription, sampleQty:0, factoryNo:'FACTORY-A', areaNo:'A01' }); linkedErfNo.value = erf.erfNo }
function chooseLookup(key: string, value: string): void { if (key === 'erfNo' && erfLookups[value]) selectErf(erfLookups[value]); else form[key] = value; lookupField.value = null }
function query(): void { errorText.value = ''; if (!dev.value) { errorText.value = '正式 API 尚未接入。'; return }; selectedNos.value = []; activeNo.value = ''; queriedState.value = issueState.value; queriedNo.value = qrsSearch.value.trim().toUpperCase(); searched.value = true; message.value = `查得 ${rows.value.length} 筆 ${issueLabels[issueState.value]} 8D 單。` }
function selectRow(row: BasisRow): void { activeNo.value = row.qrsNo }
function toggleSelected(no: string): void { selectedNos.value = selectedNos.value.includes(no) ? selectedNos.value.filter(item => item !== no) : [...selectedNos.value, no]; activeNo.value = no }
function getMockNo(): void { const nums = state.value.map(row => Number(row.qrsNo.match(/^QRS-DEV-(\d+)$/)?.[1] || 0)); form.qrsNo = `QRS-DEV-${String(Math.max(0,...nums)+1).padStart(3,'0')}` }
function openForm(nextMode: FormMode): void {
  if (!dev.value) return
  if (nextMode !== 'add' && !active.value) { errorText.value = '請先選取一筆。'; return }
  if (nextMode === 'edit' && active.value?.issueState !== 0) { errorText.value = '只有 Unfrozen 可修改。'; return }
  Object.assign(form, blankForm())
  if (nextMode !== 'add' && active.value) for (const key of Object.keys(form)) if (active.value[key] != null) form[key] = active.value[key] as string | number
  if (nextMode !== 'edit') getMockNo()
  linkedErfNo.value = nextMode === 'edit' ? String(active.value?.erfNo || '') : ''
  editStamp.value = active.value?.dataStamp || 0
  lookupField.value = null; errorText.value = ''; mode.value = nextMode
}
function validateForm(): string {
  for (const key of ['qrsNo','qrsSummary','factoryNo','areaNo','projectDepartment','projectManager','curStage']) if (!String(form[key] ?? '').trim()) return `${key} 不可空白。`
  if (![1,2,3,4].includes(Number(form.qrsClass))) return 'QRSClass 不正確。'
  if (String(form.qrsNo).length > 30 || String(form.qrsSummary).length > 500) return 'QRSNo 或 QRSSummary 超過資料表長度。'
  for (const key of ['lotQty','sampleQty','errorQty']) if (!Number.isFinite(Number(form[key])) || Number(form[key]) < 0) return `${key} 必須為非負數。`
  if (form.planCloseDate && !Number.isFinite(Date.parse(String(form.planCloseDate)))) return 'PlanCloseDate 不正確。'
  return ''
}
function saveForm(closeAfter = true): void {
  if (!dev.value || !mode.value) return
  const problem = validateForm(); if (problem) { errorText.value = problem; return }
  const now = new Date().toISOString().slice(0,19).replace('T',' ')
  const no = String(form.qrsNo).trim().toUpperCase()
  if (mode.value !== 'edit' && linkedErfNo.value && loadLinks()[linkedErfNo.value]) { errorText.value = `${linkedErfNo.value} 已關聯 8D 單。`; return }
  if (mode.value === 'edit') {
    const current = state.value.find(row => row.qrsNo === no)
    if (!current || current.issueState !== 0 || current.dataStamp !== editStamp.value) { errorText.value = '資料已變更，請重新查詢。'; return }
    Object.assign(current, form, {qrsNo:no, dataStamp:current.dataStamp+1, reviser:props.currentUserNo || 'DEV', reviseDate:now, lastUpdateDate:now})
  } else {
    if (state.value.some(row => row.qrsNo === no)) { errorText.value = 'QRSNo 已存在，請重新取號。'; return }
    const fresh: BasisRow = { ...form, qrsNo:no, erfNo:String(form.erfNo || ''), issueState:0, dataStamp:0, lastUpdateDate:now, creator:props.currentUserNo || 'DEV', createDate:now, reviser:'', reviseDate:null,
      summaryDescription:'',dispHour:0,dispCost:0,summaryDepartment:'',summaryUserNo:'',summaryUserName:'',closeType:null,closeDepartment:'',closeUserNo:'',closeUserName:'',closeDate:null }
    state.value.push(fresh)
  }
  persist(); issueState.value = 0; queriedState.value = 0; qrsSearch.value = ''; queriedNo.value = ''; searched.value = true; activeNo.value = no; selectedNos.value = []
  if (mode.value !== 'edit' && linkedErfNo.value) { const links = loadLinks(); links[linkedErfNo.value] = no; saveLinks(links) }
  message.value = `DEV 假資料已${mode.value === 'edit' ? '修改' : '新增'} ${no}；未呼叫 API。`; errorText.value = ''
  if (closeAfter) mode.value = null
  else { editStamp.value = state.value.find(row => row.qrsNo === no)?.dataStamp || 0; if (mode.value !== 'edit') { getMockNo(); linkedErfNo.value = '' } }
}
function requestAction(action: 'delete' | 'approve'): void { errorText.value = ''; if (!dev.value || !targets.value.length) { errorText.value = '請先選取資料。'; return }; pendingAction.value = action }
function confirmAction(): void {
  const action = pendingAction.value; pendingAction.value = null
  if (!action || !dev.value || !targets.value.length) return
  const chosen = [...targets.value]
  if (action === 'approve') {
    const bad = chosen.find(row => row.issueState !== 0 || row.curStage !== 'N/A' || !row.projectDepartment || !row.projectManager || !row.planCloseDate)
    if (bad) { errorText.value = `${bad.qrsNo} 必須是 Unfrozen、CurStage=N/A，且專案部門／經理／預計結案日不可空白。`; return }
    for (const row of chosen) { row.issueState = mockIssueJob.value === 'direct' ? 2 : 1; row.dataStamp++; row.reviser = props.currentUserNo || 'DEV'; row.reviseDate = new Date().toISOString().slice(0,19).replace('T',' ') }
  } else {
    const bad = chosen.find(row => ![0,-1,2].includes(row.issueState) || (row.issueState === 2 && row.curStage !== 'N/A'))
    if (bad) { errorText.value = `${bad.qrsNo} 的狀態不可刪除。`; return }
    const physicallyDeleted = new Set(chosen.filter(row => row.issueState === 0 || row.issueState === -1).map(row => row.qrsNo))
    for (const row of chosen) if (row.issueState === 2) { row.issueState = -1; row.dataStamp++; row.reviser = props.currentUserNo || 'DEV'; row.reviseDate = new Date().toISOString().slice(0,19).replace('T',' ') }
    state.value = state.value.filter(row => !physicallyDeleted.has(row.qrsNo))
    const links = loadLinks(); for (const [erfNo, qrsNo] of Object.entries(links)) if (physicallyDeleted.has(qrsNo)) delete links[erfNo]; saveLinks(links)
  }
  persist(); selectedNos.value = []; activeNo.value = ''; searched.value = true
  message.value = `DEV 假資料已${action === 'approve' ? '送核准' : '處理刪除'} ${chosen.length} 筆；未呼叫 API。`; errorText.value = ''
}
function formatCell(row: BasisRow, key: string): string | number { const value = row[key]; if (key === 'qrsClass') return qrsClasses[Number(value)] || String(value ?? ''); return value ?? '' }
onMounted(() => { load(); if (dev.value && props.linkedErf) { openForm('add'); selectErf(props.linkedErf) } })
watch(dev, enabled => { if (!enabled) { state.value = []; activeNo.value = ''; selectedNos.value = []; mode.value = null; pendingAction.value = null; searched.value = false } })
</script>

<template>
  <section class="wp801-page">
    <div v-if="dev" class="dev-banner">DEV 假資料。Issue Job 模擬：<select v-model="mockIssueJob"><option value="direct">直接核准</option><option value="pending">進入待簽核</option></select>　流水號、簽核、資料變更都只作用在本機假資料。</div>
    <h1>廠內 8D 矯正單 <small>8D BASIS_FACTORY · WP801</small></h1>
    <p v-if="errorText" class="notice error" role="alert">{{ errorText }}</p><p v-else class="notice" role="status">{{ message }}</p>
    <fieldset class="query-frame"><legend>Query</legend><div class="query-line"><label v-for="option in [0,1,2,-1]" :key="option"><input v-model.number="issueState" type="radio" :value="option" :disabled="!dev">{{ issueLabels[option] }}</label><label>QRSNo <input v-model="qrsSearch" class="mes-input yellow" :disabled="!dev" @keydown.enter.prevent="query"></label><button type="button" class="mes-button" :disabled="!dev" @click="query">Query</button></div></fieldset>
    <fieldset class="result-frame"><legend>8DBASISFACTORYDATA</legend><div class="result-main"><div class="grid-wrap"><table><thead><tr><th class="select-col">選取</th><th v-for="column in gridColumns" :key="column[0]">{{ column[1] }}</th></tr></thead><tbody><tr v-for="row in rows" :key="row.qrsNo" :class="{active: activeNo === row.qrsNo}" @click="selectRow(row)"><td><input type="checkbox" :checked="selectedNos.includes(row.qrsNo)" @click.stop="toggleSelected(row.qrsNo)"></td><td v-for="column in gridColumns" :key="column[0]">{{ formatCell(row,column[0]) }}</td></tr><tr v-if="!rows.length"><td :colspan="gridColumns.length + 1" class="empty">{{ searched ? '沒有符合資料' : '請按 Query 查詢' }}</td></tr></tbody></table></div><div class="actions"><div><button type="button" class="mes-button" :disabled="!dev" @click="openForm('add')">ADD (&A)</button><button type="button" class="mes-button" :disabled="!dev || !active" @click="openForm('edit')">EDIT (&E)</button><button type="button" class="mes-button" :disabled="!dev || !targets.length" @click="requestAction('delete')">DEL (&D)</button><button type="button" class="mes-button" :disabled="!dev || !active" @click="openForm('copy')">Copy (&C)</button><button type="button" class="mes-button" :disabled="!dev || !targets.length" @click="requestAction('approve')">Approve (&Y)</button><button type="button" class="mes-button" :disabled="!dev || !active?.erfNo" @click="active && emit('openErfInfo',active.erfNo)">ERFINFO</button></div><button type="button" class="mes-button" @click="emit('close')">Close (&X)</button></div></div><div class="status-bar">UserNo: {{ props.currentUserNo || 'DEV' }}　資料總筆數 {{ rows.length }}　{{ new Date().toLocaleString('zh-TW') }}</div></fieldset>
    <div v-if="mode" class="modal-backdrop" @click.self="mode = null"><div class="form-modal" role="dialog" aria-modal="true" :aria-label="'8D Basis Factory ' + mode"><header><strong>8D Basis Factory ({{ mode.toUpperCase() }})</strong><button type="button" class="mes-button" @click="mode = null">Close</button></header><fieldset><legend>8DBASISFACTORYDATA</legend><div class="form-grid"><div class="class-field"><span>QRSClass</span><label v-for="option in [1,2,3,4]" :key="option"><input v-model.number="form.qrsClass" type="radio" :value="option" :disabled="!!linkedErfNo">{{ qrsClasses[option] }}</label></div><label class="form-field">QRSNo <span class="input-group"><input v-model="form.qrsNo" class="mes-input yellow" :readonly="mode === 'edit'"><button v-if="mode !== 'edit'" type="button" class="mes-button" @click="getMockNo">...</button></span></label><label v-for="field in formFields" :key="field.key" class="form-field" :class="{wide: field.kind === 'textarea'}">{{ field.label }}<span class="input-group"><textarea v-if="field.kind === 'textarea'" v-model="form[field.key]" class="mes-input yellow" rows="2" :readonly="field.linked && !!linkedErfNo && field.key !== 'holdDescription'"/><input v-else v-model="form[field.key]" :type="field.kind === 'date' ? 'date' : field.kind === 'number' ? 'number' : 'text'" :step="field.kind === 'number' ? 'any' : undefined" class="mes-input yellow" :readonly="field.linked && !!linkedErfNo"/><button v-if="field.lookup" type="button" class="mes-button" :disabled="field.linked && !!linkedErfNo" @click="lookupField = lookupField === field.key ? null : field.key">...</button></span><span v-if="lookupField === field.key" class="lookup-options"><button v-for="value in lookupValues[field.key] || []" :key="value" type="button" @click="chooseLookup(field.key,value)">{{ value }}</button></span></label></div></fieldset><div class="modal-actions"><button type="button" class="mes-button primary" @click="saveForm(true)">Confirm</button><button type="button" class="mes-button" @click="saveForm(false)">Apply</button><button type="button" class="mes-button" @click="mode = null">Close</button></div></div></div>
    <div v-if="pendingAction" class="modal-backdrop"><div class="confirm-modal" role="alertdialog" aria-modal="true"><strong>{{ pendingAction === 'delete' ? '刪除' : '核准' }}所選 {{ targets.length }} 筆 8D 單？</strong><p v-if="pendingAction === 'delete'">Unfrozen/Unused 將從 DEV 清單移除；Active 轉成 Unused。</p><div class="modal-actions"><button type="button" class="mes-button primary" @click="confirmAction">確定</button><button type="button" class="mes-button" @click="pendingAction = null">取消</button></div></div></div>
  </section>
</template>

<style scoped>
.wp801-page{height:100%;min-height:0;display:flex;flex-direction:column;gap:7px;box-sizing:border-box;padding:9px;background:#f7f5ee;color:#333;font:13px Arial,"Microsoft JhengHei",sans-serif}.dev-banner{padding:6px 9px;border:1px solid #d8c994;background:#fff8dc;color:#66531e}.dev-banner select{background:#fff0b3}h1{margin:0;font-size:18px}h1 small{font-size:12px;font-weight:400;color:#777}.notice{margin:0;min-height:18px}.error{padding:5px 8px;background:#fff0ef;color:#99251e}fieldset{min-width:0;margin:0;padding:7px 8px;border:1px solid #bbb;background:#fff}legend{padding:0 5px}.query-line{display:flex;flex-wrap:wrap;align-items:center;gap:12px}.query-line label{display:flex;align-items:center;gap:4px}.mes-input{height:27px;min-width:0;box-sizing:border-box;padding:3px 5px;border:1px solid #888;font:inherit}.mes-input.yellow{background:#fff0b3!important}.mes-button{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f2f2f2);color:#333;font:inherit;cursor:pointer}.mes-button:disabled{color:#999;cursor:default}.mes-button.primary{background:#f9bc48}.result-frame{flex:1;min-height:240px;display:flex;flex-direction:column}.result-main{display:flex;gap:9px;flex:1;min-height:0}.grid-wrap{flex:1;min-width:0;overflow:auto;border:1px solid #aaa}.grid-wrap table{border-collapse:collapse;white-space:nowrap}.grid-wrap th,.grid-wrap td{padding:5px 7px;border:1px solid #d4d4d4;text-align:left}.grid-wrap th{position:sticky;top:0;background:#ead591;font-weight:500}.grid-wrap td{background:#ffe89c}.grid-wrap tr.active td{background:#f4ca68}.grid-wrap tr{cursor:pointer}.grid-wrap .empty{background:#fff;text-align:center}.actions{display:flex;flex-direction:column;justify-content:space-between;min-width:110px}.actions>div{display:flex;flex-direction:column;gap:6px}.actions .mes-button{width:110px}.status-bar{padding:4px;color:#666}.modal-backdrop{position:fixed;inset:0;z-index:2000;display:flex;align-items:center;justify-content:center;background:#0006}.form-modal,.confirm-modal{width:min(950px,94vw);max-height:88vh;overflow:auto;padding:13px;background:#f7f5ee;border:1px solid #888;box-shadow:0 10px 32px #0004}.form-modal header{display:flex;align-items:center;justify-content:space-between;margin-bottom:9px}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px 14px}.class-field{grid-column:1/-1;display:flex;flex-wrap:wrap;align-items:center;gap:12px}.class-field label{display:flex;gap:4px;align-items:center}.form-field{position:relative;display:grid;grid-template-columns:150px minmax(0,1fr);align-items:start;gap:6px}.form-field.wide{grid-column:1/-1}.input-group{display:flex;gap:4px}.input-group .mes-input{flex:1}.input-group textarea{height:auto;min-height:54px}.lookup-options{position:absolute;z-index:3;top:100%;left:155px;display:flex;gap:4px;padding:6px;background:#fff8dc;border:1px solid #aaa}.lookup-options button{border:1px solid #aaa;background:#fff;cursor:pointer}.modal-actions{display:flex;justify-content:flex-end;gap:8px;margin-top:10px}.confirm-modal{width:min(440px,90vw)}@media(max-width:850px){.form-grid{grid-template-columns:1fr}.form-field.wide{grid-column:auto}}
</style>
