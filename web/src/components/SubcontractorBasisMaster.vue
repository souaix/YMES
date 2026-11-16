<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

interface Subcontractor {
  subcontractorNo: string
  subcontractorName: string
  description: string
  erpNo: string
  issueState: number
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}

interface SubcontractorContact {
  subcontractorNo: string
  contactorName: string
  telNo: string
  faxNo: string
  title: string
  address: string
  email: string
  description: string
  creator: string | null
  createDate: string | null
  dataStamp: number | null
  reviser: string | null
  reviseDate: string | null
}

type FormMode = 'add' | 'edit' | 'copy' | null
type IssueFilter = 0 | 1 | 2 | -1
type MasterForm = Pick<Subcontractor, 'subcontractorNo' | 'subcontractorName' | 'description'>
type ContactForm = Pick<SubcontractorContact, 'subcontractorNo' | 'contactorName' | 'telNo' | 'faxNo' | 'title' | 'address' | 'email' | 'description'>

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const masterStorageKey = 'ymes-dev-mock:ent-subcontractor-basis:v1'
const contactStorageKey = 'ymes-dev-mock:ent-subcontractor-contact:v1'

const issueStates: { value: IssueFilter; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const seedMasters: Subcontractor[] = [
  { subcontractorNo: 'DEV-SUB-001', subcontractorName: '開發用外包商甲有限公司', description: '只供 Development 預覽使用', erpNo: 'DEV-ERP-001', issueState: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { subcontractorNo: 'DEV-SUB-002', subcontractorName: '開發用外包商乙股份有限公司', description: '已核准範例', erpNo: '', issueState: 2, creator: 'DEV001', createDate: '2026-11-11T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00' },
  { subcontractorNo: 'DEV-SUB-003', subcontractorName: '開發用外包商丙企業', description: '簽核中範例', erpNo: '', issueState: 1, creator: 'DEV001', createDate: '2026-11-13T08:00:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-14T09:00:00+08:00' },
  { subcontractorNo: 'DEV-SUB-004', subcontractorName: '開發用外包商丁企業', description: '', erpNo: '', issueState: -1, creator: 'DEV001', createDate: '2026-11-14T08:00:00+08:00', dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-15T09:00:00+08:00' },
]
const seedContacts: SubcontractorContact[] = [
  { subcontractorNo: 'DEV-SUB-001', contactorName: 'DEV CONTACT A', telNo: '02-0000-1101', faxNo: '02-0000-1102', title: '窗口', address: '台北市開發路 1 號', email: 'contact.a@example.test', description: 'Development 假資料', creator: 'DEV001', createDate: '2026-11-10T08:10:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
  { subcontractorNo: 'DEV-SUB-001', contactorName: 'DEV CONTACT B', telNo: '03-0000-1103', faxNo: '', title: '工程師', address: '桃園市範例路 2 號', email: 'contact.b@example.test', description: '', creator: 'DEV001', createDate: '2026-11-10T08:20:00+08:00', dataStamp: 1, reviser: 'DEV002', reviseDate: '2026-11-12T09:10:00+08:00' },
  { subcontractorNo: 'DEV-SUB-002', contactorName: 'DEV CONTACT C', telNo: '04-0000-1201', faxNo: '', title: '業務', address: '', email: 'contact.c@example.test', description: '', creator: 'DEV001', createDate: '2026-11-11T08:10:00+08:00', dataStamp: 0, reviser: null, reviseDate: null },
]

const masters = ref<Subcontractor[]>([])
const contacts = ref<SubcontractorContact[]>([])
const filterState = ref<IssueFilter>(0)
const keyword = ref('')
const selectedMasterNos = ref<string[]>([])
const selectedContactNames = ref<string[]>([])
const activeMasterNo = ref('')
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const masterMode = ref<FormMode>(null)
const contactMode = ref<FormMode>(null)
const masterForm = ref<MasterForm>(blankMasterForm())
const contactForm = ref<ContactForm>(blankContactForm())
const masterError = ref('')
const contactError = ref('')
const masterDirty = ref(false)
const contactDirty = ref(false)
const masterAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const contactAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const masterDataStamp = ref<number | null>(null)
const contactDataStamp = ref<number | null>(null)

function blankMasterForm(): MasterForm { return { subcontractorNo: '', subcontractorName: '', description: '' } }
function blankContactForm(): ContactForm { return { subcontractorNo: '', contactorName: '', telNo: '', faxNo: '', title: '', address: '', email: '', description: '' } }
const visibleMasters = computed(() => {
  const term = keyword.value.trim().toLocaleLowerCase()
  return masters.value.filter(row => row.issueState === filterState.value)
    .filter(row => !term || row.subcontractorNo.toLocaleLowerCase().includes(term))
    .sort((a, b) => a.subcontractorNo.localeCompare(b.subcontractorNo))
})
const activeMaster = computed(() => masters.value.find(row => row.subcontractorNo === activeMasterNo.value) ?? null)
const visibleContacts = computed(() => contacts.value.filter(row => row.subcontractorNo === activeMasterNo.value).sort((a, b) => a.contactorName.localeCompare(b.contactorName)))
const allMastersSelected = computed(() => visibleMasters.value.length > 0 && visibleMasters.value.every(row => selectedMasterNos.value.includes(row.subcontractorNo)))
const allContactsSelected = computed(() => visibleContacts.value.length > 0 && visibleContacts.value.every(row => selectedContactNames.value.includes(row.contactorName)))

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('外包商基本資料 API 尚未建立；請依 todolist_homepc.md 完成 ET201 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMasters(): Subcontractor[] {
  try { const value = window.localStorage.getItem(masterStorageKey); return value ? JSON.parse(value) as Subcontractor[] : structuredClone(seedMasters) }
  catch { return structuredClone(seedMasters) }
}
function readContacts(): SubcontractorContact[] {
  try { const value = window.localStorage.getItem(contactStorageKey); return value ? JSON.parse(value) as SubcontractorContact[] : structuredClone(seedContacts) }
  catch { return structuredClone(seedContacts) }
}
function saveMasters(value: Subcontractor[]): void { window.localStorage.setItem(masterStorageKey, JSON.stringify(value)) }
function saveContacts(value: SubcontractorContact[]): void { window.localStorage.setItem(contactStorageKey, JSON.stringify(value)) }
function stateLabel(state: number): string { return issueStates.find(item => item.value === state)?.label ?? '未知' }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date)
}
function masterPath(no: string): string { return `api/ent/subcontractor-basis/${encodeURIComponent(no)}` }
function contactPath(no: string): string { return `${masterPath(no)}/contacts` }

async function loadContacts(no: string): Promise<void> {
  selectedContactNames.value = []
  if (!no) { contacts.value = []; return }
  try {
    const loaded = mockMode.value
      ? readContacts().filter(row => row.subcontractorNo === no)
      : await requestJson<SubcontractorContact[]>(contactPath(no))
    if (activeMasterNo.value === no) contacts.value = loaded
  } catch (error) {
    if (activeMasterNo.value === no) {
      contacts.value = []
      errorMessage.value = error instanceof Error ? error.message : '外包商連絡人載入失敗。'
    }
  }
}
watch(activeMasterNo, no => { void loadContacts(no) })
watch(filterState, () => {
  const visibleNos = new Set(visibleMasters.value.map(row => row.subcontractorNo))
  selectedMasterNos.value = selectedMasterNos.value.filter(no => visibleNos.has(no))
  if (!visibleNos.has(activeMasterNo.value)) {
    const first = visibleMasters.value[0]?.subcontractorNo ?? ''
    activeMasterNo.value = first
    selectedMasterNos.value = first ? [first] : []
  }
})

async function loadMasters(search = ''): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mockMode.value) {
      masters.value = readMasters()
      contacts.value = readContacts()
      if (!window.localStorage.getItem(masterStorageKey)) saveMasters(masters.value)
      if (!window.localStorage.getItem(contactStorageKey)) saveContacts(contacts.value)
    } else {
      const query = new URLSearchParams()
      if (search.trim()) query.set('subcontractorNo', search.trim())
      masters.value = await requestJson<Subcontractor[]>(`api/ent/subcontractor-basis${query.size ? `?${query}` : ''}`)
    }
    const visibleNos = new Set(visibleMasters.value.map(row => row.subcontractorNo))
    selectedMasterNos.value = selectedMasterNos.value.filter(no => visibleNos.has(no))
    if (!visibleNos.has(activeMasterNo.value)) {
      const first = visibleMasters.value[0]?.subcontractorNo ?? ''
      activeMasterNo.value = first
      selectedMasterNos.value = first ? [first] : []
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '外包商資料載入失敗。' }
  finally { loading.value = false }
}

