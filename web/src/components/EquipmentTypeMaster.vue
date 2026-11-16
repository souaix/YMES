<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { createXlsxTemplate, parseFirstWorksheetXlsx } from './materialImportWorkbook'

type IssueState = -1 | 0 | 1 | 2
type TypeMode = 'new' | 'edit' | 'copy' | null
type Dialog = 'type' | 'accessories' | 'consumes' | 'properties' | 'import' | null
type ChildMode = 'new' | 'edit' | 'copy' | null
interface EquipmentType {
  equipmentType: string; equipmentClass: string; equipmentClassNo: number; equipmentClassShow: string
  pictureName: string | null; equipmentPicture: string | null; engineerGroupNo: string; capacity: number
  description: string | null; stdUnitEmpTime: number; stdUnitEqpTime: number; countEqpUnitQty: number
  issueState: IssueState; creator: string; createDate: string; reviser: string | null; reviseDate: string | null; dataStamp: number
}
interface Consume { equipmentType: string; consumeNo: string; quantity: number; toleranceQty: number; period: number; tolerancePeriod: number; creator: string; createDate: string }
interface Property { equipment: string; propertyNo: string; elementName: string; defaultValue: string; propertySequence: number; description: string; issueState: number; creator: string; createDate: string; reviser?: string | null; reviseDate?: string | null; dataStamp: number }
interface ImportRow { equipmentType: string; engineerGroupNo: string; capacity: number; equipmentClass: string; description: string; stdUnitEmpTime: number; stdUnitEqpTime: number; countEqpUnitQty: number }
interface ImportResult { equipmentType: string; equipmentClass: string; result: string; message: string }
interface ImportJob { jobId: string; status: 'queued' | 'running' | 'completed' | 'failed' | 'cancelled'; progress: number; results: ImportResult[] }
const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const isMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const keys = {
  types: 'ymes-dev-mock:equipment-types:v1', accessories: 'ymes-dev-mock:equipment-type-accessories:v1',
  consumes: 'ymes-dev-mock:equipment-type-consumes:v1', properties: 'ymes-dev-mock:equipment-type-properties:v1',
  pictures: 'ymes-dev-mock:equipment-type-pictures:v1',
}
const classes = [
  { value: 0, code: 'GENERAL', label: '一般設備' }, { value: 1, code: 'VIRTUAL', label: '虛擬設備' },
  { value: 2, code: 'CHAMBER', label: '腔體設備' }, { value: 3, code: 'TESTER', label: '測試設備' },
  { value: 4, code: 'HANDLER', label: '搬送設備' }, { value: 5, code: 'PROBER', label: '探針設備' }, { value: 6, code: 'MOCVD', label: 'MOCVD' },
]
const states: { value: IssueState; label: string }[] = [{ value: 0, label: '未審核' }, { value: 1, label: '審核中' }, { value: 2, label: '已審核' }, { value: -1, label: '不使用' }]
const seed: EquipmentType[] = [
  { equipmentType: 'DEV-EQP-LATHE', equipmentClass: 'GENERAL', equipmentClassNo: 0, equipmentClassShow: '一般設備', pictureName: null, equipmentPicture: null, engineerGroupNo: 'DEV-EG-01', capacity: -1, description: '開發假資料：通用加工設備類別', stdUnitEmpTime: 0, stdUnitEqpTime: 0, countEqpUnitQty: 1, issueState: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 },
  { equipmentType: 'DEV-EQP-ROBOT', equipmentClass: 'HANDLER', equipmentClassNo: 4, equipmentClassShow: '搬送設備', pictureName: null, equipmentPicture: null, engineerGroupNo: 'DEV-EG-02', capacity: 8, description: '開發假資料：機械手臂設備類別', stdUnitEmpTime: 0.5, stdUnitEqpTime: 1, countEqpUnitQty: 1, issueState: 1, creator: 'DEV001', createDate: '2026-10-15T09:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-16T11:20:00+08:00', dataStamp: 1 },
  { equipmentType: 'DEV-EQP-CHAMBER', equipmentClass: 'CHAMBER', equipmentClassNo: 2, equipmentClassShow: '腔體設備', pictureName: null, equipmentPicture: null, engineerGroupNo: 'DEV-EG-03', capacity: 4, description: '開發假資料：腔體設備類別', stdUnitEmpTime: 0, stdUnitEqpTime: 2.5, countEqpUnitQty: 1, issueState: 2, creator: 'DEV002', createDate: '2026-10-02T10:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-04T10:00:00+08:00', dataStamp: 2 },
]
const accessorySeed = ['DEV-ACC-CLAMP', 'DEV-ACC-FILTER', 'DEV-ACC-SENSOR', 'DEV-ACC-TRAY']
const accessoryLinks: Record<string, string[]> = { 'DEV-EQP-LATHE': ['DEV-ACC-CLAMP', 'DEV-ACC-SENSOR'], 'DEV-EQP-ROBOT': ['DEV-ACC-TRAY'], 'DEV-EQP-CHAMBER': ['DEV-ACC-FILTER'] }
const consumeSeed: Record<string, Consume[]> = { 'DEV-EQP-LATHE': [
  { equipmentType: 'DEV-EQP-LATHE', consumeNo: 'DEV-CUTTER', quantity: 10, toleranceQty: 1, period: 30, tolerancePeriod: 2, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00' },
  { equipmentType: 'DEV-EQP-LATHE', consumeNo: 'DEV-LUBE', quantity: 1, toleranceQty: 0, period: -1, tolerancePeriod: -1, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00' },
] }
const propertySeed: Record<string, Property[]> = { 'DEV-EQP-LATHE': [
  { equipment: 'DEV-EQP-LATHE', propertyNo: 'DEV-TEMP', elementName: '設備溫度', defaultValue: '25', propertySequence: 1, description: '開發假資料屬性', issueState: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', dataStamp: 0 },
] }
const pictureSeed = ['DEV-EQUIPMENT-01.png', 'DEV-EQUIPMENT-02.png', 'DEV-EQUIPMENT-03.png']
const propertyOptions = ref([{ propertyNo: 'DEV-TEMP', elementName: '設備溫度' }, { propertyNo: 'DEV-PRESSURE', elementName: '設備壓力' }, { propertyNo: 'DEV-SPEED', elementName: '設備速度' }, { propertyNo: 'DEV-RUN-MODE', elementName: '運轉模式' }])
const engineerGroupOptions = ref<{ engineerGroupNo: string; engineerGroupName: string }[]>([])
const engineerGroupSearch = ref('')
const selectedEngineerGroupNo = ref('')
const engineerGroupDialogOpen = ref(false)
const rows = ref<EquipmentType[]>([])
const status = ref<IssueState>(0)
const keyword = ref('')
const currentNo = ref('')
const typeSourceNo = ref('')
const typeFormDirty = ref(false)
const checked = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const dialog = ref<Dialog>(null)
const typeMode = ref<TypeMode>(null)
const form = ref({ equipmentType: '', equipmentClassNo: 0, capacity: -1, pictureName: '', equipmentPicture: '', engineerGroupNo: '', description: '', stdUnitEmpTime: 0, stdUnitEqpTime: 0, countEqpUnitQty: 1 })
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const selectedFile = ref<File | null>(null)
const pictureDialogOpen = ref(false)
const pictureNames = ref<string[]>([])
const pictureSearch = ref('')
const selectedLibraryPicture = ref('')
const libraryPictureUrl = ref('')
const current = computed(() => rows.value.find((r) => r.equipmentType === currentNo.value) ?? null)
const visibleRows = computed(() => rows.value.filter((r) => r.issueState === status.value && r.equipmentType.toLowerCase().includes(keyword.value.trim().toLowerCase())).slice().sort((a,b) => a.equipmentType.localeCompare(b.equipmentType)))

const accessoryAll = ref<string[]>([])
const accessorySelected = ref<string[]>([])
const accessorySaved = ref<string[]>([])
const availableChecked = ref<string[]>([])
const assignedChecked = ref<string[]>([])
const searchAvailable = ref('')
const searchAssigned = ref('')
const available = computed(() => accessoryAll.value.filter((x) => !accessorySelected.value.includes(x)).filter((x) => x.toLowerCase().includes(searchAvailable.value.toLowerCase().trim())))
const assigned = computed(() => accessorySelected.value.filter((x) => x.toLowerCase().includes(searchAssigned.value.toLowerCase().trim())))
const accessoryDirty = computed(() => { const a=[...accessorySelected.value].sort(), b=[...accessorySaved.value].sort(); return a.length!==b.length || a.some((x,i)=>x!==b[i]) })
const consumes = ref<Consume[]>([])
const consumeNo = ref('')
const consumeMode = ref<ChildMode>(null)
const consumeForm = ref({ consumeNo: '', quantity: 0, toleranceQty: 0, period: -1, tolerancePeriod: -1 })
const consumeAudit = ref({ creator: '', createDate: '' })
const consumeFormDirty = ref(false)
const properties = ref<Property[]>([])
const propertyNo = ref('')
const propertyMode = ref<ChildMode>(null)
const propertyForm = ref({ propertyNo: '', defaultValue: '', propertySequence: 1, description: '' })
const propertyAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const propertyFormDirty = ref(false)
const importFile = ref<File | null>(null)
const importRows = ref<ImportRow[]>([])
const importResults = ref<ImportResult[]>([])
const importMessage = ref('')
const importProgress = ref(0)
const importRunning = ref(false)
let cancelImportRequested = false
let cancelRequestSent = false
let importJobId = ''
const selectedConsume = computed(() => consumes.value.find((r) => r.consumeNo === consumeNo.value) ?? null)
const selectedProperty = computed(() => properties.value.find((r) => r.propertyNo === propertyNo.value) ?? null)
const filteredPictureNames = computed(() => pictureNames.value.filter((name) => name.toLowerCase().includes(pictureSearch.value.trim().toLowerCase())))
const filteredEngineerGroups = computed(() => engineerGroupOptions.value.filter((row) => (row.engineerGroupNo + ' ' + row.engineerGroupName).toLowerCase().includes(engineerGroupSearch.value.trim().toLowerCase())))

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) { const p = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null; throw new Error(p?.detail || p?.message || p?.title || '設備類別 API 回應錯誤 (' + response.status + ')。') }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function read<T>(key: string, initial: T): T {
  try { const value = localStorage.getItem(key); return value ? JSON.parse(value) as T : structuredClone(initial) } catch { return structuredClone(initial) }
}
function write<T>(key: string, value: T): void { localStorage.setItem(key, JSON.stringify(value)) }
function stateName(value: number): string { return states.find((x) => x.value === value)?.label ?? String(value) }
function className(code: string): string { return classes.find((x) => x.code === code)?.label ?? code }
function dateText(value?: string | null): string { if (!value) return ''; const d=new Date(value); return Number.isNaN(d.valueOf()) ? value : d.toLocaleString('zh-TW') }
async function load(): Promise<void> {
  loading.value=true; error.value=''; notice.value=''
  try {
    if (isMock.value) rows.value=read<EquipmentType[]>(keys.types,seed)
    else { const q=new URLSearchParams(); if(keyword.value.trim()) q.set('equipmentType',keyword.value.trim()); const suffix=q.toString(); const data=await request<EquipmentType[]>('api/equipment/types'+(suffix?'?'+suffix:'')); rows.value=data.map((r)=>({...r,equipmentClassShow:className(r.equipmentClass)})) }
    checked.value=checked.value.filter((x)=>rows.value.some((r)=>r.equipmentType===x))
    currentNo.value=(visibleRows.value.find((r)=>r.equipmentType===currentNo.value)??visibleRows.value[0])?.equipmentType??''
  } catch(e) { rows.value=[]; currentNo.value=''; error.value=e instanceof Error?e.message:'設備類別載入失敗。' }
  finally { loading.value=false }
}
function query(): void { void load() }
function selectRow(no: string): void { currentNo.value=no }
function toggleRow(no: string): void { checked.value=checked.value.includes(no)?checked.value.filter((x)=>x!==no):[...checked.value,no] }
function goHome(): void { window.history.pushState({},'',appBase||'/'); window.dispatchEvent(new PopStateEvent('popstate')) }
function openType(mode: Exclude<TypeMode,null>): void {
  const row=current.value
  if((mode==='edit'||mode==='copy')&&!row) return
  if(mode==='edit'&&row?.issueState!==0) { error.value='只有未審核的設備類別可以修改。'; return }
  dialog.value='type'; typeMode.value=mode; typeSourceNo.value=mode==='new'?'':row!.equipmentType; typeFormDirty.value=false
  const src=mode==='new'?null:row
  form.value=src?{equipmentType:mode==='copy'?'':src.equipmentType,equipmentClassNo:src.equipmentClassNo,capacity:src.capacity,pictureName:src.pictureName??'',equipmentPicture:src.equipmentPicture??'',engineerGroupNo:src.engineerGroupNo,description:src.description??'',stdUnitEmpTime:src.stdUnitEmpTime,stdUnitEqpTime:src.stdUnitEqpTime,countEqpUnitQty:src.countEqpUnitQty}:{equipmentType:'',equipmentClassNo:0,capacity:-1,pictureName:'',equipmentPicture:'',engineerGroupNo:'',description:'',stdUnitEmpTime:0,stdUnitEqpTime:0,countEqpUnitQty:1}
  audit.value=mode==='edit'&&src?{creator:src.creator,createDate:dateText(src.createDate),reviser:props.currentUserNo||'DEV001',reviseDate:new Date().toLocaleString('zh-TW')}:{creator:props.currentUserNo||'DEV001',createDate:new Date().toLocaleString('zh-TW'),reviser:'',reviseDate:''}
  selectedFile.value=null
}
function pictureChanged(e: Event): void {
  const file=(e.target as HTMLInputElement).files?.[0]??null; selectedFile.value=file
  if(!file) return
  form.value.pictureName=file.name.slice(0,50)
  const reader=new FileReader(); reader.onload=()=>form.value.equipmentPicture=String(reader.result??''); reader.readAsDataURL(file)
}
async function openPicturePicker(): Promise<void> {
  try {
    pictureNames.value = isMock.value ? read<string[]>(keys.pictures, pictureSeed) : await request<string[]>('api/equipment/types/pictures')
    pictureSearch.value = ''
    selectedLibraryPicture.value = form.value.pictureName
    pictureDialogOpen.value = true
    if (selectedLibraryPicture.value) await previewLibraryPicture(selectedLibraryPicture.value)
    else libraryPictureUrl.value = ''
  } catch (e) { error.value = e instanceof Error ? e.message : '設備圖片清單載入失敗。' }
}
async function previewLibraryPicture(name: string): Promise<void> {
  selectedLibraryPicture.value = name
  try {
    if (isMock.value) libraryPictureUrl.value = 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="320" height="180"><rect width="100%" height="100%" fill="#f2efdf"/><path d="M35 145h250M70 145V75h180v70M105 75V45h110v30" fill="none" stroke="#897e54" stroke-width="5"/><text x="160" y="168" text-anchor="middle" font-family="sans-serif" font-size="14" fill="#65551a">DEVELOPMENT MOCK</text></svg>')
    else libraryPictureUrl.value = (await request<{ dataUrl: string }>('api/equipment/types/pictures/' + encodeURIComponent(name))).dataUrl
  } catch (e) { libraryPictureUrl.value = ''; error.value = e instanceof Error ? e.message : '設備圖片預覽載入失敗。' }
}
function applyLibraryPicture(): void {
  if (!selectedLibraryPicture.value) return
  form.value.pictureName = selectedLibraryPicture.value
  form.value.equipmentPicture = libraryPictureUrl.value
  selectedFile.value = null
  typeFormDirty.value = true
  pictureDialogOpen.value = false
}
async function openEngineerGroupPicker(): Promise<void> {
  engineerGroupSearch.value = ''
  selectedEngineerGroupNo.value = form.value.engineerGroupNo
  try {
    if (isMock.value) {
      engineerGroupOptions.value = read('ymes-dev-mock:equipment-engineer-groups:v1', [
        { engineerGroupNo: 'DEV-EG-01', engineerGroupName: '開發用設備維護組' },
        { engineerGroupNo: 'DEV-EG-02', engineerGroupName: '開發用公用設備組' },
        { engineerGroupNo: 'DEV-EG-03', engineerGroupName: '開發用設備品保組' },
      ])
    } else engineerGroupOptions.value = await request<{ engineerGroupNo: string; engineerGroupName: string }[]>('api/equipment/engineer-groups')
    engineerGroupDialogOpen.value = true
  } catch (e) { error.value = e instanceof Error ? e.message : '工程群組資料載入失敗。' }
}
function applyEngineerGroup(): void {
  const row = engineerGroupOptions.value.find((x) => x.engineerGroupNo === selectedEngineerGroupNo.value)
  if (!row) return
  form.value.engineerGroupNo = row.engineerGroupNo
  typeFormDirty.value = true
  engineerGroupDialogOpen.value = false
}
async function deleteLibraryPicture(): Promise<void> {
  const name = selectedLibraryPicture.value
  if (!name || !window.confirm('確定刪除圖片 ' + name + '？')) return
  try {
    if (isMock.value) {
      if (rows.value.some((row) => row.pictureName === name)) throw new Error('圖片仍被設備類別引用，不能刪除。')
      write(keys.pictures, read<string[]>(keys.pictures, pictureSeed).filter((item) => item !== name))
      pictureNames.value = read<string[]>(keys.pictures, pictureSeed)
    } else {
      await request<void>('api/equipment/types/pictures/' + encodeURIComponent(name), { method: 'DELETE' })
      pictureNames.value = await request<string[]>('api/equipment/types/pictures')
    }
    selectedLibraryPicture.value = ''
    libraryPictureUrl.value = ''
  } catch (e) { error.value = e instanceof Error ? e.message : '設備圖片刪除失敗。' }
}
function validateType(): string {
  if(!form.value.equipmentType.trim()) return '設備類別編號為必填。'
  if(!form.value.engineerGroupNo.trim()) return '工程群組編號為必填。'
  if(!Number.isInteger(Number(form.value.capacity))||Number(form.value.capacity)<-1||Number(form.value.capacity)>999999) return '產能上限須為 -1 至 999999 的整數。'
  if(!Number.isFinite(Number(form.value.stdUnitEmpTime))||Number(form.value.stdUnitEmpTime)<0) return '標準人時須為大於或等於 0。'
  if(!Number.isFinite(Number(form.value.stdUnitEqpTime))||Number(form.value.stdUnitEqpTime)<0) return '標準機時須為大於或等於 0。'
  if(!Number.isInteger(Number(form.value.countEqpUnitQty))||Number(form.value.countEqpUnitQty)<1) return '機時單位批量須為大於或等於 1 的整數。'
  if(form.value.equipmentType.trim().length>50) return '設備類別編號不可超過 50 個字元。'
  return ''
}
async function saveType(closeAfterSave = true): Promise<void> {
  if(!typeMode.value||busy.value) return
  const problem=validateType(); if(problem) { error.value=problem; return }
  busy.value=true; error.value=''
  const mode=typeMode.value, source=mode==='new'?null:(rows.value.find((r)=>r.equipmentType===typeSourceNo.value)??current.value), no=form.value.equipmentType.trim().toUpperCase()
  try {
    const c=classes.find((x)=>x.value===Number(form.value.equipmentClassNo))??classes[0]
    const body={equipmentType:no,equipmentClass:c.code,equipmentClassNo:c.value,pictureName:form.value.pictureName.trim()||null,engineerGroupNo:form.value.engineerGroupNo.trim(),capacity:Number(form.value.capacity),description:form.value.description.trim()||null,stdUnitEmpTime:Number(form.value.stdUnitEmpTime),stdUnitEqpTime:Number(form.value.stdUnitEqpTime),countEqpUnitQty:Number(form.value.countEqpUnitQty),...(mode==='edit'&&source?{dataStamp:source.dataStamp}:{})}
    let saved: EquipmentType
    if(isMock.value) {
      const data=read<EquipmentType[]>(keys.types,seed), index=data.findIndex((r)=>r.equipmentType===no)
      if(mode!=='edit'&&index>=0) throw new Error('設備類別編號已存在。')
      const now=new Date().toISOString()
      if(mode==='edit') { if(!source||index<0) throw new Error('找不到要修改的設備類別。'); saved={...data[index],...body,equipmentClassShow:c.label,equipmentPicture:form.value.equipmentPicture||null,issueState:0,reviser:props.currentUserNo||'DEV001',reviseDate:now,dataStamp:data[index].dataStamp+1}; data[index]=saved }
      else { saved={...body,equipmentClassShow:c.label,equipmentPicture:form.value.equipmentPicture||null,issueState:0,creator:props.currentUserNo||'DEV001',createDate:now,reviser:null,reviseDate:null,dataStamp:0}; data.push(saved); if(mode==='copy'&&source){const rel=read<Record<string,string[]>>(keys.accessories,accessoryLinks);rel[no]=[...(rel[source.equipmentType]??[])];write(keys.accessories,rel)} }
      write(keys.types,data)
    } else {
      const upload = new FormData()
      upload.append('dto', JSON.stringify(body))
      if (selectedFile.value) upload.append('pictureBody', selectedFile.value)
      if(mode==='edit'&&source) saved=await request<EquipmentType>('api/equipment/types/'+encodeURIComponent(source.equipmentType),{method:'PUT',body:upload})
      else if(mode==='copy'&&source) saved=await request<EquipmentType>('api/equipment/types/'+encodeURIComponent(source.equipmentType)+'/copy',{method:'POST',body:upload})
      else saved=await request<EquipmentType>('api/equipment/types',{method:'POST',body:upload})
    }
    if(closeAfterSave){dialog.value=null;typeMode.value=null} typeFormDirty.value=false; await load(); currentNo.value=saved.equipmentType; audit.value={creator:saved.creator??props.currentUserNo??'',createDate:dateText(saved.createDate),reviser:saved.reviser??'',reviseDate:dateText(saved.reviseDate)}; notice.value=mode==='edit'?'設備類別已修改。':mode==='copy'?'設備類別已複製。':'設備類別已新增。'
  } catch(e) { error.value=e instanceof Error?e.message:'設備類別儲存失敗。' } finally { busy.value=false }
}
async function massAction(action:'delete'|'approve'): Promise<void> {
  const targets=rows.value.filter((r)=>checked.value.includes(r.equipmentType)); if(!targets.length&&current.value) targets.push(current.value)
  const valid=targets.filter((r)=>action==='approve'?r.issueState===0:[0,-1,2].includes(r.issueState))
  if(!valid.length) { error.value=action==='approve'?'只有未審核的設備類別可以核准。':'選取資料狀態不允許刪除。'; return }
  if(!window.confirm('確定'+(action==='approve'?'核准':'刪除')+' '+valid.length+' 筆設備類別？')) return
  busy.value=true
  try {
    if(isMock.value) {
      const data=read<EquipmentType[]>(keys.types,seed)
      if(action==='approve') { for(const t of valid){const r=data.find((x)=>x.equipmentType===t.equipmentType);if(r){r.issueState=2;r.dataStamp++;r.reviser=props.currentUserNo||'DEV001';r.reviseDate=new Date().toISOString()}};write(keys.types,data) }
      else { const gone=new Set(valid.map((x)=>x.equipmentType));write(keys.types,data.filter((x)=>!gone.has(x.equipmentType)));for(const [key,seedValue] of [[keys.accessories,accessoryLinks],[keys.consumes,consumeSeed],[keys.properties,propertySeed]] as const){const map=read<Record<string,unknown>>(key,seedValue as Record<string,unknown>);write(key,Object.fromEntries(Object.entries(map).filter(([name])=>!gone.has(name))))} }
    } else for(const row of valid) { if(action==='approve') await request<void>('api/equipment/types/'+encodeURIComponent(row.equipmentType)+'/approve',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({issueState:row.issueState,dataStamp:row.dataStamp})}); else await request<void>('api/equipment/types/'+encodeURIComponent(row.equipmentType)+'?issueState='+row.issueState+'&dataStamp='+row.dataStamp,{method:'DELETE'}) }
    checked.value=[]; await load(); notice.value='已'+(action==='approve'?'核准':'刪除')+' '+valid.length+' 筆設備類別。'
  } catch(e) { error.value=e instanceof Error?e.message:'設備類別操作失敗。' } finally { busy.value=false }
}
async function openAccessories(): Promise<void> {
  if(!current.value) return
  dialog.value='accessories'; error.value=''
  try { if(isMock.value){accessoryAll.value=[...accessorySeed];accessorySelected.value=[...(read<Record<string,string[]>>(keys.accessories,accessoryLinks)[current.value.equipmentType]??[])]}else{const r=await request<{available:string[];selected:string[]}>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/accessory-types');accessoryAll.value=r.available;accessorySelected.value=[...r.selected]};accessorySaved.value=[...accessorySelected.value] }
  catch(e){error.value=e instanceof Error?e.message:'配件類別載入失敗。'}
}
function moveAccessories(dir:'add'|'remove',all=false):void {
  if(dir==='add'){const next=all?available.value:[...availableChecked.value];accessorySelected.value=[...new Set([...accessorySelected.value,...next])];availableChecked.value=[]}
  else {const next=all?[...assigned.value]:[...assignedChecked.value];accessorySelected.value=accessorySelected.value.filter((x)=>!next.includes(x));assignedChecked.value=[]}
}
async function saveAccessories():Promise<void>{
  if(!current.value||busy.value)return;busy.value=true
  try{if(isMock.value){const d=read<Record<string,string[]>>(keys.accessories,accessoryLinks);d[current.value.equipmentType]=[...accessorySelected.value];write(keys.accessories,d)}else await request<void>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/accessory-types',{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify({accessoryTypes:accessorySelected.value})});accessorySaved.value=[...accessorySelected.value];dialog.value=null;notice.value='配件類別設定已確認。'}
  catch(e){error.value=e instanceof Error?e.message:'配件類別設定儲存失敗。'}finally{busy.value=false}
}
async function openConsumes():Promise<void>{
  if(!current.value)return;dialog.value='consumes';consumeNo.value=''
  try{consumes.value=isMock.value?(read<Record<string,Consume[]>>(keys.consumes,consumeSeed)[current.value.equipmentType]??[]):await request<Consume[]>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/consumes')}
  catch(e){consumes.value=[];error.value=e instanceof Error?e.message:'耗材資料載入失敗。'}
}
function openConsume(mode:Exclude<ChildMode,null>):void{
  const row=selectedConsume.value;if((mode==='edit'||mode==='copy')&&!row)return;consumeMode.value=mode;consumeFormDirty.value=false
  consumeForm.value=row&&mode!=='new'?{consumeNo:row.consumeNo,quantity:row.quantity,toleranceQty:row.toleranceQty,period:row.period,tolerancePeriod:row.tolerancePeriod}:{consumeNo:'',quantity:0,toleranceQty:0,period:-1,tolerancePeriod:-1}
  consumeAudit.value=mode==='edit'&&row?{creator:row.creator,createDate:dateText(row.createDate)}:{creator:props.currentUserNo||'DEV001',createDate:new Date().toLocaleString('zh-TW')}
}
function periodChanged():void{if(Number(consumeForm.value.period)===-1)consumeForm.value.tolerancePeriod=-1}
async function saveConsume(closeAfterSave=true):Promise<void>{
  if(!current.value||!consumeMode.value||busy.value)return;const f=consumeForm.value,no=f.consumeNo.trim().toUpperCase(),n=[f.quantity,f.toleranceQty,f.period,f.tolerancePeriod].map(Number)
  if(!no){error.value='耗材編號為必填。';return}if(n.some((x)=>!Number.isInteger(x))){error.value='數量與週期欄位必須為整數。';return}if(n[0]<0||n[1]<0||n[2]<-1||n[3]<-1){error.value='數量不可小於 0，週期下限為 -1。';return}if(n[1]>n[0]){error.value='容許數量不可大於使用數量。';return}if(n[2]!==-1&&n[3]===-1){error.value='只有週期為 -1 時，容許週期才可為 -1。';return}
  const mode=consumeMode.value;if(mode!=='edit'&&consumes.value.some((x)=>x.consumeNo===no)){error.value='耗材編號已存在。';return}busy.value=true
  try{const body={consumeNo:no,quantity:n[0],toleranceQty:n[1],period:n[2],tolerancePeriod:n[3]}
    if(isMock.value){const all=read<Record<string,Consume[]>>(keys.consumes,consumeSeed),list=[...(all[current.value.equipmentType]??[])],i=list.findIndex((x)=>x.consumeNo===consumeNo.value),old=i>=0?list[i]:null;const result={equipmentType:current.value.equipmentType,...body,creator:mode==='edit'&&old?old.creator:(props.currentUserNo||'DEV001'),createDate:mode==='edit'&&old?old.createDate:new Date().toISOString()};if(mode==='edit'&&i>=0)list[i]=result;else list.push(result);all[current.value.equipmentType]=list;write(keys.consumes,all)}
    else{const base='api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/consumes';if(mode==='edit')await request<void>(base+'/'+encodeURIComponent(consumeNo.value),{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});else await request<void>(base,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)})}
    if(closeAfterSave)consumeMode.value=null;consumeFormDirty.value=false;await openConsumes();consumeNo.value=no;const saved=selectedConsume.value;if(saved)consumeAudit.value={creator:saved.creator,createDate:dateText(saved.createDate)}
  }catch(e){error.value=e instanceof Error?e.message:'耗材儲存失敗。'}finally{busy.value=false}
}
function copyConsume():void{openConsume('copy')}
async function deleteConsume():Promise<void>{
  if(!current.value||!selectedConsume.value||!window.confirm('確定刪除耗材 '+consumeNo.value+'？'))return
  try{if(isMock.value){const d=read<Record<string,Consume[]>>(keys.consumes,consumeSeed);d[current.value.equipmentType]=(d[current.value.equipmentType]??[]).filter((x)=>x.consumeNo!==consumeNo.value);write(keys.consumes,d)}else await request<void>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/consumes/'+encodeURIComponent(consumeNo.value),{method:'DELETE'});consumeNo.value='';await openConsumes()}catch(e){error.value=e instanceof Error?e.message:'耗材刪除失敗。'}
}
async function openProperties():Promise<void>{
  if(!current.value)return;dialog.value='properties';propertyNo.value=''
  try {
    if (isMock.value) properties.value = (read<Record<string,Property[]>>(keys.properties,propertySeed)[current.value.equipmentType] ?? []).filter((x)=>x.issueState===0)
    else {
      const result = await Promise.all([
        request<Property[]>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/properties?issueState=0'),
        request<{propertyNo:string;elementName:string}[]>('api/system/elements?elementType=1'),
      ])
      properties.value = result[0]
      propertyOptions.value = result[1]
    }
  }
  catch(e){properties.value=[];error.value=e instanceof Error?e.message:'設備屬性載入失敗。'}
}
function openProperty(mode:Exclude<ChildMode,null>):void{
  const r=selectedProperty.value;if((mode==='edit'||mode==='copy')&&!r)return;if(mode==='edit'&&(!r||r.issueState!==0)){error.value='只有未審核的設備屬性可以修改。';return}propertyMode.value=mode;propertyFormDirty.value=false
  propertyForm.value=r&&mode!=='new'?{propertyNo:r.propertyNo,defaultValue:r.defaultValue,propertySequence:r.propertySequence,description:r.description}:{propertyNo:'',defaultValue:'',propertySequence:properties.value.length+1,description:''}
  propertyAudit.value=mode==='edit'&&r?{creator:r.creator,createDate:dateText(r.createDate),reviser:r.reviser??props.currentUserNo??'',reviseDate:dateText(r.reviseDate)}:{creator:props.currentUserNo||'DEV001',createDate:new Date().toLocaleString('zh-TW'),reviser:'',reviseDate:''}
}
async function saveProperty(closeAfterSave=true):Promise<void>{
  if(!current.value||!propertyMode.value||busy.value)return;const f=propertyForm.value,mode=propertyMode.value
  if(!f.propertyNo){error.value='屬性編號為必填。';return}if(!Number.isInteger(Number(f.propertySequence))||Number(f.propertySequence)<0||Number(f.propertySequence)>99){error.value='屬性順序須為 0 至 99 的整數。';return}if(mode!=='edit'&&properties.value.some((x)=>x.propertyNo===f.propertyNo)){error.value='此設備已有相同屬性。';return}
  busy.value=true
  try{const body={propertyNo:f.propertyNo,defaultValue:f.defaultValue,propertySequence:Number(f.propertySequence),description:f.description,...(mode==='edit'&&selectedProperty.value?{dataStamp:selectedProperty.value.dataStamp}:{})}
    if(isMock.value){const all=read<Record<string,Property[]>>(keys.properties,propertySeed),list=[...(all[current.value.equipmentType]??[])],i=list.findIndex((x)=>x.propertyNo===propertyNo.value),old=i>=0?list[i]:null,def=propertyOptions.value.find((x)=>x.propertyNo===f.propertyNo);const result:Property={equipment:current.value.equipmentType,...body,elementName:def?.elementName??f.propertyNo,issueState:0,creator:mode==='edit'&&old?old.creator:(props.currentUserNo||'DEV001'),createDate:mode==='edit'&&old?old.createDate:new Date().toISOString(),reviser:mode==='edit'?(props.currentUserNo||'DEV001'):null,reviseDate:mode==='edit'?new Date().toISOString():null,dataStamp:mode==='edit'?(old?.dataStamp??0)+1:0};if(mode==='edit'&&i>=0)list[i]=result;else list.push(result);all[current.value.equipmentType]=list;write(keys.properties,all)}
    else{const base='api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/properties';if(mode==='edit')await request<void>(base+'/'+encodeURIComponent(propertyNo.value),{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});else await request<void>(base,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)})}
    if(closeAfterSave)propertyMode.value=null;propertyFormDirty.value=false;await openProperties();propertyNo.value=f.propertyNo;const saved=selectedProperty.value;if(saved)propertyAudit.value={creator:saved.creator,createDate:dateText(saved.createDate),reviser:saved.reviser??'',reviseDate:dateText(saved.reviseDate)}
  }catch(e){error.value=e instanceof Error?e.message:'設備屬性儲存失敗。'}finally{busy.value=false}
}
function copyProperty():void{openProperty('copy')}
async function deleteProperty():Promise<void>{
  if(!current.value||!selectedProperty.value||!window.confirm('確定刪除屬性 '+propertyNo.value+'？'))return
  try{if(isMock.value){const d=read<Record<string,Property[]>>(keys.properties,propertySeed);d[current.value.equipmentType]=(d[current.value.equipmentType]??[]).filter((x)=>x.propertyNo!==propertyNo.value);write(keys.properties,d)}else await request<void>('api/equipment/types/'+encodeURIComponent(current.value.equipmentType)+'/properties/'+encodeURIComponent(propertyNo.value)+'?issueState='+selectedProperty.value.issueState+'&dataStamp='+selectedProperty.value.dataStamp,{method:'DELETE'});propertyNo.value='';await openProperties()}catch(e){error.value=e instanceof Error?e.message:'設備屬性刪除失敗。'}
}
const equipmentTypeImportColumns = ['EquipmentType', 'EngineerGroupNo', 'Capacity', 'EquipmentClass', 'Description', 'StdUnitEMPTime', 'StdunitEQPTime', 'CountEQPUnitQty']
function parseEquipmentTypeImportMatrix(matrix: string[][]): ImportRow[] {
  if (matrix.length < 2) throw new Error('Excel 第一個工作表沒有資料列。')
  const rows = matrix.slice(1).map((line, index) => {
    const rowNumber = index + 2
    const readInteger = (column: number, name: string): number => {
      const value = (line[column] ?? '').trim()
      if (!value) return 0
      const number = Number(value)
      if (!Number.isInteger(number)) throw new Error(`第 ${rowNumber} 列 ${name} 必須是整數。`)
      return number
    }
    const equipmentType = (line[0] ?? '').trim().toUpperCase()
    const engineerGroupNo = (line[1] ?? '').trim()
    if (!equipmentType) throw new Error(`第 ${rowNumber} 列 EquipmentType 為必填。`)
    if (!engineerGroupNo) throw new Error(`第 ${rowNumber} 列 EngineerGroupNo 為必填。`)
    return {
      equipmentType,
      engineerGroupNo,
      capacity: readInteger(2, 'Capacity'),
      equipmentClass: (line[3] ?? '').trim(),
      description: (line[4] ?? '').trim(),
      stdUnitEmpTime: readInteger(5, 'StdUnitEMPTime'),
      stdUnitEqpTime: readInteger(6, 'StdunitEQPTime'),
      countEqpUnitQty: readInteger(7, 'CountEQPUnitQty'),
    }
  })
  if (rows.length === 0) throw new Error('Excel 第一個工作表沒有資料列。')
  return rows
}
function uniqueImportResults(rows: ImportRow[]): ImportResult[] {
  const unique = new Map<string, ImportResult>()
  for (const row of rows) {
    const key = `${row.equipmentType}\u0000${row.equipmentClass}`
    if (!unique.has(key)) unique.set(key, { equipmentType: row.equipmentType, equipmentClass: row.equipmentClass, result: 'pending', message: '' })
  }
  return [...unique.values()]
}
function formatImportMessage(row: ImportResult): string {
  if (row.result === 'pending') return row.message
  return row.message ? `${row.result}; ${row.message}` : row.result
}
function openImportDialog(): void {
  importFile.value = null
  importRows.value = []
  importResults.value = []
  importMessage.value = ''
  importProgress.value = 0
  dialog.value = 'import'
}
async function importFileChanged(e:Event):Promise<void>{
  const file=(e.target as HTMLInputElement).files?.[0]??null
  importFile.value=file;importRows.value=[];importResults.value=[];importMessage.value='';importProgress.value=0
  if(!file)return
  const extension=file.name.toLowerCase().split('.').pop()
  try{
    if(extension==='xlsx'){
      importRows.value=parseEquipmentTypeImportMatrix(await parseFirstWorksheetXlsx(file))
      importResults.value=uniqueImportResults(importRows.value)
    }else if(extension==='xls'&&!isMock.value){
      const formData=new FormData();formData.append('file',file)
      const preview=await request<{results:ImportResult[]}>('api/equipment/types/import/preview',{method:'POST',body:formData})
      importResults.value=(preview.results??[]).map((row)=>({...row,result:row.result||'pending'}))
    }else if(extension==='xls'){
      throw new Error('Development 假資料可預覽 .xlsx；舊版 .xls 由正式 API 解析。')
    }else{
      throw new Error('只接受 WinForms 範本格式 .xlsx 或 .xls。')
    }
    importMessage.value='檔案已載入，按 Import 開始逐類別檢查。'
  }catch(e){importFile.value=null;importRows.value=[];importResults.value=[];importMessage.value=e instanceof Error?e.message:'Excel 讀取失敗。'}
}
async function downloadTemplate():Promise<void>{
  try{
    let blob:Blob
    if(isMock.value){
      blob=createXlsxTemplate(equipmentTypeImportColumns,'EquipmentType',[['DEV-EQP-NEW','DEV-EG-01','-1','GENERAL','開發用範例','0','0','1']])
    }else{
      const response=await fetch(apiUrl('api/equipment/types/import/template'),{credentials:'include'})
      if(response.status===401){window.location.assign(appBase+'/');throw new Error('登入狀態已逾期，請重新登入。')}
      if(!response.ok)throw new Error('設備類別範本下載失敗 ('+response.status+')。')
      blob=await response.blob()
    }
    const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='EquipmentTypeTemplate.xlsx';a.click();URL.revokeObjectURL(url)
  }catch(e){importMessage.value=e instanceof Error?e.message:'設備類別範本下載失敗。'}
}
function delay(milliseconds:number):Promise<void>{return new Promise((resolve)=>window.setTimeout(resolve,milliseconds))}
async function cancelImport():Promise<void>{
  if(!importRunning.value)return
  cancelImportRequested=true
  if(cancelRequestSent)return
  if(!isMock.value&&importJobId){
    try{await request<void>('api/equipment/types/import/jobs/'+encodeURIComponent(importJobId)+'/cancel',{method:'POST'});cancelRequestSent=true}
    catch(e){importMessage.value=e instanceof Error?e.message:'取消匯入工作失敗。';return}
  }else if(isMock.value)cancelRequestSent=true
  importMessage.value='已要求取消；目前處理中的類別完成後停止。'
}
async function runImport():Promise<void>{
  if(!importFile.value||!importResults.value.length){importMessage.value='請先選取並載入有效的 Excel 範本。';return}
  busy.value=true;importRunning.value=true;importProgress.value=0;cancelImportRequested=false;cancelRequestSent=false;importJobId=''
  try{
    if(isMock.value){
      const data=read<EquipmentType[]>(keys.types,seed)
      const types=[...new Set(importResults.value.map((row)=>row.equipmentType))]
      for(let index=0;index<types.length;index++){
        if(cancelImportRequested)break
        const type=types[index],groupRows=importRows.value.filter((row)=>row.equipmentType===type)
        let result='success',message='Development 假資料已建立。'
        if(groupRows.length!==1){result='fail';message='Development 簡化模式每個設備類別只處理一列；未寫入。'}
        else if(data.some((row)=>row.equipmentType.toUpperCase()===type)){result='fail';message='設備類別編號已存在。'}
        else{
          const row=groupRows[0],category=classes.find((item)=>item.code.toUpperCase()===row.equipmentClass.toUpperCase())
          if(!row.engineerGroupNo) {result='fail';message='工程群組編號為必填。'}
          else if(!Number.isInteger(row.capacity)||row.capacity< -1||!Number.isInteger(row.countEqpUnitQty)||row.countEqpUnitQty<1||row.stdUnitEmpTime<0||row.stdUnitEqpTime<0){result='fail';message='欄位格式不正確。'}
          else if(!category){result='fail';message='Development 選項中找不到此設備分類；未寫入。'}
          else data.push({equipmentType:type,equipmentClass:category.code,equipmentClassNo:category.value,equipmentClassShow:category.label,pictureName:null,equipmentPicture:null,engineerGroupNo:row.engineerGroupNo,capacity:row.capacity,description:row.description||null,stdUnitEmpTime:row.stdUnitEmpTime,stdUnitEqpTime:row.stdUnitEqpTime,countEqpUnitQty:row.countEqpUnitQty,issueState:0,creator:props.currentUserNo||'DEV001',createDate:new Date().toISOString(),reviser:null,reviseDate:null,dataStamp:0})
        }
        importResults.value=importResults.value.map((row)=>row.equipmentType===type?{...row,result,message} : row)
        write(keys.types,data)
        importProgress.value=Math.round(((index+1)/types.length)*100)
        await delay(180)
      }
      importMessage.value=cancelImportRequested?'匯入已取消；已完成的假資料結果保留。':'本機假資料檢查完成。'
      await load()
    }else{
      const body=new FormData();body.append('file',importFile.value)
      const job=await request<ImportJob>('api/equipment/types/import/jobs',{method:'POST',body})
      importJobId=job.jobId;importProgress.value=job.progress??0;if(job.results?.length)importResults.value=job.results
      if(cancelImportRequested&&(job.status==='queued'||job.status==='running'))await cancelImport()
      let currentJob=job
      while(currentJob.status==='queued'||currentJob.status==='running'){
        await delay(500)
        currentJob=await request<ImportJob>('api/equipment/types/import/jobs/'+encodeURIComponent(job.jobId))
        importProgress.value=currentJob.progress
        if(currentJob.results?.length)importResults.value=currentJob.results
        if(cancelImportRequested&&currentJob.status==='running')await cancelImport()
      }
      if(currentJob.results?.length)importResults.value=currentJob.results
      importMessage.value=currentJob.status==='completed'?'匯入處理完成。':currentJob.status==='cancelled'?'匯入已取消。':'匯入工作失敗。'
      if(currentJob.status==='completed')await load()
    }
  }catch(e){importMessage.value=e instanceof Error?e.message:'匯入失敗。'}
  finally{busy.value=false;importRunning.value=false;importJobId=''}
}
function closeDialog():void{if(importRunning.value)return;if(dialog.value==='accessories'&&accessoryDirty.value&&!window.confirm('配件異動尚未確認，關閉會放棄變更。要繼續嗎？'))return;dialog.value=null;typeMode.value=null;consumeMode.value=null;propertyMode.value=null;engineerGroupDialogOpen.value=false}
onMounted(()=>{void load()})
watch(()=>[props.developmentPreview,props.apiBaseUrl],()=>{void load()})
</script>

<template>
  <section class="eqtype-page">
    <div v-if="isMock" class="eqtype-dev-note">開發假資料模式｜資料只保存在本機瀏覽器；正式環境只呼叫 API。</div>
    <div v-if="error" class="eqtype-message error" role="alert">{{ error }}<button type="button" @click="error = ''">×</button></div>
    <div v-if="notice" class="eqtype-message success" role="status">{{ notice }}<button type="button" @click="notice = ''">×</button></div>
    <section class="eqtype-query">
      <fieldset class="eqtype-states"><legend>設備類別狀態</legend><label v-for="item in states" :key="item.value"><input v-model="status" type="radio" :value="item.value" @change="query" />{{ item.label }}</label></fieldset>
      <label for="eqtype-keyword">設備類別編號</label><input id="eqtype-keyword" v-model.trim="keyword" class="eqtype-keyword" @keyup.enter="query" /><span>%</span><button class="eqtype-query-button" type="button" :disabled="loading" @click="query">查詢</button>
    </section>
    <section class="eqtype-list">
      <div class="eqtype-grid-scroll"><table class="eqtype-table"><thead><tr>
        <th class="eqtype-check"><input type="checkbox" aria-label="全選" :checked="visibleRows.length>0&&visibleRows.every((x)=>checked.includes(x.equipmentType))" @change="checked=($event.target as HTMLInputElement).checked?visibleRows.map((x)=>x.equipmentType):[]" /></th>
        <th>設備類別資料</th><th>設備分類</th><th>圖片名稱</th><th>管理群組</th><th>產能</th><th>標準人時</th><th>標準機時</th><th>機時單位批量</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
      </tr></thead><tbody>
        <tr v-for="row in visibleRows" :key="row.equipmentType" :class="{'selected-row':row.equipmentType===currentNo}" tabindex="0" @click="selectRow(row.equipmentType)" @keydown.enter="selectRow(row.equipmentType)">
          <td class="eqtype-check" @click.stop><input type="checkbox" :checked="checked.includes(row.equipmentType)" :aria-label="'選取 '+row.equipmentType" @change="toggleRow(row.equipmentType)" /></td>
          <td class="eqtype-code">{{ row.equipmentType }}</td><td>{{ row.equipmentClassShow||className(row.equipmentClass) }}</td><td>{{ row.pictureName||'—' }}</td><td>{{ row.engineerGroupNo }}</td><td class="numeric">{{ row.capacity }}<small v-if="row.capacity===-1">（不限制）</small></td><td class="numeric">{{ Number(row.stdUnitEmpTime).toFixed(2) }}</td><td class="numeric">{{ Number(row.stdUnitEqpTime).toFixed(2) }}</td><td class="numeric">{{ row.countEqpUnitQty }}</td><td class="eqtype-desc">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser||'' }}</td><td>{{ row.reviseDate||'' }}</td>
        </tr>
        <tr v-if="!loading&&!visibleRows.length"><td colspan="14" class="eqtype-empty">沒有符合條件的設備類別資料</td></tr>
        <tr v-if="loading&&!visibleRows.length"><td colspan="14" class="eqtype-empty">資料載入中…</td></tr>
      </tbody></table></div>
      <aside class="eqtype-actions" aria-label="設備類別功能">
        <button class="eqtype-action primary" type="button" @click="openType('new')">新增(A)</button><button class="eqtype-action" type="button" :disabled="current?.issueState!==0" @click="openType('edit')">修改(E)</button><button class="eqtype-action" type="button" :disabled="!current" @click="openType('copy')">複製(C)</button>
        <button class="eqtype-action" type="button" :disabled="!checked.length&&current?.issueState!==0" @click="massAction('approve')">核准</button><button class="eqtype-action danger" type="button" :disabled="busy||(!checked.length&&!current)" @click="massAction('delete')">刪除(D)</button>
        <button class="eqtype-action" type="button" :disabled="!current" @click="openProperties">屬性</button><button class="eqtype-action" type="button" :disabled="!current" @click="openAccessories">ACC Type</button><button class="eqtype-action" type="button" :disabled="!current" @click="openConsumes">Consume</button>
        <button class="eqtype-action" type="button" @click="openImportDialog">設備類別匯入</button><button class="eqtype-action" type="button" @click="goHome">關閉</button>
      </aside>
      <footer class="eqtype-footer">資料總筆數 <strong>{{ visibleRows.length }}</strong></footer>
    </section>

    <div v-if="dialog==='type'" class="eqtype-overlay" @click.self="closeDialog"><form class="eqtype-form eqtype-main-form" @input="typeFormDirty=true" @change="typeFormDirty=true" @submit.prevent="saveType(true)">
      <header><strong>設備類別資料定義 ({{ typeMode==='edit'?'EDIT':typeMode==='copy'?'COPY':'ADD' }})</strong><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
      <fieldset><legend>設備類別主檔資料</legend><div class="eqtype-columns"><div class="eqtype-column">
        <label><span>設備類別編號</span><input v-model="form.equipmentType" required maxlength="50" :readonly="typeMode==='edit'" @blur="form.equipmentType=form.equipmentType.toUpperCase()" /></label>
        <label><span>設備分類</span><select v-model.number="form.equipmentClassNo"><option v-for="c in classes" :key="c.value" :value="c.value">{{ c.label }}</option></select></label>
        <label><span>產能上限</span><span class="eqtype-inline"><input v-model.number="form.capacity" type="number" min="-1" max="999999" maxlength="6" step="1" required /><small>(-1 代表不限制)</small></span></label>
        <label class="eqtype-engineer-group"><span>工程群組編號</span><span class="eqtype-picture-name"><input v-model.trim="form.engineerGroupNo" maxlength="20" required readonly /><button type="button" :disabled="busy" @click="openEngineerGroupPicker">...</button></span></label>
        <label class="eqtype-long"><span>說明</span><textarea v-model="form.description" maxlength="255" rows="5" /></label>
      </div><div class="eqtype-column">
        <label><span>圖片名稱</span><span class="eqtype-picture-name"><input v-model.trim="form.pictureName" maxlength="50" readonly /><button type="button" :disabled="busy" @click="openPicturePicker">...</button></span></label><label><span>設備圖片</span><input type="file" accept=".bmp,.jpg,.jpeg,.gif,.png" @change="pictureChanged" /></label>
        <div v-if="form.equipmentPicture" class="eqtype-image-preview"><img :src="form.equipmentPicture" alt="設備類別圖片" /><button type="button" @click="form.equipmentPicture='';form.pictureName='';selectedFile=null;typeFormDirty=true">清除圖片</button></div>
        <label><span>標準人時</span><span class="eqtype-inline"><input v-model.number="form.stdUnitEmpTime" type="number" min="0" step="0.01" /><small>小時</small></span></label>
        <label><span>標準機時</span><span class="eqtype-inline"><input v-model.number="form.stdUnitEqpTime" type="number" min="0" step="0.01" /><small>小時</small></span></label>
        <label><span>機時單位批量</span><input v-model.number="form.countEqpUnitQty" type="number" min="1" step="1" required /></label><p class="eqtype-hint">設備類別可用產能 -1 表示不限制。</p>
      </div></div></fieldset>
      <div class="eqtype-audit"><span>建立人　{{ audit.creator }}</span><span>建立日期　{{ audit.createDate }}</span><span>修改人　{{ audit.reviser }}</span><span>修改日期　{{ audit.reviseDate }}</span></div>
      <footer><button class="eqtype-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="closeDialog">關閉</button><button type="button" :disabled="busy||!typeFormDirty" @click="saveType(false)">套用</button></footer>
    </form></div>

    <div v-if="dialog==='type'&&pictureDialogOpen" class="eqtype-overlay eqtype-nested" @click.self="pictureDialogOpen=false"><section class="eqtype-form eqtype-picture-picker">
      <header><strong>設備圖片選擇</strong><button type="button" aria-label="關閉" @click="pictureDialogOpen=false">×</button></header>
      <div class="eqtype-picture-browser"><div><input v-model.trim="pictureSearch" placeholder="搜尋圖片名稱" /><div class="eqtype-picture-list"><button v-for="name in filteredPictureNames" :key="name" type="button" :class="{'selected-picture':name===selectedLibraryPicture}" @click="previewLibraryPicture(name)" @dblclick="applyLibraryPicture">{{ name }}</button><p v-if="!filteredPictureNames.length">沒有可用圖片</p></div></div><div class="eqtype-library-preview"><img v-if="libraryPictureUrl" :src="libraryPictureUrl" alt="設備圖片預覽" /><span v-else>選取圖片以預覽</span><strong>{{ selectedLibraryPicture }}</strong></div></div>
      <footer><button class="eqtype-confirm" type="button" :disabled="!selectedLibraryPicture" @click="applyLibraryPicture">OK</button><button type="button" :disabled="!selectedLibraryPicture" @click="deleteLibraryPicture">Remove</button><button type="button" @click="pictureDialogOpen=false">Close</button></footer>
    </section></div>

    <div v-if="dialog==='type'&&engineerGroupDialogOpen" class="eqtype-overlay eqtype-nested" @click.self="engineerGroupDialogOpen=false"><section class="eqtype-form eqtype-child-list eqtype-engineer-group-picker">
      <header><strong>工程群組選擇</strong><button type="button" aria-label="關閉" @click="engineerGroupDialogOpen=false">×</button></header>
      <label class="eqtype-picker-search"><span>工程群組編號</span><input v-model.trim="engineerGroupSearch" placeholder="輸入編號或名稱搜尋" @keyup.enter="applyEngineerGroup" /></label>
      <div class="eqtype-child-grid"><table class="eqtype-table"><thead><tr><th>工程群組編號</th><th>工程群組名稱</th></tr></thead><tbody>
        <tr v-for="row in filteredEngineerGroups" :key="row.engineerGroupNo" :class="{'selected-row':row.engineerGroupNo===selectedEngineerGroupNo}" @click="selectedEngineerGroupNo=row.engineerGroupNo" @dblclick="applyEngineerGroup"><td>{{ row.engineerGroupNo }}</td><td>{{ row.engineerGroupName }}</td></tr><tr v-if="!filteredEngineerGroups.length"><td colspan="2" class="eqtype-empty">沒有符合條件的工程群組</td></tr>
      </tbody></table></div><footer><button class="eqtype-confirm" type="button" :disabled="!selectedEngineerGroupNo" @click="applyEngineerGroup">確定</button><button type="button" @click="engineerGroupDialogOpen=false">關閉</button></footer>
    </section></div>

    <div v-if="dialog==='accessories'&&current" class="eqtype-overlay" @click.self="closeDialog"><section class="eqtype-form eqtype-acc-form">
      <header><strong>設備類別配件類別設定 (ACC Type)</strong><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
      <label class="eqtype-readonly"><span>設備類別</span><input :value="current.equipmentType" readonly /></label>
      <div class="eqtype-dual"><section><h3>未設定配件類別</h3><input v-model.trim="searchAvailable" placeholder="搜尋" /><div class="eqtype-options"><label v-for="x in available" :key="x"><input v-model="availableChecked" type="checkbox" :value="x" />{{ x }}</label><p v-if="!available.length">沒有可選項目</p></div></section>
        <div class="eqtype-transfer"><button type="button" @click="moveAccessories('add')">加入 →</button><button type="button" @click="moveAccessories('add',true)">全部加入 ⇒</button><button type="button" @click="moveAccessories('remove')">← 移除</button><button type="button" @click="moveAccessories('remove',true)">⇐ 全部移除</button></div>
        <section><h3>已設定配件類別</h3><input v-model.trim="searchAssigned" placeholder="搜尋" /><div class="eqtype-options"><label v-for="x in assigned" :key="x"><input v-model="assignedChecked" type="checkbox" :value="x" />{{ x }}</label><p v-if="!assigned.length">尚未設定</p></div></section></div>
      <p v-if="accessoryDirty" class="eqtype-hint">配件設定尚未確認。</p><footer><button class="eqtype-confirm" type="button" :disabled="busy" @click="saveAccessories">確認</button><button type="button" @click="closeDialog">關閉</button></footer>
    </section></div>

    <div v-if="dialog==='consumes'&&current" class="eqtype-overlay" @click.self="closeDialog"><section class="eqtype-form eqtype-child-list">
      <header><strong>設備類別耗材設定｜{{ current.equipmentType }}</strong><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
      <div class="eqtype-child-grid"><table class="eqtype-table"><thead><tr><th>耗材編號</th><th>數量</th><th>容許數量</th><th>週期</th><th>容許週期</th><th>建立人</th><th>建立日期</th></tr></thead><tbody>
        <tr v-for="r in consumes" :key="r.consumeNo" :class="{'selected-row':r.consumeNo===consumeNo}" @click="consumeNo=r.consumeNo" @dblclick="openConsume('edit')"><td>{{ r.consumeNo }}</td><td>{{ r.quantity }}</td><td>{{ r.toleranceQty }}</td><td>{{ r.period }}</td><td>{{ r.tolerancePeriod }}</td><td>{{ r.creator }}</td><td>{{ dateText(r.createDate) }}</td></tr><tr v-if="!consumes.length"><td colspan="7" class="eqtype-empty">尚未設定耗材</td></tr>
      </tbody></table></div>
      <div class="eqtype-child-actions"><button class="eqtype-action primary" type="button" @click="openConsume('new')">新增(A)</button><button class="eqtype-action" type="button" :disabled="!selectedConsume" @click="openConsume('edit')">修改(E)</button><button class="eqtype-action" type="button" :disabled="!selectedConsume" @click="copyConsume">複製(C)</button><button class="eqtype-action danger" type="button" :disabled="!selectedConsume" @click="deleteConsume">刪除(D)</button><span></span><button class="eqtype-action" type="button" @click="closeDialog">關閉</button></div>
    </section></div>
    <div v-if="dialog==='consumes'&&consumeMode" class="eqtype-overlay eqtype-nested" @click.self="consumeMode=null"><form class="eqtype-form eqtype-child-form" @input="consumeFormDirty=true" @change="consumeFormDirty=true" @submit.prevent="saveConsume(true)">
      <header><strong>耗材資料定義 ({{ consumeMode==='edit'?'EDIT':consumeMode==='copy'?'COPY':'ADD' }})</strong><button type="button" aria-label="關閉" @click="consumeMode=null">×</button></header><fieldset><legend>耗材資料</legend>
        <label><span>設備類別</span><input :value="current?.equipmentType" readonly /></label><label><span>耗材編號</span><input v-model.trim="consumeForm.consumeNo" required maxlength="25" :readonly="consumeMode==='edit'" @blur="consumeForm.consumeNo=consumeForm.consumeNo.toUpperCase()" /></label>
        <label><span>數量</span><input v-model.number="consumeForm.quantity" type="number" min="0" step="1" required /></label><label><span>容許數量</span><input v-model.number="consumeForm.toleranceQty" type="number" min="0" step="1" required /></label>
        <label><span>週期</span><span class="eqtype-inline"><input v-model.number="consumeForm.period" type="number" min="-1" step="1" required @change="periodChanged" /><small>-1 代表不限</small></span></label><label><span>容許週期</span><input v-model.number="consumeForm.tolerancePeriod" type="number" min="-1" step="1" :disabled="Number(consumeForm.period)===-1" required /></label>
      </fieldset><div class="eqtype-audit"><span>建立人　{{ consumeAudit.creator }}</span><span>建立日期　{{ consumeAudit.createDate }}</span></div><footer><button class="eqtype-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="consumeMode=null">關閉</button><button type="button" :disabled="busy||!consumeFormDirty" @click="saveConsume(false)">套用</button></footer>
    </form></div>

    <div v-if="dialog==='properties'&&current" class="eqtype-overlay" @click.self="closeDialog"><section class="eqtype-form eqtype-child-list">
      <header><strong>設備類別屬性設定｜{{ current.equipmentType }}</strong><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
      <div class="eqtype-child-grid"><table class="eqtype-table"><thead><tr><th>設備類別</th><th>屬性編號</th><th>預設值</th><th>屬性順序</th><th>說明</th><th>狀態</th></tr></thead><tbody>
        <tr v-for="r in properties" :key="r.propertyNo" :class="{'selected-row':r.propertyNo===propertyNo}" @click="propertyNo=r.propertyNo" @dblclick="openProperty('edit')"><td>{{ r.equipment }}</td><td>{{ r.propertyNo }}｜{{ r.elementName }}</td><td>{{ r.defaultValue }}</td><td>{{ r.propertySequence }}</td><td>{{ r.description }}</td><td>{{ stateName(r.issueState) }}</td></tr><tr v-if="!properties.length"><td colspan="6" class="eqtype-empty">尚未設定設備屬性</td></tr>
      </tbody></table></div>
      <div class="eqtype-child-actions"><button class="eqtype-action primary" type="button" @click="openProperty('new')">新增(A)</button><button class="eqtype-action" type="button" :disabled="!selectedProperty" @click="openProperty('edit')">修改(E)</button><button class="eqtype-action" type="button" :disabled="!selectedProperty" @click="copyProperty">複製(C)</button><button class="eqtype-action danger" type="button" :disabled="!selectedProperty" @click="deleteProperty">刪除(D)</button><span></span><button class="eqtype-action" type="button" @click="closeDialog">關閉</button></div>
    </section></div>
    <div v-if="dialog==='properties'&&propertyMode" class="eqtype-overlay eqtype-nested" @click.self="propertyMode=null"><form class="eqtype-form eqtype-child-form" @input="propertyFormDirty=true" @change="propertyFormDirty=true" @submit.prevent="saveProperty(true)">
      <header><strong>屬性資料定義 ({{ propertyMode==='edit'?'EDIT':propertyMode==='copy'?'COPY':'ADD' }})</strong><button type="button" aria-label="關閉" @click="propertyMode=null">×</button></header><fieldset><legend>屬性資料</legend>
        <label><span>設備類別</span><input :value="current?.equipmentType" readonly /></label><label><span>屬性編號</span><select v-model="propertyForm.propertyNo" required :disabled="propertyMode==='edit'"><option value="">請選擇</option><option v-for="x in propertyOptions" :key="x.propertyNo" :value="x.propertyNo">{{ x.propertyNo }}｜{{ x.elementName }}</option></select></label>
        <label><span>預設值</span><input v-model="propertyForm.defaultValue" maxlength="255" /></label><label><span>屬性順序</span><input v-model.number="propertyForm.propertySequence" type="number" min="0" max="99" step="1" /></label><label><span>說明</span><textarea v-model="propertyForm.description" rows="3" maxlength="255" /></label>
      </fieldset><div class="eqtype-audit"><span>建立人　{{ propertyAudit.creator }}</span><span>建立日期　{{ propertyAudit.createDate }}</span><span>修改人　{{ propertyAudit.reviser }}</span><span>修改日期　{{ propertyAudit.reviseDate }}</span></div><footer><button class="eqtype-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="propertyMode=null">關閉</button><button type="button" :disabled="busy||!propertyFormDirty" @click="saveProperty(false)">套用</button></footer>
    </form></div>

    <div v-if="dialog==='import'" class="eqtype-overlay" @click.self="closeDialog"><section class="eqtype-form eqtype-import">
      <header><strong>設備類別資料匯入</strong><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
      <div class="eqtype-import-file"><label for="eqtype-upload">File Path</label><input id="eqtype-upload" type="file" accept=".xlsx,.xls" :disabled="importRunning" @change="importFileChanged" /><button type="button" :disabled="importRunning" @click="downloadTemplate">Template Download</button></div>
      <div class="eqtype-progress" role="progressbar" :aria-valuenow="importProgress" aria-valuemin="0" aria-valuemax="100"><span :style="{width:importProgress+'%'}"></span></div><p v-if="importMessage" class="eqtype-hint" role="status">{{ importMessage }}</p>
      <div class="eqtype-child-grid eqtype-import-grid"><table class="eqtype-table"><thead><tr><th>EquipmentType</th><th>EquipmentClass</th><th>Message</th></tr></thead><tbody>
        <tr v-for="(r,i) in importResults" :key="r.equipmentType+'-'+r.equipmentClass+'-'+i"><td>{{ r.equipmentType }}</td><td>{{ r.equipmentClass }}</td><td>{{ formatImportMessage(r) }}</td></tr><tr v-if="!importResults.length"><td colspan="3" class="eqtype-empty">選取 Excel 檔案後顯示設備類別與處理結果。</td></tr>
      </tbody></table></div><footer><button class="eqtype-confirm" type="button" :disabled="busy||!importFile||!importResults.length" @click="runImport">Import</button><button type="button" :disabled="!importRunning" @click="cancelImport">Cancel</button><button type="button" :disabled="importRunning" @click="closeDialog">Close</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.eqtype-page{min-width:0;padding:10px 14px 24px;background:#fff;color:#30363b;font:12px 'Segoe UI','Microsoft JhengHei',sans-serif}
.eqtype-dev-note,.eqtype-hint{margin:0 0 6px;padding:7px 10px;border:1px solid #ddd2a6;background:#fff9dd;color:#65551a}
.eqtype-message{display:flex;justify-content:space-between;margin:5px 0;padding:7px 10px;border:1px solid}.eqtype-message button{border:0;background:transparent;cursor:pointer}.eqtype-message.error{border-color:#d5a3a3;background:#fff0f0;color:#7a2424}.eqtype-message.success{border-color:#9bc3a3;background:#eef8ef;color:#275d33}
.eqtype-query{display:flex;min-height:50px;align-items:center;gap:8px 12px;margin-bottom:6px;padding:4px 7px;border:1px solid #dedede;white-space:nowrap}
.eqtype-states{display:flex;gap:10px 16px;margin:0;padding:0 4px;border:0}.eqtype-states legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}.eqtype-states label{display:flex;align-items:center;gap:4px}.eqtype-states input{margin:0;accent-color:#555}
.eqtype-keyword{width:min(220px,24vw);height:29px;padding:4px 7px;border:1px solid #888;font:inherit}.eqtype-query-button{min-width:78px;height:29px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);font:inherit;cursor:pointer}
.eqtype-list{display:grid;grid-template-columns:minmax(0,1fr) 120px;grid-template-rows:minmax(300px,1fr) auto;max-height:72vh;overflow:hidden;padding:5px;border:1px solid #c7c7c7}
.eqtype-grid-scroll,.eqtype-child-grid{min-width:0;overflow:auto;border:1px solid #c7c7c7}.eqtype-table{width:100%;border-collapse:collapse;font-size:11px;white-space:nowrap}.eqtype-table th{position:sticky;top:0;z-index:1;height:31px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;color:#26251f;font-weight:500;text-align:left}.eqtype-table td{height:32px;padding:5px 8px;border:1px solid #d0d0d0;line-height:1.25}.eqtype-table tbody tr{cursor:pointer}.eqtype-table tbody tr:hover{background:#f7f6f0}.eqtype-table tbody tr.selected-row{background:#fbf3ce;box-shadow:inset 3px 0 #efa72c}.eqtype-table input[type=checkbox]{width:14px;height:14px;accent-color:#706b51}.eqtype-check{width:28px;text-align:center}.eqtype-code{font-family:'Segoe UI',sans-serif;color:#273b50}.eqtype-desc{max-width:240px;overflow:hidden;text-overflow:ellipsis}.eqtype-table .numeric{text-align:right}.eqtype-table small{display:block;color:#777;font-size:10px}.eqtype-empty{height:58px!important;text-align:center;color:#888}.eqtype-actions{display:flex;grid-column:2;grid-row:1;flex-direction:column;gap:5px;max-height:70vh;overflow:auto;margin:0;padding:4px 0 4px 8px;border-left:1px solid #c7c7c7}.eqtype-action{display:flex;min-height:34px;align-items:center;justify-content:center;padding:4px 6px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;font:inherit;cursor:pointer}.eqtype-action.primary{border-color:#f2a532;background:#ffb33b;color:#fff;font-weight:600}.eqtype-action.danger{color:#7e2929}.eqtype-action:disabled{color:#aaa;cursor:not-allowed}.eqtype-footer{grid-column:1/-1;min-height:24px;padding:5px 3px 0;color:#777}.eqtype-footer strong{margin-left:8px;font-weight:500}
.eqtype-overlay{position:fixed;inset:0;z-index:40;display:grid;place-items:center;padding:14px;background:#0005}.eqtype-nested{z-index:50}.eqtype-form{display:flex;max-height:96vh;flex-direction:column;gap:10px;overflow:auto;padding:12px 16px;border:1px solid #aaa;background:#fff;color:#444;box-shadow:0 10px 35px #0004;font:14px 'Segoe UI','Microsoft JhengHei',sans-serif}.eqtype-form>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}.eqtype-form>header button{width:32px;height:30px;border:1px solid #ccc;background:#fafafa;font-size:20px;cursor:pointer}.eqtype-form fieldset{min-width:0;margin:0;padding:12px;border:1px solid #aaa}.eqtype-form fieldset>legend{padding:0 6px;color:#555}.eqtype-main-form{width:min(1000px,98vw)}.eqtype-columns{display:grid;grid-template-columns:1fr 1fr;gap:28px}.eqtype-column{display:grid;align-content:start;gap:9px}.eqtype-column>label,.eqtype-child-form fieldset>label{display:grid;grid-template-columns:minmax(130px,34%) minmax(0,1fr) auto;align-items:center;gap:7px}.eqtype-form input:not([type=file]),.eqtype-form select,.eqtype-form textarea{width:100%;min-width:0;min-height:31px;padding:4px 7px;border:1px solid #888;border-radius:0;background:#fff;font:inherit}.eqtype-form input:not([type=file]):not([readonly]),.eqtype-form select:not(:disabled){border-color:#b3a765}.eqtype-form input[required],.eqtype-form select[required]{background:#fff1aa}.eqtype-form input[readonly],.eqtype-form input:disabled{background:#eee}.eqtype-inline{display:flex;align-items:center;gap:8px}.eqtype-inline input{max-width:160px}.eqtype-inline small{white-space:nowrap;color:#666}.eqtype-long{align-items:start!important}.eqtype-long textarea{grid-column:2/-1}.eqtype-image-preview{display:flex;align-items:center;gap:10px;margin-left:34%;padding:5px;border:1px solid #ccc}.eqtype-image-preview img{max-width:150px;max-height:100px;object-fit:contain}.eqtype-image-preview button{padding:5px;border:1px solid #ccc;background:#fafafa}.eqtype-hint{margin:0}.eqtype-audit{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;padding:9px;border:1px solid #c7c7c7;color:#777}.eqtype-form>footer{display:flex;justify-content:flex-end;gap:9px}.eqtype-form>footer button{min-width:100px;height:38px;border:1px solid #ccc;background:#fafafa;font:inherit;cursor:pointer}.eqtype-form>footer .eqtype-confirm{border-color:#f0a126;background:#ffa62b;color:#fff}.eqtype-readonly{display:grid;grid-template-columns:130px minmax(0,260px);align-items:center;gap:8px}
.eqtype-acc-form{width:min(790px,96vw)}.eqtype-dual{display:grid;grid-template-columns:1fr 140px 1fr;gap:14px;min-height:320px}.eqtype-dual section{display:flex;min-height:300px;flex-direction:column;gap:8px}.eqtype-dual h3{margin:0;padding:5px 7px;background:#f1f1f1;font-size:13px}.eqtype-options{flex:1;overflow:auto;border:1px solid #aaa}.eqtype-options label{display:flex;gap:7px;padding:5px 8px;border-bottom:1px solid #eee}.eqtype-options input{width:auto!important;min-height:0!important}.eqtype-options p{padding:8px;color:#777}.eqtype-transfer{display:flex;flex-direction:column;justify-content:center;gap:8px}.eqtype-transfer button{min-height:34px;border:1px solid #bbb;background:linear-gradient(#fff,#eee);cursor:pointer}
.eqtype-child-list{width:min(1050px,97vw);min-height:360px}.eqtype-child-grid{flex:1;min-height:200px;max-height:58vh}.eqtype-child-actions{display:flex;align-items:center;gap:6px}.eqtype-child-actions span{flex:1}.eqtype-child-form{width:min(620px,96vw)}.eqtype-child-form fieldset{display:grid;gap:10px}.eqtype-child-form fieldset>label{grid-template-columns:150px minmax(0,1fr)}.eqtype-import{width:min(1100px,97vw);min-height:540px}.eqtype-import-file{display:flex;align-items:center;gap:9px}.eqtype-import-file input[type=file]{flex:1;min-width:0;padding:5px;border:1px solid #bbb}.eqtype-import-file button{min-height:31px;border:1px solid #bbb;background:#f8f8f8;cursor:pointer}.eqtype-progress{height:14px;overflow:hidden;border:1px solid #bbb;background:#eee}.eqtype-progress span{display:block;height:100%;background:#e8bd45;transition:width .2s}.eqtype-import-grid{flex:1;min-height:200px;max-height:58vh}
.eqtype-picture-picker{width:min(760px,96vw)}.eqtype-picture-browser{display:grid;grid-template-columns:minmax(220px,1fr) minmax(220px,1fr);gap:14px;min-height:300px}.eqtype-picture-browser>div:first-child{display:flex;min-width:0;flex-direction:column;gap:8px}.eqtype-picture-list{min-height:240px;max-height:42vh;overflow:auto;border:1px solid #aaa;background:#fff}.eqtype-picture-list button{display:block;width:100%;padding:6px 9px;border:0;border-bottom:1px solid #eee;background:#fff;text-align:left;font:inherit;cursor:pointer}.eqtype-picture-list button:hover{background:#f8f2d8}.eqtype-picture-list button.selected-picture{background:#f5e7a8;box-shadow:inset 3px 0 #c89a36}.eqtype-picture-list p{padding:10px;color:#777}.eqtype-library-preview{display:flex;min-height:280px;flex-direction:column;align-items:center;justify-content:center;gap:10px;padding:10px;border:1px solid #ccc;background:#f8f8f8;color:#777}.eqtype-library-preview img{max-width:100%;max-height:34vh;object-fit:contain}.eqtype-library-preview strong{max-width:100%;overflow-wrap:anywhere;color:#444} .eqtype-engineer-group input[readonly]{background:#fff1aa}.eqtype-main-form .eqtype-picture-name input{background:#fff}.eqtype-picker-search{display:grid;grid-template-columns:150px minmax(0,1fr);align-items:center;gap:8px}.eqtype-engineer-group-picker{width:min(620px,96vw);min-height:300px}.eqtype-child-form .eqtype-audit{grid-template-columns:repeat(2,1fr)}.eqtype-picture-name{display:flex;min-width:0;gap:5px}.eqtype-picture-name input{flex:1}.eqtype-picture-name button{width:34px;border:1px solid #aaa;background:#f7f7f7;cursor:pointer}@media(max-width:1000px){.eqtype-columns{gap:14px}.eqtype-list{grid-template-columns:minmax(0,1fr) 105px}.eqtype-table{min-width:1300px}}@media(max-width:700px){.eqtype-query{flex-wrap:wrap}.eqtype-states{flex-wrap:wrap}.eqtype-list{grid-template-columns:minmax(0,1fr) 88px}.eqtype-action{min-height:32px;padding:4px;font-size:10px}.eqtype-columns,.eqtype-dual{grid-template-columns:1fr}.eqtype-audit{grid-template-columns:1fr 1fr}.eqtype-transfer{flex-direction:row;flex-wrap:wrap}.eqtype-import-file{flex-wrap:wrap}.eqtype-import-file input[type=file]{flex-basis:100%}}
</style>
