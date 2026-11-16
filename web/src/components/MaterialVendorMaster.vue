<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface Vendor { vendorNo: string; vendorName: string; issueState: number; description: string | null; creator: string | null; createDate: string | null; erpNo: string | null; dataStamp: number | null; reviser: string | null; reviseDate: string | null }
interface Contactor { vendorNo: string; contactorName: string; telNo: string | null; faxNo: string | null; title: string | null; address: string | null; email: string | null; description: string | null; dataStamp: number | null; creator: string | null; createDate: string | null; reviser: string | null; reviseDate: string | null }
interface MockData { vendors: Vendor[]; contactors: Contactor[] }
type State = 0 | 1 | 2 | -1
type Mode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const api = 'api/material-vendors'
const storageKey = 'ymes-dev-mock:mtl-material-vendor:v1'
const states: { value: State; label: string }[] = [{ value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' }]
const seed: MockData = {
  vendors: [
    { vendorNo: 'DEV-MV-001', vendorName: '開發用物料供應商甲', issueState: 0, description: 'Development 假資料', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', erpNo: 'ERP-DEV-001', dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-02T09:00:00+08:00' },
    { vendorNo: 'DEV-MV-002', vendorName: '開發用物料供應商乙', issueState: 2, description: '已核准範例', creator: 'DEV001', createDate: '2026-11-03T08:00:00+08:00', erpNo: null, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-04T09:00:00+08:00' },
    { vendorNo: 'DEV-MV-003', vendorName: '開發用物料供應商丙', issueState: -1, description: '不使用範例', creator: 'DEV001', createDate: '2026-11-05T08:00:00+08:00', erpNo: null, dataStamp: 1, reviser: 'DEV001', reviseDate: '2026-11-06T09:00:00+08:00' },
  ],
  contactors: [
    { vendorNo: 'DEV-MV-001', contactorName: '林開發', telNo: '02-0000-1001', faxNo: '02-0000-1002', title: '業務', address: '開發用地址', email: 'dev.vendor@example.invalid', description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:10:00+08:00', reviser: null, reviseDate: null },
    { vendorNo: 'DEV-MV-001', contactorName: '陳開發', telNo: '02-0000-1003', faxNo: null, title: '客服', address: null, email: 'service@example.invalid', description: null, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:20:00+08:00', reviser: null, reviseDate: null },
    { vendorNo: 'DEV-MV-002', contactorName: '王供應', telNo: '03-0000-2001', faxNo: null, title: '窗口', address: '開發用地址二', email: 'sales@example.invalid', description: null, dataStamp: 1, creator: 'DEV001', createDate: '2026-11-03T08:10:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-04T09:10:00+08:00' },
  ],
}

const rows = ref<Vendor[]>([])
const contacts = ref<Contactor[]>([])
const issueState = ref<State>(0)
const vendorQuery = ref('')
const activeNo = ref('')
const activeContactName = ref('')
const selectedNos = ref<string[]>([])
const anchorNo = ref('')
const selectedContactNames = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const notice = ref('')

const vendorMode = ref<Mode>(null)
const vendorSource = ref<Vendor | null>(null)
const vendorForm = ref({ vendorNo: '', vendorName: '', description: '' })
const vendorAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const vendorError = ref('')
const contactMode = ref<Mode>(null)
const contactSource = ref<Contactor | null>(null)
const contactForm = ref({ vendorNo: '', contactorName: '', telNo: '', faxNo: '', title: '', address: '', email: '', description: '' })
const contactAudit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const contactError = ref('')

const activeVendor = computed(() => rows.value.find(row => row.vendorNo === activeNo.value) ?? null)
const activeContact = computed(() => contacts.value.find(row => row.contactorName === activeContactName.value) ?? null)
const vendorRows = computed(() => rows.value.slice().sort((a, b) => a.vendorNo.localeCompare(b.vendorNo)))
const contactRows = computed(() => contacts.value.slice().sort((a, b) => a.contactorName.localeCompare(b.contactorName)))
const vendorTitle = computed(() => '物料供應商基本資料（' + (vendorMode.value === 'add' ? '新增' : vendorMode.value === 'copy' ? '複製' : '修改') + '）')
const contactTitle = computed(() => '供應商連絡人（' + (contactMode.value === 'add' ? '新增' : contactMode.value === 'copy' ? '複製' : '修改') + '）')

function freshMock(): MockData { return structuredClone(seed) }
function readMock(): MockData {
  try { const value = window.localStorage.getItem(storageKey); if (!value) return freshMock(); const parsed = JSON.parse(value) as Partial<MockData>; return { vendors: parsed.vendors ?? structuredClone(seed.vendors), contactors: parsed.contactors ?? structuredClone(seed.contactors) } }
  catch { return freshMock() }
}
function writeMock(value: MockData): void { window.localStorage.setItem(storageKey, JSON.stringify(value)) }
function now(): string { return new Date().toISOString() }
function enc(value: string): string { return encodeURIComponent(value) }
function stampQuery(value: number | null): string { return value === null ? 'dataStamp=0&dataStampIsNull=true' : 'dataStamp=' + enc(String(value)) }
function url(path: string): string { const base = props.apiBaseUrl.trim(); if (!base) throw new Error('目前環境尚未設定 API 位址。'); return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString() }
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(url(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) { const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null; if (response.status === 404) throw new Error('物料供應商 API 尚未建立；請依 todolist_homepc.md 完成 MTL ML101。'); throw new Error(problem?.detail || problem?.message || problem?.title || ('API 回應錯誤 (' + response.status + ')。')) }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function dateText(value: string | null): string { if (!value) return ''; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(date) }
function stateText(value: number): string { return states.find(item => item.value === value)?.label ?? String(value) }

async function loadContacts(vendorNo: string): Promise<void> {
  if (!vendorNo) { contacts.value = []; activeContactName.value = ''; return }
  contacts.value = mock.value ? readMock().contactors.filter(row => row.vendorNo === vendorNo) : await request<Contactor[]>(api + '/' + enc(vendorNo) + '/contactors')
  activeContactName.value = contacts.value[0]?.contactorName ?? ''
  selectedContactNames.value = []
}
async function loadVendors(): Promise<void> {
  loading.value = true; errorMessage.value = ''; notice.value = ''
  try {
    if (mock.value) { const q = vendorQuery.value.trim().toLowerCase(); rows.value = readMock().vendors.filter(row => row.issueState === issueState.value && (!q || row.vendorNo.toLowerCase().includes(q))) }
    else { const q = new URLSearchParams({ issueState: String(issueState.value) }); if (vendorQuery.value.trim()) q.set('vendorNo', vendorQuery.value.trim()); rows.value = await request<Vendor[]>(api + '?' + q.toString()) }
    selectedNos.value = []; anchorNo.value = ''; activeNo.value = rows.value[0]?.vendorNo ?? ''; await loadContacts(activeNo.value)
  } catch (error) { rows.value = []; contacts.value = []; activeNo.value = ''; errorMessage.value = error instanceof Error ? error.message : '物料供應商資料載入失敗。' }
  finally { loading.value = false }
}
async function chooseVendor(row: Vendor, event: MouseEvent): Promise<void> {
  const i = vendorRows.value.findIndex(item => item.vendorNo === row.vendorNo)
  if (event.shiftKey && anchorNo.value) { const a = vendorRows.value.findIndex(item => item.vendorNo === anchorNo.value); if (a >= 0) selectedNos.value = vendorRows.value.slice(Math.min(a, i), Math.max(a, i) + 1).map(item => item.vendorNo) }
  else if (event.ctrlKey || event.metaKey) selectedNos.value = selectedNos.value.includes(row.vendorNo) ? selectedNos.value.filter(x => x !== row.vendorNo) : [...selectedNos.value, row.vendorNo]
  else selectedNos.value = [row.vendorNo]
  anchorNo.value = row.vendorNo; activeNo.value = row.vendorNo
  try { await loadContacts(row.vendorNo) } catch (error) { contacts.value = []; errorMessage.value = error instanceof Error ? error.message : '供應商連絡人載入失敗。' }
}
function toggleVendor(row: Vendor, checked: boolean): void { selectedNos.value = checked ? [...new Set([...selectedNos.value, row.vendorNo])] : selectedNos.value.filter(x => x !== row.vendorNo); activeNo.value = row.vendorNo; anchorNo.value = row.vendorNo; void loadContacts(row.vendorNo) }
function selectContact(row: Contactor): void { activeContactName.value = row.contactorName; selectedContactNames.value = [row.contactorName] }
function targetVendors(): Vendor[] { const keys = selectedNos.value.length ? selectedNos.value : activeNo.value ? [activeNo.value] : []; return keys.map(key => rows.value.find(row => row.vendorNo === key)).filter((row): row is Vendor => Boolean(row)) }
function targetContacts(): Contactor[] { const keys = selectedContactNames.value.length ? selectedContactNames.value : activeContactName.value ? [activeContactName.value] : []; return keys.map(key => contacts.value.find(row => row.contactorName === key)).filter((row): row is Contactor => Boolean(row)) }

function openVendor(mode: Exclude<Mode, null>): void {
  const row = activeVendor.value
  if ((mode === 'edit' || mode === 'copy') && !row) return
  if (mode === 'edit' && row?.issueState !== 0) { errorMessage.value = '只有未簽核的供應商可以修改。'; return }
  vendorMode.value = mode; vendorSource.value = mode === 'edit' ? row : null; vendorError.value = ''
  vendorForm.value = row && mode !== 'add' ? { vendorNo: row.vendorNo, vendorName: row.vendorName, description: row.description ?? '' } : { vendorNo: '', vendorName: '', description: '' }
  vendorAudit.value = mode === 'edit' && row ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '' } : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeVendor(): void { vendorMode.value = null; vendorSource.value = null; vendorError.value = '' }
async function saveVendor(closeAfter: boolean): Promise<void> {
  if (!vendorMode.value || busy.value) return
  vendorForm.value.vendorNo = vendorForm.value.vendorNo.trim().toUpperCase(); vendorForm.value.vendorName = vendorForm.value.vendorName.trim(); vendorForm.value.description = vendorForm.value.description.trim(); vendorError.value = ''
  if (!vendorForm.value.vendorNo || !vendorForm.value.vendorName) { vendorError.value = '供應商編號與名稱為必填。'; return }
  if (vendorForm.value.vendorNo.length > 20 || vendorForm.value.vendorName.length > 220 || vendorForm.value.description.length > 255) { vendorError.value = '欄位長度超過資料庫限制。'; return }
  busy.value = true
  try {
    const mode = vendorMode.value; const payload = { vendorNo: vendorForm.value.vendorNo, vendorName: vendorForm.value.vendorName, description: vendorForm.value.description }; let saved: Vendor | null = null
    if (mock.value) { const data = readMock(); if (data.vendors.some(row => row.vendorNo.toLowerCase() === payload.vendorNo.toLowerCase() && !(mode === 'edit' && vendorSource.value?.vendorNo === row.vendorNo))) throw new Error('相同供應商編號已存在。'); const t = now(); saved = mode === 'edit' && vendorSource.value ? { ...vendorSource.value, ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: t, dataStamp: (vendorSource.value.dataStamp ?? 0) + 1 } : { ...payload, issueState: 0, creator: props.currentUserNo || 'DEV001', createDate: t, erpNo: null, dataStamp: 0, reviser: null, reviseDate: null }; if (mode === 'edit' && vendorSource.value) data.vendors = data.vendors.map(row => row.vendorNo === vendorSource.value?.vendorNo ? saved! : row); else data.vendors.push(saved); writeMock(data) }
    else if (mode === 'edit' && vendorSource.value) await request(api + '/' + enc(vendorSource.value.vendorNo) + '?' + stampQuery(vendorSource.value.dataStamp), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    else await request(api, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    await loadVendors(); const current = rows.value.find(row => row.vendorNo === payload.vendorNo) ?? saved
    if (closeAfter) closeVendor(); else if (current) { vendorMode.value = 'edit'; vendorSource.value = current; vendorForm.value = { vendorNo: current.vendorNo, vendorName: current.vendorName, description: current.description ?? '' }; vendorAudit.value = { creator: current.creator ?? '', createDate: current.createDate ?? '', reviser: current.reviser ?? '', reviseDate: current.reviseDate ?? '' } }
  } catch (error) { vendorError.value = error instanceof Error ? error.message : '物料供應商儲存失敗。' }
  finally { busy.value = false }
}
async function deleteVendors(): Promise<void> {
  const targets = targetVendors(); if (!targets.length) return
  if (targets.some(row => row.issueState === 1)) { errorMessage.value = '簽核中的供應商不可刪除。'; return }
  if (!window.confirm('確定處理 ' + targets.length + ' 筆供應商？已核准資料將設為不使用。')) return
  busy.value = true; let done = 0
  try { for (const row of targets) { if (mock.value) { const data = readMock(); if (row.issueState === 2) data.vendors = data.vendors.map(item => item.vendorNo === row.vendorNo ? { ...item, issueState: -1, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now() } : item); else { data.vendors = data.vendors.filter(item => item.vendorNo !== row.vendorNo); data.contactors = data.contactors.filter(item => item.vendorNo !== row.vendorNo) } writeMock(data) } else await request(api + '/' + enc(row.vendorNo) + '?issueState=' + row.issueState + '&' + stampQuery(row.dataStamp), { method: 'DELETE' }); done++ } await loadVendors() }
  catch (error) { const message = '已完成 ' + done + ' 筆；刪除停止：' + (error instanceof Error ? error.message : '刪除失敗。'); await loadVendors(); errorMessage.value = message }
  finally { busy.value = false }
}
async function approveVendors(): Promise<void> {
  const targets = targetVendors(); if (!targets.length) return
  if (targets.some(row => row.issueState !== 0)) { errorMessage.value = '只有未簽核資料可以核准。'; return }
  if (!window.confirm('確定核准 ' + targets.length + ' 筆物料供應商？')) return
  busy.value = true; let done = 0
  try { for (const row of targets) { if (mock.value) { const data = readMock(); data.vendors = data.vendors.map(item => item.vendorNo === row.vendorNo ? { ...item, issueState: 2, dataStamp: (item.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: now() } : item); writeMock(data) } else await request(api + '/' + enc(row.vendorNo) + '/approve?' + stampQuery(row.dataStamp), { method: 'POST' }); done++ } await loadVendors() }
  catch (error) { const message = '已完成 ' + done + ' 筆；核准停止：' + (error instanceof Error ? error.message : '核准失敗。'); await loadVendors(); errorMessage.value = message }
  finally { busy.value = false }
}

function openContact(mode: Exclude<Mode, null>): void {
  const row = activeContact.value; if (!activeVendor.value || ((mode === 'edit' || mode === 'copy') && !row)) return
  contactMode.value = mode; contactSource.value = mode === 'edit' ? row : null; contactError.value = ''
  contactForm.value = row && mode !== 'add' ? { vendorNo: row.vendorNo, contactorName: row.contactorName, telNo: row.telNo ?? '', faxNo: row.faxNo ?? '', title: row.title ?? '', address: row.address ?? '', email: row.email ?? '', description: row.description ?? '' } : { vendorNo: activeVendor.value.vendorNo, contactorName: '', telNo: '', faxNo: '', title: '', address: '', email: '', description: '' }
  contactAudit.value = mode === 'edit' && row ? { creator: row.creator ?? '', createDate: row.createDate ?? '', reviser: row.reviser ?? '', reviseDate: row.reviseDate ?? '' } : { creator: props.currentUserNo || 'DEV001', createDate: now(), reviser: '', reviseDate: '' }
}
function closeContact(): void { contactMode.value = null; contactSource.value = null; contactError.value = '' }
async function saveContact(): Promise<void> {
  if (!contactMode.value || busy.value) return
  const f = contactForm.value; f.contactorName = f.contactorName.trim(); f.telNo = f.telNo.trim(); f.faxNo = f.faxNo.trim(); f.title = f.title.trim(); f.address = f.address.trim(); f.email = f.email.trim(); f.description = f.description.trim(); contactError.value = ''
  if (!f.vendorNo || !f.contactorName) { contactError.value = '供應商編號與連絡人姓名為必填。'; return }
  const limits: [keyof typeof f, number, string][] = [['contactorName', 50, '連絡人姓名'], ['telNo', 20, '電話'], ['faxNo', 20, '傳真'], ['title', 20, '職稱'], ['address', 50, '地址'], ['email', 50, 'Email'], ['description', 255, '說明']]
  const long = limits.find(([key, max]) => f[key].length > max); if (long) { contactError.value = long[2] + '不可超過 ' + long[1] + ' 個字元。'; return }
  busy.value = true
  try {
    const mode = contactMode.value; const payload = { contactorName: f.contactorName, telNo: f.telNo, faxNo: f.faxNo, title: f.title, address: f.address, email: f.email, description: f.description }
    if (mock.value) { const data = readMock(); if (data.contactors.some(row => row.vendorNo === f.vendorNo && row.contactorName.toLowerCase() === f.contactorName.toLowerCase() && !(mode === 'edit' && contactSource.value?.contactorName === row.contactorName))) throw new Error('相同供應商已存在此連絡人。'); const t = now(); const saved: Contactor = mode === 'edit' && contactSource.value ? { ...contactSource.value, ...payload, reviser: props.currentUserNo || 'DEV001', reviseDate: t, dataStamp: (contactSource.value.dataStamp ?? 0) + 1 } : { vendorNo: f.vendorNo, ...payload, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createDate: t, reviser: null, reviseDate: null }; if (mode === 'edit' && contactSource.value) data.contactors = data.contactors.map(row => row.vendorNo === f.vendorNo && row.contactorName === contactSource.value?.contactorName ? saved : row); else data.contactors.push(saved); writeMock(data) }
    else if (mode === 'edit' && contactSource.value) await request(api + '/' + enc(f.vendorNo) + '/contactors/' + enc(contactSource.value.contactorName) + '?' + stampQuery(contactSource.value.dataStamp), { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    else await request(api + '/' + enc(f.vendorNo) + '/contactors', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) })
    await loadContacts(f.vendorNo); closeContact()
  } catch (error) { contactError.value = error instanceof Error ? error.message : '連絡人資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteContacts(): Promise<void> {
  const targets = targetContacts(); if (!targets.length || !window.confirm('確定刪除選取的 ' + targets.length + ' 筆連絡人？')) return
  busy.value = true; let done = 0
  try { for (const row of targets) { if (mock.value) { const data = readMock(); data.contactors = data.contactors.filter(item => !(item.vendorNo === row.vendorNo && item.contactorName === row.contactorName)); writeMock(data) } else await request(api + '/' + enc(row.vendorNo) + '/contactors/' + enc(row.contactorName) + '?' + stampQuery(row.dataStamp), { method: 'DELETE' }); done++ } await loadContacts(activeNo.value) }
  catch (error) { const message = '已完成 ' + done + ' 筆；刪除停止：' + (error instanceof Error ? error.message : '刪除失敗。'); await loadContacts(activeNo.value); errorMessage.value = message }
  finally { busy.value = false }
}
function mailContact(): void { const email = activeContact.value?.email?.trim(); if (email) window.location.href = 'mailto:' + email }
function autoEmail(): void { if (contactForm.value.contactorName && !contactForm.value.email) contactForm.value.email = contactForm.value.contactorName + '@' }

onMounted(() => { void loadVendors() })
</script>

<template>
  <section class="material-vendor-page">
    <header class="page-title"><h1>物料供應商基本資料</h1></header>
    <div class="search-strip">
      <div class="issue-states"><label v-for="state in states" :key="state.value"><input v-model.number="issueState" type="radio" :value="state.value" @change="loadVendors"><span>{{ state.label }}</span></label></div>
      <label class="query-field"><span>物料供應商編號</span><input v-model="vendorQuery" class="kd-input" @keyup.enter="loadVendors"></label><button class="win-btn" :disabled="loading" @click="loadVendors">{{ loading ? '查詢中' : '查詢' }}</button>
    </div>
    <p v-if="errorMessage" class="message error">{{ errorMessage }}</p><p v-else-if="notice" class="message">{{ notice }}</p>
    <main class="panels">
      <section class="grid-panel vendor-panel">
        <div class="panel-heading"><h2>物料供應商資料</h2><span>{{ stateText(issueState) }}</span></div>
        <div class="panel-body"><div class="grid-scroll"><table><thead><tr><th class="check"></th><th>供應商編號</th><th>供應商名稱</th><th>說明</th><th>ERP 編號</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
          <tr v-for="row in vendorRows" :key="row.vendorNo" :class="{ active: activeNo === row.vendorNo, selected: selectedNos.includes(row.vendorNo) }" @click="chooseVendor(row, $event)" @dblclick="openVendor('edit')"><td class="check"><input type="checkbox" :checked="selectedNos.includes(row.vendorNo)" @click.stop @change="toggleVendor(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.vendorNo }}</td><td>{{ row.vendorName }}</td><td>{{ row.description }}</td><td>{{ row.erpNo }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td></tr>
          <tr v-if="!loading && !vendorRows.length"><td colspan="9" class="empty">沒有符合條件的供應商</td></tr>
        </tbody></table></div><aside class="actions"><button class="win-btn primary" :disabled="busy" @click="openVendor('add')">新增(A)</button><button class="win-btn" :disabled="busy || activeVendor?.issueState !== 0" @click="openVendor('edit')">修改(E)</button><button class="win-btn" :disabled="busy || !activeVendor" @click="openVendor('copy')">複製(C)</button><button class="win-btn danger" :disabled="busy || !activeVendor || issueState === 1" @click="deleteVendors">刪除(D)</button><button class="win-btn" :disabled="busy || !activeVendor || issueState !== 0" @click="approveVendors">核准</button></aside></div>
        <footer class="panel-footer">資料總筆數：{{ vendorRows.length }}</footer>
      </section>
      <section class="grid-panel contact-panel">
        <div class="panel-heading"><h2>物料供應商連絡人</h2><span>{{ activeVendor ? activeVendor.vendorNo + '　' + activeVendor.vendorName : '請先選取供應商' }}</span></div>
        <div class="panel-body"><div class="grid-scroll"><table><thead><tr><th class="check"></th><th>連絡人姓名</th><th>電話</th><th>傳真</th><th>職稱</th><th>地址</th><th>Email</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody>
          <tr v-for="row in contactRows" :key="row.vendorNo + ':' + row.contactorName" :class="{ active: activeContactName === row.contactorName, selected: selectedContactNames.includes(row.contactorName) }" @click="selectContact(row)" @dblclick="openContact('edit')"><td class="check"><input type="checkbox" :checked="selectedContactNames.includes(row.contactorName)" @click.stop="selectContact(row)" @change="selectedContactNames = ($event.target as HTMLInputElement).checked ? [...new Set([...selectedContactNames, row.contactorName])] : selectedContactNames.filter(name => name !== row.contactorName)"></td><td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.title }}</td><td>{{ row.address }}</td><td><a v-if="row.email" :href="'mailto:' + row.email">{{ row.email }}</a></td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ dateText(row.reviseDate) }}</td></tr>
          <tr v-if="!loading && !contactRows.length"><td colspan="12" class="empty">{{ activeVendor ? '此供應商尚無連絡人。' : '請先選取供應商。' }}</td></tr>
        </tbody></table></div><aside class="actions"><button class="win-btn primary" :disabled="busy || !activeVendor" @click="openContact('add')">新增</button><button class="win-btn" :disabled="busy || !activeContact" @click="openContact('edit')">修改</button><button class="win-btn" :disabled="busy || !activeContact" @click="openContact('copy')">複製</button><button class="win-btn danger" :disabled="busy || !activeContact" @click="deleteContacts">刪除</button><button class="win-btn" :disabled="!activeContact?.email" @click="mailContact">Mail</button></aside></div>
        <footer class="panel-footer">連絡人筆數：{{ contactRows.length }}</footer>
      </section>
    </main>
    <footer class="status-line"><span>{{ activeVendor ? '目前供應商：' + activeVendor.vendorNo : '未選取供應商' }}</span><span>使用者：{{ currentUserNo || '—' }}</span><button class="win-btn" @click="emit('close')">關閉</button></footer>

    <div v-if="vendorMode" class="backdrop" @click.self="closeVendor"><form class="editor vendor-editor" @submit.prevent="saveVendor(true)">
      <header class="dialog-title"><h2>{{ vendorTitle }}</h2><button type="button" class="x-button" @click="closeVendor">×</button></header><fieldset class="fields"><legend>物料供應商資料</legend>
        <label><span>供應商編號</span><input v-model="vendorForm.vendorNo" class="kd-input" maxlength="20" :readonly="vendorMode === 'edit'" @blur="vendorForm.vendorNo = vendorForm.vendorNo.trim().toUpperCase()"></label><label><span>供應商名稱</span><input v-model="vendorForm.vendorName" class="kd-input" maxlength="220"></label><label class="wide"><span>說明</span><textarea v-model="vendorForm.description" class="kd-input" maxlength="255"></textarea></label>
      </fieldset><fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="vendorAudit.creator" readonly></label><label>建立日期<input :value="dateText(vendorAudit.createDate)" readonly></label><label>修改人<input :value="vendorAudit.reviser" readonly></label><label>修改日期<input :value="dateText(vendorAudit.reviseDate)" readonly></label></fieldset><p v-if="vendorError" class="message error">{{ vendorError }}</p><footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeVendor">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="saveVendor(false)">套用</button></footer>
    </form></div>

    <div v-if="contactMode" class="backdrop" @click.self="closeContact"><form class="editor contact-editor" @submit.prevent="saveContact">
      <header class="dialog-title"><h2>{{ contactTitle }}</h2><button type="button" class="x-button" @click="closeContact">×</button></header><fieldset class="contact-fields"><legend>供應商連絡人資料</legend>
        <label><span>供應商編號</span><input :value="contactForm.vendorNo" readonly></label><label><span>連絡人姓名</span><input v-model="contactForm.contactorName" class="kd-input" maxlength="50" :readonly="contactMode === 'edit'" @blur="contactForm.contactorName = contactForm.contactorName.trim(); autoEmail()"></label>
        <label><span>電話</span><input v-model="contactForm.telNo" class="kd-input" maxlength="20" @blur="contactForm.telNo = contactForm.telNo.trim()"></label><label><span>傳真</span><input v-model="contactForm.faxNo" class="kd-input" maxlength="20" @blur="contactForm.faxNo = contactForm.faxNo.trim()"></label><label><span>職稱</span><input v-model="contactForm.title" class="kd-input" maxlength="20" @blur="contactForm.title = contactForm.title.trim()"></label><label><span>Email</span><input v-model="contactForm.email" class="kd-input" maxlength="50" @blur="contactForm.email = contactForm.email.trim()"></label><label class="wide"><span>地址</span><input v-model="contactForm.address" class="kd-input" maxlength="50" @blur="contactForm.address = contactForm.address.trim()"></label><label class="wide"><span>說明</span><textarea v-model="contactForm.description" class="kd-input" maxlength="255" @blur="contactForm.description = contactForm.description.trim()"></textarea></label>
      </fieldset><fieldset class="audit"><legend>建立 / 修改資訊</legend><label>建立人<input :value="contactAudit.creator" readonly></label><label>建立日期<input :value="dateText(contactAudit.createDate)" readonly></label><label>修改人<input :value="contactAudit.reviser" readonly></label><label>修改日期<input :value="dateText(contactAudit.reviseDate)" readonly></label></fieldset><p v-if="contactError" class="message error">{{ contactError }}</p><footer class="dialog-actions"><button class="win-btn primary" :disabled="busy">確定</button><button type="button" class="win-btn" :disabled="busy" @click="closeContact">關閉</button><button type="button" class="win-btn" :disabled="busy" @click="saveContact">套用</button></footer>
    </form></div>
  </section>
</template>

<style scoped>
.material-vendor-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#fff;color:#292929;font:14px "Microsoft JhengHei",Arial,sans-serif}.page-title{height:50px;flex:none;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid #ddd8ce;background:#f7f5ef}.page-title h1{margin:0;font-size:20px;font-weight:500}.search-strip{min-height:42px;flex:none;display:flex;align-items:center;gap:12px;padding:5px 12px;border-bottom:1px solid #ccc}.issue-states{display:flex;align-items:center;gap:10px}.issue-states label{display:flex;align-items:center;gap:4px;white-space:nowrap}.issue-states input,.check input{accent-color:#e4a02f}.query-field{display:flex;align-items:center;gap:6px;margin-left:auto}.kd-input{box-sizing:border-box;min-width:0;height:27px;border:1px solid #999;border-radius:0;padding:3px 6px;background:#ffffc0;color:#222;font:inherit}.kd-input:read-only{background:#eee}.win-btn{min-height:29px;padding:3px 10px;border:1px solid #bbb;border-radius:3px;background:linear-gradient(#fff,#f1efeb);font:inherit;white-space:nowrap;cursor:pointer}.win-btn:hover:not(:disabled){border-color:#e39a37;background:#fff8e8}.win-btn:disabled{opacity:.5;cursor:default}.win-btn.primary{border-color:#e69520;background:#f7a52a;color:#fff;font-weight:600}.win-btn.danger{color:#8b3030}.message{margin:5px 12px;padding:6px 9px;border:1px solid #ddd;background:#fff9e7}.message.error{color:#8d2222;background:#fff3f1}.panels{flex:1;min-height:0;display:grid;grid-template-rows:minmax(210px,42%) minmax(210px,58%);gap:7px;padding:7px 12px 5px}.grid-panel{min-height:0;display:flex;flex-direction:column;border:1px solid #aaa;padding:0 7px 4px}.panel-heading{height:27px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 4px;border-bottom:1px solid #ddd}.panel-heading h2{margin:0;font-size:14px;font-weight:500}.panel-heading span{color:#777;font-size:12px}.panel-body{flex:1;min-height:0;display:grid;grid-template-columns:minmax(0,1fr) 88px;gap:8px;padding-top:5px}.grid-scroll{min-width:0;min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}th,td{height:24px;padding:3px 6px;border:1px solid #c9c9c9;text-align:left}th{position:sticky;top:0;z-index:1;background:#e7e5df;font-weight:500}tbody tr{cursor:pointer}tbody tr:hover{background:#fff5d8}tbody tr.active{background:#ffefbf;outline:1px solid #d5a13d;outline-offset:-1px}tbody tr.selected:not(.active){background:#f8f3e6}.check{width:24px;text-align:center;padding:2px}.empty{text-align:center;color:#777;padding:18px}.actions{display:flex;flex-direction:column;gap:6px}.panel-footer{height:20px;flex:none;display:flex;align-items:center;padding:0 3px;color:#777;font-size:11px}.status-line{height:32px;flex:none;display:flex;align-items:center;justify-content:space-between;padding:0 12px;border-top:1px solid #ccc;color:#666;font-size:12px}.backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:10px;background:#0005}.editor{box-sizing:border-box;display:flex;flex-direction:column;border:1px solid #999;background:#f9f8f4;box-shadow:0 8px 28px #0004}.vendor-editor{width:min(550px,96vw);min-height:min(262px,94vh);padding:0 10px 7px}.contact-editor{width:min(752px,96vw);max-height:95vh;overflow:auto;padding:0 10px 7px}.dialog-title{height:36px;flex:none;display:flex;align-items:center;justify-content:space-between;margin:0 -10px 5px;padding:0 10px;border-bottom:1px solid #ccc;background:#f1efe9}.dialog-title h2{margin:0;font-size:15px;font-weight:500}.x-button{border:0;background:transparent;color:#555;font-size:24px;line-height:1;cursor:pointer}.fields{display:flex;flex-direction:column;gap:8px;margin:6px 0;padding:7px;border:1px solid #aaa}.fields legend,.contact-fields legend,.audit legend{padding:0 5px;color:#555}.fields label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:7px}.fields label>span{text-align:right}.fields .wide{align-items:start}.fields textarea,.contact-fields textarea{height:50px;resize:vertical}.contact-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 10px;margin:6px 0;padding:7px;border:1px solid #aaa}.contact-fields label{display:grid;grid-template-columns:72px minmax(0,1fr);align-items:center;gap:5px}.contact-fields label>span{text-align:right}.contact-fields .wide{grid-column:1/-1}.audit{display:grid;grid-template-columns:1fr 1fr;gap:5px 10px;margin:0;padding:5px 7px;border:1px solid #aaa}.audit label{display:grid;grid-template-columns:58px minmax(0,1fr);align-items:center;gap:5px;font-size:11px;white-space:nowrap}.audit input{box-sizing:border-box;width:100%;min-width:0;height:22px;border:1px solid #aaa;background:#eee;padding:2px 5px;font:inherit}.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding:7px 0 1px}@media(max-width:850px){.search-strip{flex-wrap:wrap}.query-field{margin-left:0}.panels{grid-template-rows:minmax(240px,1fr) minmax(260px,1fr)}.panel-body{grid-template-columns:minmax(0,1fr) 76px}.contact-fields{grid-template-columns:1fr}.contact-fields .wide{grid-column:auto}.vendor-editor{min-height:0}.contact-editor{max-height:92vh}}
</style>