function selectMaster(row: Subcontractor): void { activeMasterNo.value = row.subcontractorNo; selectedMasterNos.value = [row.subcontractorNo] }
function toggleMaster(row: Subcontractor, checked: boolean): void {
  selectedMasterNos.value = checked ? [...new Set([...selectedMasterNos.value, row.subcontractorNo])] : selectedMasterNos.value.filter(no => no !== row.subcontractorNo)
  activeMasterNo.value = row.subcontractorNo
}
function toggleAllMasters(checked: boolean): void {
  selectedMasterNos.value = checked ? [...new Set([...selectedMasterNos.value, ...visibleMasters.value.map(row => row.subcontractorNo)])] : selectedMasterNos.value.filter(no => !visibleMasters.value.some(row => row.subcontractorNo === no))
}
function toggleContact(row: SubcontractorContact, checked: boolean): void {
  selectedContactNames.value = checked ? [...new Set([...selectedContactNames.value, row.contactorName])] : selectedContactNames.value.filter(name => name !== row.contactorName)
}
function toggleAllContacts(checked: boolean): void {
  selectedContactNames.value = checked ? [...new Set([...selectedContactNames.value, ...visibleContacts.value.map(row => row.contactorName)])] : []
}

function openMasterForm(mode: Exclude<FormMode, null>): void {
  errorMessage.value = ''; successMessage.value = ''; masterError.value = ''
  if (mode !== 'add' && !activeMaster.value) { errorMessage.value = '請先選取一筆外包商資料。'; return }
  if (mode === 'edit' && activeMaster.value?.issueState !== 0) { errorMessage.value = '只有未簽核資料可以修改。'; return }
  const row = activeMaster.value
  masterForm.value = mode === 'add' ? blankMasterForm() : { subcontractorNo: row!.subcontractorNo, subcontractorName: row!.subcontractorName, description: row!.description || '' }
  const now = new Date().toISOString()
  masterDataStamp.value = mode === 'edit' ? row!.dataStamp : null
  masterAudit.value = mode === 'edit'
    ? { creator: row!.creator || '', createDate: row!.createDate || '', reviser: props.currentUserNo || 'DEV001', reviseDate: now }
    : { creator: props.currentUserNo || 'DEV001', createDate: now, reviser: '', reviseDate: '' }
  masterMode.value = mode
  masterDirty.value = false
}
function validateMaster(): string {
  const value = masterForm.value
  if (!value.subcontractorNo.trim()) return '請輸入外包商編號。'
  if (!value.subcontractorName.trim()) return '請輸入外包商名稱。'
  if (value.subcontractorNo.trim().length > 20) return '外包商編號不可超過 20 個字元。'
  if (value.subcontractorName.trim().length > 220) return '外包商名稱不可超過 220 個字元。'
  if (value.description.length > 255) return '說明不可超過 255 個字元。'
  return ''
}
async function saveMaster(closeAfterSave: boolean): Promise<void> {
  if (!masterMode.value || busy.value) return
  masterError.value = validateMaster()
  if (masterError.value) return
  busy.value = true
  const mode = masterMode.value
  const payload = { ...masterForm.value, subcontractorNo: masterForm.value.subcontractorNo.trim().toUpperCase(), subcontractorName: masterForm.value.subcontractorName.trim() }
  try {
    let saved: Subcontractor
    if (mockMode.value) {
      const data = readMasters()
      const index = data.findIndex(row => row.subcontractorNo.toUpperCase() === payload.subcontractorNo)
      const now = new Date().toISOString()
      if (mode !== 'edit' && index >= 0) throw new Error('外包商編號已存在。')
      if (mode === 'edit') {
        if (index < 0) throw new Error('找不到要修改的外包商資料。')
        const original = data[index]
        if (original.issueState !== 0) throw new Error('只有未簽核資料可以修改。')
        saved = { ...original, ...payload, dataStamp: (original.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now }
        data[index] = saved
      } else {
        saved = { ...payload, erpNo: '', issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: now, dataStamp: 0, reviser: null, reviseDate: null }
        data.push(saved)
      }
      saveMasters(data)
    } else if (mode === 'edit') {
      const query = new URLSearchParams({ dataStamp: String(masterDataStamp.value ?? 0) })
      saved = await requestJson<Subcontractor>(`${masterPath(payload.subcontractorNo)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved ||= { ...payload, erpNo: activeMaster.value?.erpNo || '', issueState: 0, creator: masterAudit.value.creator, createDate: masterAudit.value.createDate, dataStamp: (masterDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    } else {
      saved = await requestJson<Subcontractor>('api/ent/subcontractor-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved ||= { ...payload, erpNo: '', issueState: 0, creator: masterAudit.value.creator, createDate: masterAudit.value.createDate, dataStamp: 0, reviser: null, reviseDate: null }
    }
    await loadMasters()
    const savedIsVisible = visibleMasters.value.some(row => row.subcontractorNo === saved.subcontractorNo)
    activeMasterNo.value = savedIsVisible ? saved.subcontractorNo : ''
    selectedMasterNos.value = savedIsVisible ? [saved.subcontractorNo] : []
    if (!closeAfterSave) {
      if (mode !== 'edit') masterMode.value = 'edit'
      masterDataStamp.value = saved.dataStamp
      masterForm.value = { subcontractorNo: saved.subcontractorNo, subcontractorName: saved.subcontractorName, description: saved.description || '' }
      masterAudit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || props.currentUserNo || '', reviseDate: saved.reviseDate || new Date().toISOString() }
    }
    masterDirty.value = false
    successMessage.value = mode === 'edit' ? '外包商資料已修改。' : '外包商資料已新增。'
    if (closeAfterSave) masterMode.value = null
  } catch (error) { masterError.value = error instanceof Error ? error.message : '外包商資料儲存失敗。' }
  finally { busy.value = false }
}
function normalizeMasterNo(): void { masterForm.value.subcontractorNo = masterForm.value.subcontractorNo.toUpperCase() }

async function deleteMasters(): Promise<void> {
  const targets = masters.value.filter(row => selectedMasterNos.value.includes(row.subcontractorNo))
  if (!targets.length) { errorMessage.value = '請先選取要刪除的外包商。'; return }
  if (!window.confirm(`確定刪除／停用選取的 ${targets.length} 筆外包商資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0; const skipped: string[] = []
  try {
    for (const row of targets) {
      if (![0, 2, -1].includes(row.issueState)) { skipped.push(`${row.subcontractorNo}（${stateLabel(row.issueState)}）不允許刪除`); continue }
      if (mockMode.value) {
        const data = readMasters()
        if (row.issueState === 2) {
          const index = data.findIndex(item => item.subcontractorNo === row.subcontractorNo)
          if (index >= 0) data[index] = { ...data[index], issueState: -1, dataStamp: (data[index].dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() }
          saveMasters(data)
        } else {
          saveMasters(data.filter(item => item.subcontractorNo !== row.subcontractorNo))
          saveContacts(readContacts().filter(item => item.subcontractorNo !== row.subcontractorNo))
        }
      } else {
        const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`${masterPath(row.subcontractorNo)}?${query}`, { method: 'DELETE' })
      }
      completed++
    }
    selectedMasterNos.value = []; activeMasterNo.value = ''
    await loadMasters()
    successMessage.value = `已處理 ${completed} 筆外包商資料；已核准資料改為不使用，未簽核或不使用資料實體刪除。${skipped.length ? ` 略過：${skipped.join('；')}。` : ''}`
  } catch (error) {
    const message = error instanceof Error ? error.message : '外包商資料刪除失敗。'
    await loadMasters(); errorMessage.value = `${message}${completed ? `（前 ${completed} 筆已完成）` : ''}`
  } finally { busy.value = false }
}
async function approveMasters(): Promise<void> {
  const targets = masters.value.filter(row => selectedMasterNos.value.includes(row.subcontractorNo))
  if (!targets.length) { errorMessage.value = '請先選取要核准的外包商。'; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆外包商資料？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0; const skipped: string[] = []
  try {
    for (const row of targets) {
      if (row.issueState !== 0) { skipped.push(`${row.subcontractorNo}（${stateLabel(row.issueState)}）不允許核准`); continue }
      if (mockMode.value) {
        const data = readMasters(); const index = data.findIndex(item => item.subcontractorNo === row.subcontractorNo)
        if (index >= 0) data[index] = { ...data[index], issueState: 2, dataStamp: (data[index].dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() }
        saveMasters(data)
      } else {
        const query = new URLSearchParams({ dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`${masterPath(row.subcontractorNo)}/approve?${query}`, { method: 'POST' })
      }
      completed++
    }
    await loadMasters()
    successMessage.value = `已送出 ${completed} 筆核准；Development 以直接轉為已簽核模擬。${skipped.length ? ` 略過：${skipped.join('；')}。` : ''}`
  } catch (error) {
    const message = error instanceof Error ? error.message : '外包商核准失敗。'
    await loadMasters(); errorMessage.value = `${message}${completed ? `（前 ${completed} 筆已完成）` : ''}`
  } finally { busy.value = false }
}

function openContactForm(mode: Exclude<FormMode, null>): void {
  if (!activeMaster.value) { errorMessage.value = '請先選取外包商。'; return }
  contactError.value = ''; errorMessage.value = ''; successMessage.value = ''
  const selected = visibleContacts.value.find(row => selectedContactNames.value.includes(row.contactorName))
  if (mode !== 'add' && !selected) { errorMessage.value = '請先選取一筆外包商連絡人。'; return }
  const now = new Date().toISOString()
  if (mode === 'add') {
    contactForm.value = { ...blankContactForm(), subcontractorNo: activeMaster.value.subcontractorNo }
    contactDataStamp.value = null
    contactAudit.value = { creator: props.currentUserNo || 'DEV001', createDate: now, reviser: '', reviseDate: '' }
  } else {
    const row = selected!
    contactForm.value = { subcontractorNo: row.subcontractorNo, contactorName: row.contactorName, telNo: row.telNo || '', faxNo: row.faxNo || '', title: row.title || '', address: row.address || '', email: row.email || '', description: row.description || '' }
    contactDataStamp.value = mode === 'edit' ? row.dataStamp : null
    contactAudit.value = mode === 'edit'
      ? { creator: row.creator || '', createDate: row.createDate || '', reviser: props.currentUserNo || 'DEV001', reviseDate: now }
      : { creator: props.currentUserNo || 'DEV001', createDate: now, reviser: '', reviseDate: '' }
  }
  contactMode.value = mode
  contactDirty.value = false
}
function normalizeContactorName(): void {
  if (contactForm.value.contactorName && !contactForm.value.email) contactForm.value.email = `${contactForm.value.contactorName}@`
  contactForm.value.contactorName = contactForm.value.contactorName.toUpperCase()
  contactDirty.value = true
}
function validateContact(): string {
  const row = contactForm.value
  if (!row.contactorName.trim()) return '請輸入連絡人姓名。'
  const limits: [keyof ContactForm, number, string][] = [
    ['contactorName', 50, '連絡人姓名'], ['telNo', 20, '電話'], ['faxNo', 20, '傳真'], ['title', 20, '職稱'], ['address', 50, '地址'], ['email', 50, 'Email'], ['description', 255, '說明'],
  ]
  for (const [field, max, label] of limits) if (row[field].length > max) return `${label}不可超過 ${max} 個字元。`
  return ''
}
async function saveContact(closeAfterSave: boolean): Promise<void> {
  if (!contactMode.value || busy.value) return
  contactError.value = validateContact()
  if (contactError.value) return
  busy.value = true
  const mode = contactMode.value
  const payload = { ...contactForm.value, contactorName: contactForm.value.contactorName.trim().toUpperCase() }
  try {
    let saved: SubcontractorContact
    if (mockMode.value) {
      const data = readContacts()
      const index = data.findIndex(row => row.subcontractorNo === payload.subcontractorNo && row.contactorName.toUpperCase() === payload.contactorName)
      const now = new Date().toISOString()
      if (mode !== 'edit' && index >= 0) throw new Error('此外包商已存在相同連絡人姓名。')
      if (mode === 'edit') {
        if (index < 0) throw new Error('找不到要修改的連絡人。')
        const original = data[index]
        saved = { ...original, ...payload, dataStamp: (original.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now }
        data[index] = saved
      } else {
        saved = { ...payload, creator: props.currentUserNo || 'DEV001', createDate: now, dataStamp: 0, reviser: null, reviseDate: null }
        data.push(saved)
      }
      saveContacts(data)
    } else if (mode === 'edit') {
      const query = new URLSearchParams({ dataStamp: String(contactDataStamp.value ?? 0) })
      saved = await requestJson<SubcontractorContact>(`${contactPath(payload.subcontractorNo)}/${encodeURIComponent(payload.contactorName)}?${query}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved ||= { ...payload, creator: contactAudit.value.creator, createDate: contactAudit.value.createDate, dataStamp: (contactDataStamp.value ?? 0) + 1, reviser: props.currentUserNo || '', reviseDate: new Date().toISOString() }
    } else {
      saved = await requestJson<SubcontractorContact>(contactPath(payload.subcontractorNo), { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
      saved ||= { ...payload, creator: contactAudit.value.creator, createDate: contactAudit.value.createDate, dataStamp: 0, reviser: null, reviseDate: null }
    }
    await loadContacts(payload.subcontractorNo)
    selectedContactNames.value = [saved.contactorName]
    if (!closeAfterSave) {
      if (mode !== 'edit') contactMode.value = 'edit'
      contactDataStamp.value = saved.dataStamp
      contactForm.value = { subcontractorNo: saved.subcontractorNo, contactorName: saved.contactorName, telNo: saved.telNo || '', faxNo: saved.faxNo || '', title: saved.title || '', address: saved.address || '', email: saved.email || '', description: saved.description || '' }
      contactAudit.value = { creator: saved.creator || '', createDate: saved.createDate || '', reviser: saved.reviser || props.currentUserNo || '', reviseDate: saved.reviseDate || new Date().toISOString() }
    }
    contactDirty.value = false
    successMessage.value = mode === 'edit' ? '外包商連絡人已修改。' : '外包商連絡人已新增。'
    if (closeAfterSave) contactMode.value = null
  } catch (error) { contactError.value = error instanceof Error ? error.message : '外包商連絡人儲存失敗。' }
  finally { busy.value = false }
}
async function deleteContacts(): Promise<void> {
  if (!activeMaster.value || !selectedContactNames.value.length) { errorMessage.value = '請先選取要刪除的外包商連絡人。'; return }
  const targets = visibleContacts.value.filter(row => selectedContactNames.value.includes(row.contactorName))
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆外包商連絡人？`)) return
  busy.value = true; errorMessage.value = ''; successMessage.value = ''; let completed = 0
  try {
    for (const row of targets) {
      if (mockMode.value) saveContacts(readContacts().filter(item => !(item.subcontractorNo === row.subcontractorNo && item.contactorName === row.contactorName)))
      else {
        const query = new URLSearchParams({ dataStamp: String(row.dataStamp ?? 0) })
        await requestJson<void>(`${contactPath(row.subcontractorNo)}/${encodeURIComponent(row.contactorName)}?${query}`, { method: 'DELETE' })
      }
      completed++
    }
    await loadContacts(activeMaster.value.subcontractorNo)
    successMessage.value = `已刪除 ${completed} 筆外包商連絡人。`
  } catch (error) {
    const message = error instanceof Error ? error.message : '外包商連絡人刪除失敗。'
    await loadContacts(activeMaster.value.subcontractorNo); errorMessage.value = `${message}${completed ? `（前 ${completed} 筆已完成）` : ''}`
  } finally { busy.value = false }
}
function mailSelectedContact(): void {
  const row = visibleContacts.value.find(item => selectedContactNames.value.includes(item.contactorName))
  if (row?.email) window.location.href = `mailto:${row.email}`
  else errorMessage.value = row ? '選取的連絡人沒有 Email。' : '請先選取一筆外包商連絡人。'
}
function closeMasterForm(): void { masterMode.value = null; masterError.value = '' }
function closeContactForm(): void { contactMode.value = null; contactError.value = '' }
function closePage(): void { if (window.history.length > 1) window.history.back(); else window.location.assign(appBase + '/') }

onMounted(() => {
  if (mockMode.value) {
    if (!window.localStorage.getItem(masterStorageKey)) saveMasters(structuredClone(seedMasters))
    if (!window.localStorage.getItem(contactStorageKey)) saveContacts(structuredClone(seedContacts))
  }
  void loadMasters()
})
</script>

<template>
  <div class="subcontractor-page">
    <div class="subcontractor-query">
      <div class="subcontractor-state-filter" role="radiogroup" aria-label="簽核狀態">
        <label v-for="state in issueStates" :key="state.value"><input v-model="filterState" type="radio" :value="state.value" />{{ state.label }}</label>
      </div>
      <label for="subcontractor-search">外包商編號</label>
      <input id="subcontractor-search" v-model="keyword" maxlength="20" @keyup.enter="loadMasters(keyword)" />
      <button type="button" :disabled="busy || loading" @click="loadMasters(keyword)">查詢</button>
      <span class="subcontractor-percent">%</span>
    </div>

    <div v-if="mockMode" class="subcontractor-note">Development 假資料｜只儲存在此瀏覽器，不會寫入正式資料庫；簽核以直接核准模擬</div>
    <div v-if="errorMessage" class="subcontractor-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="subcontractor-message success" role="status">{{ successMessage }}</div>

    <section class="subcontractor-panel master-panel">
      <div class="subcontractor-panel-title">外包商</div>
      <div class="subcontractor-panel-body">
        <div class="subcontractor-table-scroll">
          <table class="subcontractor-table master-table">
            <thead><tr>
              <th class="select-col"><input type="checkbox" aria-label="選取所有外包商" :checked="allMastersSelected" @change="toggleAllMasters(($event.target as HTMLInputElement).checked)" /></th>
              <th>外包商編號</th><th>外包商名稱</th><th>說明</th><th>ERP 編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleMasters" :key="row.subcontractorNo" tabindex="0" :aria-selected="activeMasterNo === row.subcontractorNo" :class="{ selected: activeMasterNo === row.subcontractorNo }" @click="selectMaster(row)" @keydown.enter.prevent="selectMaster(row)">
                <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取外包商 ${row.subcontractorNo}`" :checked="selectedMasterNos.includes(row.subcontractorNo)" @change="toggleMaster(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td>{{ row.subcontractorNo }}</td><td>{{ row.subcontractorName }}</td><td>{{ row.description }}</td><td>{{ row.erpNo }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="loading"><td colspan="9" class="subcontractor-empty">資料載入中…</td></tr>
              <tr v-else-if="!visibleMasters.length && !errorMessage"><td colspan="9" class="subcontractor-empty">沒有符合的外包商資料。</td></tr>
            </tbody>
          </table>
        </div>
        <div class="subcontractor-actions">
          <button class="primary" type="button" :disabled="busy" @click="openMasterForm('add')">新增(A)</button>
          <button type="button" :disabled="busy || !activeMaster || activeMaster.issueState !== 0" @click="openMasterForm('edit')">修改(E)</button>
          <button type="button" :disabled="busy || !selectedMasterNos.length" @click="deleteMasters">刪除(D)</button>
          <button type="button" :disabled="busy || !activeMaster" @click="openMasterForm('copy')">複製(C)</button>
          <button type="button" :disabled="busy || !selectedMasterNos.length" @click="approveMasters">核准</button>
        </div>
      </div>
      <footer class="subcontractor-count">資料總筆數 <strong>{{ visibleMasters.length }}</strong></footer>
    </section>

    <section class="subcontractor-panel contact-panel">
      <div class="subcontractor-panel-title">外包商連絡人<span v-if="activeMaster">｜{{ activeMaster.subcontractorNo }} {{ activeMaster.subcontractorName }}</span></div>
      <div class="subcontractor-panel-body">
        <div class="subcontractor-table-scroll">
          <table class="subcontractor-table contact-table">
            <thead><tr>
              <th class="select-col"><input type="checkbox" aria-label="選取所有連絡人" :checked="allContactsSelected" @change="toggleAllContacts(($event.target as HTMLInputElement).checked)" /></th>
              <th>外包商編號</th><th>連絡人姓名</th><th>電話</th><th>傳真</th><th>職稱</th><th>地址</th><th>Email</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
            </tr></thead>
            <tbody>
              <tr v-for="row in visibleContacts" :key="`${row.subcontractorNo}|${row.contactorName}`" tabindex="0" :aria-selected="selectedContactNames.includes(row.contactorName)" :class="{ selected: selectedContactNames.includes(row.contactorName) }" @click="selectedContactNames = [row.contactorName]" @keydown.enter.prevent="selectedContactNames = [row.contactorName]">
                <td class="select-col" @click.stop><input type="checkbox" :aria-label="`選取連絡人 ${row.contactorName}`" :checked="selectedContactNames.includes(row.contactorName)" @change="toggleContact(row, ($event.target as HTMLInputElement).checked)" /></td>
                <td>{{ row.subcontractorNo }}</td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.title }}</td><td>{{ row.address }}</td><td class="email-cell">{{ row.email }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
              </tr>
              <tr v-if="activeMaster && !visibleContacts.length"><td colspan="13" class="subcontractor-empty">沒有此外包商的連絡人資料。</td></tr>
              <tr v-else-if="!activeMaster"><td colspan="13" class="subcontractor-empty">請先選取外包商。</td></tr>
            </tbody>
          </table>
        </div>
        <div class="subcontractor-actions">
          <button class="primary" type="button" :disabled="busy || !activeMaster" @click="openContactForm('add')">新增</button>
          <button type="button" :disabled="busy || !selectedContactNames.length" @click="openContactForm('edit')">修改</button>
          <button type="button" :disabled="busy || !selectedContactNames.length" @click="deleteContacts">刪除</button>
          <button type="button" :disabled="busy || !selectedContactNames.length" @click="openContactForm('copy')">複製</button>
          <button type="button" :disabled="busy || !selectedContactNames.length" @click="mailSelectedContact">Mail</button>
          <button class="subcontractor-close" type="button" @click="closePage">關閉</button>
        </div>
      </div>
      <footer class="subcontractor-count">資料總筆數 <strong>{{ visibleContacts.length }}</strong></footer>
    </section>

    <div v-if="masterMode" class="subcontractor-overlay" @click.self="closeMasterForm">
      <form class="subcontractor-dialog master-dialog" @submit.prevent="saveMaster(true)">
        <header><h2>{{ masterMode === 'edit' ? '修改' : masterMode === 'copy' ? '複製' : '新增' }}外包商資料</h2><button type="button" aria-label="關閉" @click="closeMasterForm">×</button></header>
        <div class="subcontractor-form-scroll"><fieldset><legend>外包商資料</legend><div class="subcontractor-fields">
          <label><span>外包商編號</span><input v-model="masterForm.subcontractorNo" :readonly="masterMode === 'edit'" maxlength="20" required @input="masterDirty = true" @blur="normalizeMasterNo" /></label>
          <label><span>外包商名稱</span><input v-model="masterForm.subcontractorName" maxlength="220" required @input="masterDirty = true" /></label>
          <label class="wide"><span>說明</span><textarea v-model="masterForm.description" maxlength="255" rows="4" @input="masterDirty = true"></textarea></label>
        </div></fieldset>
          <div class="subcontractor-audit"><label>建立人<input :value="masterAudit.creator" readonly /></label><label>建立日期<input :value="formatDate(masterAudit.createDate)" readonly /></label><label>修改人<input :value="masterAudit.reviser" readonly /></label><label>修改日期<input :value="formatDate(masterAudit.reviseDate)" readonly /></label></div>
          <p v-if="masterError" class="subcontractor-message error" role="alert">{{ masterError }}</p>
        </div>
        <footer class="subcontractor-dialog-actions"><button type="button" :disabled="busy" @click="closeMasterForm">關閉</button><button type="button" :disabled="busy || !masterDirty" @click="saveMaster(false)">套用</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>

    <div v-if="contactMode" class="subcontractor-overlay" @click.self="closeContactForm">
      <form class="subcontractor-dialog contact-dialog" @submit.prevent="saveContact(true)">
        <header><h2>{{ contactMode === 'edit' ? '修改' : contactMode === 'copy' ? '複製' : '新增' }}外包商連絡人</h2><button type="button" aria-label="關閉" @click="closeContactForm">×</button></header>
        <div class="subcontractor-form-scroll"><fieldset><legend>外包商連絡人資料</legend><div class="subcontractor-fields">
          <label><span>外包商編號</span><input v-model="contactForm.subcontractorNo" readonly /></label>
          <label><span>連絡人姓名</span><input v-model="contactForm.contactorName" :readonly="contactMode === 'edit'" maxlength="50" required @input="contactDirty = true" @blur="normalizeContactorName" /></label>
          <label><span>電話</span><input v-model="contactForm.telNo" maxlength="20" @input="contactDirty = true" /></label>
          <label><span>傳真</span><input v-model="contactForm.faxNo" maxlength="20" @input="contactDirty = true" /></label>
          <label><span>職稱</span><input v-model="contactForm.title" maxlength="20" @input="contactDirty = true" /></label>
          <label><span>Email</span><input v-model="contactForm.email" maxlength="50" @input="contactDirty = true" /></label>
          <label class="wide"><span>地址</span><input v-model="contactForm.address" maxlength="50" @input="contactDirty = true" /></label>
          <label class="wide"><span>說明</span><textarea v-model="contactForm.description" maxlength="255" rows="4" @input="contactDirty = true"></textarea></label>
        </div></fieldset>
          <div class="subcontractor-audit"><label>建立人<input :value="contactAudit.creator" readonly /></label><label>建立日期<input :value="formatDate(contactAudit.createDate)" readonly /></label><label>修改人<input :value="contactAudit.reviser" readonly /></label><label>修改日期<input :value="formatDate(contactAudit.reviseDate)" readonly /></label></div>
          <p v-if="contactError" class="subcontractor-message error" role="alert">{{ contactError }}</p>
        </div>
        <footer class="subcontractor-dialog-actions"><button type="button" :disabled="busy" @click="closeContactForm">關閉</button><button type="button" :disabled="busy || !contactDirty" @click="saveContact(false)">套用</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.subcontractor-page input[type=radio],.subcontractor-page input[type=checkbox]{accent-color:#b59a00}.subcontractor-page{display:flex;min-height:0;height:100%;flex-direction:column;gap:7px;padding:8px;background:#f8f7f3;color:#333;font-size:13px}.subcontractor-query{display:flex;min-height:40px;align-items:center;gap:8px;padding:5px 10px;border:1px solid #d0cec7;background:#fff}.subcontractor-state-filter{display:flex;align-items:center;gap:12px;margin-right:10px}.subcontractor-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}.subcontractor-query>label{margin-left:2px;white-space:nowrap}.subcontractor-query>input{width:190px;height:27px;padding:4px 6px;border:1px solid #aaa}.subcontractor-query>button,.subcontractor-actions button,.subcontractor-dialog-actions button{min-height:29px;padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}.subcontractor-query>button:disabled,.subcontractor-actions button:disabled,.subcontractor-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}.subcontractor-percent{margin-left:-6px;color:#777}.subcontractor-panel{display:flex;min-height:180px;flex-direction:column;border:1px solid #c7c4bb;background:#fff}.master-panel{flex:1.1}.contact-panel{flex:1}.subcontractor-panel-title{flex:none;padding:5px 10px;border-bottom:1px solid #d5d2c9;background:#faf9f6;color:#4b4942;font-weight:600}.subcontractor-panel-title span{font-weight:400;color:#777}.subcontractor-panel-body{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 92px;gap:8px;padding:7px}.subcontractor-table-scroll{min-width:0;overflow:auto}.subcontractor-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.subcontractor-table th{position:sticky;top:0;z-index:1;height:30px;padding:5px 8px;border:1px solid #9cb6c3;background:#56a9df;color:#fff;text-align:left;font-weight:500}.subcontractor-table td{height:29px;max-width:240px;overflow:hidden;padding:5px 8px;border:1px solid #d0d0d0;text-overflow:ellipsis}.subcontractor-table tbody tr{cursor:pointer}.subcontractor-table tbody tr:hover{background:#faf7e7}.subcontractor-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}.subcontractor-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}.select-col{width:27px;text-align:center}.subcontractor-empty{text-align:center;color:#777}.email-cell{color:#325b80;text-decoration:underline}.subcontractor-actions{display:flex;flex-direction:column;gap:5px}.subcontractor-actions .primary,.subcontractor-dialog-actions .primary{border-color:#cf9b33;background:#ffad32;color:#fff;font-weight:600}.subcontractor-close{margin-top:auto}.subcontractor-count{flex:none;padding:2px 12px 7px;color:#777;font-size:11px}.subcontractor-count strong{margin-left:7px;color:#555;font-weight:500}.subcontractor-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.subcontractor-message{margin:0;padding:7px 10px;border:1px solid;font-size:12px}.subcontractor-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}.subcontractor-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}.subcontractor-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:16px;background:rgb(20 20 16 / 35%)}.subcontractor-dialog{display:flex;width:min(570px,100%);max-height:94vh;flex-direction:column;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}.master-dialog{width:min(503px,100%)}.contact-dialog{width:min(496px,100%)}.subcontractor-dialog>header{display:flex;flex:none;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}.subcontractor-dialog>header h2{margin:0;font-size:16px;font-weight:500}.subcontractor-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}.subcontractor-form-scroll{overflow:auto;padding:3px 10px}.subcontractor-dialog fieldset{margin:9px 2px;padding:11px 13px 14px;border:1px solid #c8c5bc}.subcontractor-dialog legend{padding:0 5px;color:#555}.subcontractor-fields{display:grid;grid-template-columns:1fr;gap:9px 18px}.subcontractor-fields>label{display:grid;grid-template-columns:100px minmax(0,1fr);align-items:start;gap:8px}.contact-dialog .subcontractor-fields{grid-template-columns:1fr 1fr}.subcontractor-fields>label>span{padding-top:7px;text-align:right}.subcontractor-fields input,.subcontractor-fields textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;border-radius:0;background:#fff0ae;color:#26251f;font:inherit}.subcontractor-fields textarea{resize:vertical}.subcontractor-fields input:read-only{background:#eee}.subcontractor-fields .wide{grid-column:1/-1}.subcontractor-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:7px;margin:0 2px 9px;padding:9px;border:1px solid #d6d2c7;background:#faf9f6}.subcontractor-audit label{display:grid;gap:4px;color:#666;font-size:11px}.subcontractor-audit input{width:100%;min-height:27px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.subcontractor-dialog-actions{display:flex;flex:none;justify-content:flex-end;gap:8px;padding:8px 12px 12px;border-top:1px solid #ddd}@media(max-width:1000px){.subcontractor-page{height:auto;min-height:100%;overflow:auto}.subcontractor-state-filter{gap:6px;margin-right:2px}.subcontractor-query{flex-wrap:wrap}.subcontractor-panel{min-height:280px}.subcontractor-panel-body{grid-template-columns:minmax(0,1fr)}.subcontractor-actions{flex-direction:row;flex-wrap:wrap}.subcontractor-close{margin:0 0 0 auto}.contact-dialog .subcontractor-fields,.subcontractor-audit{grid-template-columns:1fr}.subcontractor-fields .wide{grid-column:auto}.subcontractor-fields>label{grid-template-columns:100px minmax(0,1fr)}}
</style>
