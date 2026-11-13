<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
interface Vendor { vendorNo: string; vendorName: string; description: string | null; issueState: number; erpNo: string | null; dataStamp: number; creator: string | null; createdAt: string | null; reviser: string | null; revisedAt: string | null }
interface Contactor { vendorNo: string; contactorName: string; telNo: string | null; faxNo: string | null; title: string | null; address: string | null; email: string | null; description: string | null; dataStamp: number; creator: string | null; createdAt: string | null; reviser: string | null; revisedAt: string | null }
type VendorMode = 'new' | 'edit' | 'copy' | null
type ContactMode = 'new' | 'edit' | 'copy' | null
const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const usesDevelopmentMock = import.meta.env.DEV
const vendorMockKey = 'ymes-dev-mock:equipment-vendors:v1'
const contactMockKey = 'ymes-dev-mock:equipment-vendor-contactors:v1'
const seedVendors: Vendor[] = [
  { vendorNo: 'DEV-V001', vendorName: '開發用設備供應商甲', description: '僅供 UI 操作展示', issueState: 0, erpNo: null, dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: null, revisedAt: null },
  { vendorNo: 'DEV-V002', vendorName: '開發用設備供應商乙', description: '已核准狀態範例', issueState: 2, erpNo: null, dataStamp: 1, creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-11-03T09:30:00+08:00' },
  { vendorNo: 'DEV-V003', vendorName: '開發用設備供應商丙', description: '停用狀態範例', issueState: -1, erpNo: null, dataStamp: 1, creator: 'DEV001', createdAt: '2026-11-04T08:00:00+08:00', reviser: 'DEV002', revisedAt: '2026-11-05T09:30:00+08:00' },
]
const seedContactors: Contactor[] = [
  { vendorNo: 'DEV-V001', contactorName: '開發聯絡人甲', telNo: '02-0000-0001', faxNo: '', title: '業務', address: '開發用地址', email: 'dev.contact1@example.invalid', description: '', dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: null, revisedAt: null },
  { vendorNo: 'DEV-V001', contactorName: '開發聯絡人乙', telNo: '02-0000-0002', faxNo: '', title: '客服', address: '開發用地址', email: 'dev.contact2@example.invalid', description: '', dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-01T08:00:00+08:00', reviser: null, revisedAt: null },
  { vendorNo: 'DEV-V002', contactorName: '開發聯絡人丙', telNo: '02-0000-0003', faxNo: '', title: '窗口', address: '', email: 'dev.contact3@example.invalid', description: '', dataStamp: 0, creator: 'DEV001', createdAt: '2026-11-02T08:00:00+08:00', reviser: null, revisedAt: null },
]
const vendors = ref<Vendor[]>([])
const contactors = ref<Contactor[]>([])
const selectedVendorNo = ref('')
const selectedContactorName = ref('')
const issueState = ref(0)
const keyword = ref('')
const loading = ref(false)
const contactsLoading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const contactError = ref('')
const successMessage = ref('')
const vendorMode = ref<VendorMode>(null)
const contactMode = ref<ContactMode>(null)
const vendorForm = ref({ vendorNo: '', vendorName: '', description: '', dataStamp: 0 })
const contactForm = ref({ vendorNo: '', contactorName: '', telNo: '', faxNo: '', title: '', address: '', email: '', description: '', dataStamp: 0 })
const vendorAudit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
const contactAudit = ref({ creator: '', createdAt: '', reviser: '', revisedAt: '' })
let contactRequestId = 0
const visibleVendors = computed(() => vendors.value.filter(row => row.issueState === issueState.value && row.vendorNo.toLocaleLowerCase().includes(keyword.value.trim().toLocaleLowerCase())))
const selectedVendor = computed(() => vendors.value.find(row => row.vendorNo === selectedVendorNo.value) ?? null)
const selectedContactor = computed(() => contactors.value.find(row => row.contactorName === selectedContactorName.value) ?? null)
function makeApiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeApiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('設備供應商 API 尚未建立；請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(problem?.detail || problem?.message || problem?.title || 'API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function readMockRows<T>(key: string, seed: T[]): T[] {
  try {
    const saved = window.localStorage.getItem(key)
    return saved ? JSON.parse(saved) as T[] : structuredClone(seed)
  } catch { return structuredClone(seed) }
}
function saveMockRows<T>(key: string, rows: T[]): void { window.localStorage.setItem(key, JSON.stringify(rows)) }
function stateName(state: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(state)] ?? '未知' }
function formatDate(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function nextVendorNo(): string {
  const used = new Set(vendors.value.map(row => row.vendorNo.toUpperCase()))
  let sequence = 1
  while (used.has('DEV-V' + String(sequence).padStart(3, '0'))) sequence++
  return 'DEV-V' + String(sequence).padStart(3, '0')
}
function selectVendor(row: Vendor): void { selectedVendorNo.value = row.vendorNo; selectedContactorName.value = ''; void loadContactors(row.vendorNo) }
function applyFilter(): void {
  if (!visibleVendors.value.some(row => row.vendorNo === selectedVendorNo.value)) {
    const first = visibleVendors.value[0]
    selectedVendorNo.value = first?.vendorNo ?? ''
    selectedContactorName.value = ''
    void loadContactors(selectedVendorNo.value)
  }
}
async function loadVendors(): Promise<void> {
  loading.value = true; errorMessage.value = ''; successMessage.value = ''
  try {
    if (usesDevelopmentMock) {
      vendors.value = readMockRows(vendorMockKey, seedVendors)
      if (!visibleVendors.value.some(row => row.vendorNo === selectedVendorNo.value)) selectedVendorNo.value = visibleVendors.value[0]?.vendorNo ?? ''
      await loadContactors(selectedVendorNo.value)
      return
    }
    vendors.value = await requestJson<Vendor[]>('api/equipment/vendors')
    if (!visibleVendors.value.some(row => row.vendorNo === selectedVendorNo.value)) selectedVendorNo.value = visibleVendors.value[0]?.vendorNo ?? ''
    await loadContactors(selectedVendorNo.value)
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '供應商資料載入失敗。'; vendors.value = []; contactors.value = [] }
  finally { loading.value = false }
}
async function loadContactors(vendorNo: string): Promise<void> {
  const requestId = ++contactRequestId
  contactors.value = []; selectedContactorName.value = ''; contactError.value = ''
  if (!vendorNo) { contactsLoading.value = false; return }
  contactsLoading.value = true
  try {
    const rows = usesDevelopmentMock
      ? readMockRows(contactMockKey, seedContactors).filter(row => row.vendorNo === vendorNo)
      : await requestJson<Contactor[]>('api/equipment/vendors/' + encodeURIComponent(vendorNo) + '/contactors')
    if (requestId === contactRequestId) contactors.value = rows
  } catch (error) { if (requestId === contactRequestId) contactError.value = error instanceof Error ? error.message : '聯絡人資料載入失敗。' }
  finally { if (requestId === contactRequestId) contactsLoading.value = false }
}
function openVendorForm(mode: Exclude<VendorMode, null>): void {
  vendorMode.value = mode
  const row = selectedVendor.value
  vendorForm.value = mode === 'edit' && row
    ? { vendorNo: row.vendorNo, vendorName: row.vendorName, description: row.description ?? '', dataStamp: row.dataStamp }
    : { vendorNo: nextVendorNo(), vendorName: mode === 'copy' && row ? row.vendorName : '', description: mode === 'copy' && row ? row.description ?? '' : '', dataStamp: 0 }
  vendorAudit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createdAt: formatDate(row.createdAt), reviser: row.reviser ?? '', revisedAt: formatDate(row.revisedAt) }
    : { creator: props.currentUserNo ?? '', createdAt: new Date().toLocaleString('zh-TW'), reviser: '', revisedAt: '' }
}
function openContactForm(mode: Exclude<ContactMode, null>): void {
  const vendor = selectedVendor.value
  if (!vendor) return
  const row = selectedContactor.value
  contactMode.value = mode
  contactForm.value = mode === 'edit' && row
    ? { vendorNo: vendor.vendorNo, contactorName: row.contactorName, telNo: row.telNo ?? '', faxNo: row.faxNo ?? '', title: row.title ?? '', address: row.address ?? '', email: row.email ?? '', description: row.description ?? '', dataStamp: row.dataStamp }
    : { vendorNo: vendor.vendorNo, contactorName: '', telNo: mode === 'copy' && row ? row.telNo ?? '' : '', faxNo: mode === 'copy' && row ? row.faxNo ?? '' : '', title: mode === 'copy' && row ? row.title ?? '' : '', address: mode === 'copy' && row ? row.address ?? '' : '', email: mode === 'copy' && row ? row.email ?? '' : '', description: mode === 'copy' && row ? row.description ?? '' : '', dataStamp: 0 }
  contactAudit.value = mode === 'edit' && row
    ? { creator: row.creator ?? '', createdAt: formatDate(row.createdAt), reviser: row.reviser ?? '', revisedAt: formatDate(row.revisedAt) }
    : { creator: props.currentUserNo ?? '', createdAt: new Date().toLocaleString('zh-TW'), reviser: '', revisedAt: '' }
}
async function saveVendor(): Promise<void> {
  if (busy.value || !vendorMode.value) return
  busy.value = true; errorMessage.value = ''
  const mode = vendorMode.value
  try {
    let result: Vendor
    if (usesDevelopmentMock) {
      const rows = readMockRows(vendorMockKey, seedVendors)
      const vendorNo = vendorForm.value.vendorNo.trim()
      const existing = rows.findIndex(row => row.vendorNo.toLowerCase() === vendorNo.toLowerCase())
      if (mode !== 'edit' && existing >= 0) throw new Error('供應商編號已存在。')
      const now = new Date().toISOString()
      if (mode === 'edit') {
        if (existing < 0) throw new Error('找不到要修改的供應商。')
        const original = rows[existing]
        result = { ...original, vendorName: vendorForm.value.vendorName.trim(), description: vendorForm.value.description.trim() || null, dataStamp: original.dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: now }
        rows[existing] = result
      } else {
        result = { vendorNo, vendorName: vendorForm.value.vendorName.trim(), description: vendorForm.value.description.trim() || null, issueState: 0, erpNo: null, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: null, revisedAt: null }
        rows.push(result)
      }
      saveMockRows(vendorMockKey, rows)
    } else {
      const path = mode === 'edit' ? 'api/equipment/vendors/' + encodeURIComponent(vendorForm.value.vendorNo) : 'api/equipment/vendors'
      result = await requestJson<Vendor>(path, { method: mode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(vendorForm.value) })
    }
    vendorMode.value = null; keyword.value = ''; vendors.value = usesDevelopmentMock ? readMockRows(vendorMockKey, seedVendors) : vendors.value
    selectedVendorNo.value = result.vendorNo; issueState.value = result.issueState; await loadContactors(result.vendorNo); successMessage.value = '供應商資料已儲存。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '供應商資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteVendor(): Promise<void> {
  const row = selectedVendor.value
  if (!row || busy.value || !window.confirm('確定刪除供應商 ' + row.vendorNo + '？')) return
  busy.value = true; errorMessage.value = ''
  try {
    if (usesDevelopmentMock) {
      saveMockRows(vendorMockKey, readMockRows(vendorMockKey, seedVendors).filter(item => item.vendorNo !== row.vendorNo))
      saveMockRows(contactMockKey, readMockRows(contactMockKey, seedContactors).filter(item => item.vendorNo !== row.vendorNo))
      selectedVendorNo.value = ''
      await loadVendors()
    } else {
      const query = new URLSearchParams({ issueState: String(row.issueState), dataStamp: String(row.dataStamp) })
      await requestJson<void>('api/equipment/vendors/' + encodeURIComponent(row.vendorNo) + '?' + query, { method: 'DELETE' })
      selectedVendorNo.value = ''; await loadVendors()
    }
    successMessage.value = '供應商資料已刪除。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '供應商資料刪除失敗。' }
  finally { busy.value = false }
}
async function approveVendor(): Promise<void> {
  const row = selectedVendor.value
  if (!row || row.issueState !== 0 || busy.value || !window.confirm('確定核准供應商 ' + row.vendorNo + '？')) return
  busy.value = true; errorMessage.value = ''
  try {
    if (usesDevelopmentMock) {
      const rows = readMockRows(vendorMockKey, seedVendors)
      const index = rows.findIndex(item => item.vendorNo === row.vendorNo)
      if (index < 0) throw new Error('找不到要核准的供應商。')
      rows[index] = { ...rows[index], issueState: 2, dataStamp: rows[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: new Date().toISOString() }
      saveMockRows(vendorMockKey, rows); vendors.value = rows
      if (!visibleVendors.value.some(item => item.vendorNo === row.vendorNo)) { selectedVendorNo.value = ''; selectedContactorName.value = ''; contactors.value = [] }
    } else {
      const query = new URLSearchParams({ dataStamp: String(row.dataStamp) })
      await requestJson<void>('api/equipment/vendors/' + encodeURIComponent(row.vendorNo) + '/approve?' + query, { method: 'POST' })
      await loadVendors()
    }
    successMessage.value = '供應商已核准。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '供應商核准失敗。' }
  finally { busy.value = false }
}
async function saveContactor(): Promise<void> {
  if (busy.value || !contactMode.value || !selectedVendor.value) return
  busy.value = true; contactError.value = ''
  const mode = contactMode.value; const vendorNo = contactForm.value.vendorNo
  try {
    let result: Contactor
    if (usesDevelopmentMock) {
      const rows = readMockRows(contactMockKey, seedContactors)
      const contactorName = contactForm.value.contactorName.trim()
      const index = rows.findIndex(row => row.vendorNo === vendorNo && row.contactorName.toLowerCase() === contactorName.toLowerCase())
      if (mode !== 'edit' && index >= 0) throw new Error('同一供應商已有相同聯絡人。')
      const now = new Date().toISOString()
      const fields = { telNo: contactForm.value.telNo.trim() || null, faxNo: contactForm.value.faxNo.trim() || null, title: contactForm.value.title.trim() || null, address: contactForm.value.address.trim() || null, email: contactForm.value.email.trim() || null, description: contactForm.value.description.trim() || null }
      if (mode === 'edit') {
        if (index < 0) throw new Error('找不到要修改的聯絡人。')
        const original = rows[index]
        result = { ...original, ...fields, dataStamp: original.dataStamp + 1, reviser: props.currentUserNo || 'DEV001', revisedAt: now }
        rows[index] = result
      } else {
        result = { vendorNo, contactorName, ...fields, dataStamp: 0, creator: props.currentUserNo || 'DEV001', createdAt: now, reviser: null, revisedAt: null }
        rows.push(result)
      }
      saveMockRows(contactMockKey, rows)
    } else {
      const root = 'api/equipment/vendors/' + encodeURIComponent(vendorNo) + '/contactors'
      const path = mode === 'edit' ? root + '/' + encodeURIComponent(contactForm.value.contactorName) : root
      result = await requestJson<Contactor>(path, { method: mode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(contactForm.value) })
    }
    contactMode.value = null; await loadContactors(vendorNo); selectedContactorName.value = result.contactorName
  } catch (error) { contactError.value = error instanceof Error ? error.message : '聯絡人資料儲存失敗。' }
  finally { busy.value = false }
}
async function deleteContactor(): Promise<void> {
  const vendor = selectedVendor.value; const row = selectedContactor.value
  if (!vendor || !row || busy.value || !window.confirm('確定刪除聯絡人 ' + row.contactorName + '？')) return
  busy.value = true; contactError.value = ''
  try {
    if (usesDevelopmentMock) {
      saveMockRows(contactMockKey, readMockRows(contactMockKey, seedContactors).filter(item => !(item.vendorNo === vendor.vendorNo && item.contactorName === row.contactorName)))
      await loadContactors(vendor.vendorNo)
    } else {
      const path = 'api/equipment/vendors/' + encodeURIComponent(vendor.vendorNo) + '/contactors/' + encodeURIComponent(row.contactorName)
      await requestJson<void>(path + '?dataStamp=' + encodeURIComponent(String(row.dataStamp)), { method: 'DELETE' })
      await loadContactors(vendor.vendorNo)
    }
  } catch (error) { contactError.value = error instanceof Error ? error.message : '聯絡人資料刪除失敗。' }
  finally { busy.value = false }
}
function mailSelected(): void { const email = selectedContactor.value?.email?.trim(); if (email) window.location.href = 'mailto:' + email }
function closePage(): void { window.history.pushState({}, '', appBase + '/'); window.dispatchEvent(new PopStateEvent('popstate')) }
function onContactorNameBlur(): void { if (contactForm.value.contactorName && !contactForm.value.email) contactForm.value.email = contactForm.value.contactorName + '@' }
onMounted(() => { void loadVendors() })
</script>

<template>
  <div class="vendor-page">
    <form class="vendor-query" @submit.prevent="applyFilter">
      <fieldset class="vendor-state-filter"><legend>設備供應商</legend>
        <label><input v-model.number="issueState" type="radio" name="vendorState" :value="0" @change="applyFilter" />未簽核</label>
        <label><input v-model.number="issueState" type="radio" name="vendorState" :value="1" @change="applyFilter" />簽核中</label>
        <label><input v-model.number="issueState" type="radio" name="vendorState" :value="2" @change="applyFilter" />已簽核</label>
        <label><input v-model.number="issueState" type="radio" name="vendorState" :value="-1" @change="applyFilter" />不使用</label>
      </fieldset>
      <label class="vendor-search-label" for="vendor-search">供應商編號</label>
      <input id="vendor-search" v-model="keyword" type="search" /><span>%</span><button type="button" :disabled="loading" @click="applyFilter">查詢</button>
    </form>
    <div v-if="usesDevelopmentMock" class="mock-data-note" role="status">Development 假資料｜操作結果保存在此瀏覽器，不會寫入正式資料庫</div>
    <div v-if="errorMessage" class="vendor-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="vendor-message success" role="status">{{ successMessage }}</div>
    <section class="vendor-panel">
      <header class="vendor-panel-heading"><h2>供應商資料</h2><span>設備設定</span></header>
      <div class="vendor-list-layout">
        <div class="vendor-table-scroll"><table class="vendor-table">
          <thead><tr><th>供應商編號</th><th>供應商名稱</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleVendors" :key="row.vendorNo" tabindex="0" :aria-selected="selectedVendorNo === row.vendorNo" :class="{ selected: selectedVendorNo === row.vendorNo }" @click="selectVendor(row)" @keydown.enter.prevent="selectVendor(row)">
              <td class="vendor-code">{{ row.vendorNo }}</td><td>{{ row.vendorName }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createdAt) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.revisedAt) }}</td>
            </tr>
            <tr v-if="!loading && visibleVendors.length === 0"><td colspan="7" class="vendor-empty">{{ vendors.length ? '查無符合條件的供應商。' : '目前沒有供應商資料。' }}</td></tr>
          </tbody>
        </table></div>
        <aside class="vendor-actions" aria-label="供應商操作">
          <button class="primary" type="button" :disabled="busy" @click="openVendorForm('new')">新增(A)</button>
          <button type="button" :disabled="busy || selectedVendor?.issueState !== 0" @click="openVendorForm('edit')">修改(E)</button>
          <button type="button" :disabled="busy || !selectedVendor" @click="openVendorForm('copy')">複製(C)</button>
          <button type="button" :disabled="busy || selectedVendor?.issueState !== 0" @click="approveVendor">核准</button>
          <button type="button" :disabled="busy || !selectedVendor || ![0, 2, -1].includes(selectedVendor.issueState)" @click="deleteVendor">刪除(D)</button>
          <span class="vendor-state">{{ selectedVendor ? stateName(selectedVendor.issueState) : '未選取' }}</span>
          <button type="button" @click="closePage">關閉</button>
        </aside>
      </div>
      <footer class="vendor-count">資料總筆數 <strong>{{ visibleVendors.length }}</strong></footer>
    </section>
    <section class="vendor-panel contact-panel">
      <header class="vendor-panel-heading"><h2>供應商聯絡人</h2><span>{{ selectedVendor?.vendorNo || '請先選擇供應商' }}</span></header>
      <div v-if="contactError" class="vendor-message error" role="alert">{{ contactError }}</div>
      <div class="vendor-list-layout">
        <div class="vendor-table-scroll"><table class="vendor-table">
          <thead><tr><th>聯絡人</th><th>電話</th><th>傳真</th><th>職稱</th><th>地址</th><th>Email</th><th>說明</th></tr></thead>
          <tbody>
            <tr v-for="row in contactors" :key="row.contactorName" tabindex="0" :aria-selected="selectedContactorName === row.contactorName" :class="{ selected: selectedContactorName === row.contactorName }" @click="selectedContactorName = row.contactorName">
              <td>{{ row.contactorName }}</td><td>{{ row.telNo }}</td><td>{{ row.faxNo }}</td><td>{{ row.title }}</td><td>{{ row.address }}</td><td>{{ row.email }}</td><td>{{ row.description }}</td>
            </tr>
            <tr v-if="contactsLoading"><td colspan="7" class="vendor-empty">聯絡人資料載入中…</td></tr>
            <tr v-else-if="selectedVendor && contactors.length === 0 && !contactError"><td colspan="7" class="vendor-empty">此供應商目前沒有聯絡人。</td></tr>
            <tr v-else-if="!selectedVendor"><td colspan="7" class="vendor-empty">選擇供應商後載入聯絡人。</td></tr>
          </tbody>
        </table></div>
        <aside class="vendor-actions" aria-label="聯絡人操作">
          <button class="primary" type="button" :disabled="busy || !selectedVendor" @click="openContactForm('new')">新增</button>
          <button type="button" :disabled="busy || !selectedContactor" @click="openContactForm('edit')">修改</button>
          <button type="button" :disabled="busy || !selectedContactor" @click="openContactForm('copy')">複製</button>
          <button type="button" :disabled="busy || !selectedContactor" @click="deleteContactor">刪除</button>
          <button type="button" :disabled="!selectedContactor?.email" @click="mailSelected">Mail</button>
        </aside>
      </div>
      <footer class="vendor-count">資料總筆數 <strong>{{ contactors.length }}</strong></footer>
    </section>
    <div v-if="vendorMode" class="vendor-overlay" @click.self="vendorMode = null">
      <form class="vendor-dialog" @submit.prevent="saveVendor">
        <header><strong>設備供應商基本資料 ({{ vendorMode === 'edit' ? 'EDIT' : vendorMode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="vendorMode = null">×</button></header>
        <fieldset><legend>供應商資料</legend>
          <label><span>供應商編號</span><input v-model.trim="vendorForm.vendorNo" required maxlength="20" :readonly="vendorMode === 'edit'" @blur="vendorForm.vendorNo = vendorForm.vendorNo.toUpperCase()" /></label>
          <label><span>供應商名稱</span><input v-model.trim="vendorForm.vendorName" required maxlength="220" /></label>
          <label class="vendor-description"><span>說明</span><textarea v-model="vendorForm.description" maxlength="255" rows="4" /></label>
        </fieldset>
        <div class="vendor-audit"><label>建立人<input :value="vendorAudit.creator" readonly /></label><label>建立日期<input :value="vendorAudit.createdAt" readonly /></label><label>修改人<input :value="vendorAudit.reviser" readonly /></label><label>修改日期<input :value="vendorAudit.revisedAt" readonly /></label></div>
        <footer class="vendor-dialog-actions"><button type="button" @click="vendorMode = null">關閉</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>
    <div v-if="contactMode" class="vendor-overlay" @click.self="contactMode = null">
      <form class="vendor-dialog contact-dialog" @submit.prevent="saveContactor">
        <header><strong>設備供應商聯絡人 ({{ contactMode === 'edit' ? 'EDIT' : contactMode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="contactMode = null">×</button></header>
        <fieldset><legend>聯絡人資料</legend>
          <label><span>供應商編號</span><input :value="contactForm.vendorNo" readonly /></label>
          <label><span>聯絡人</span><input v-model.trim="contactForm.contactorName" required maxlength="50" :readonly="contactMode === 'edit'" @blur="onContactorNameBlur" /></label>
          <label><span>電話</span><input v-model.trim="contactForm.telNo" maxlength="20" /></label>
          <label><span>傳真</span><input v-model.trim="contactForm.faxNo" maxlength="20" /></label>
          <label><span>職稱</span><input v-model.trim="contactForm.title" maxlength="20" /></label>
          <label><span>Email</span><input v-model.trim="contactForm.email" type="text" inputmode="email" maxlength="50" /></label>
          <label><span>地址</span><input v-model.trim="contactForm.address" maxlength="50" /></label>
          <label class="vendor-description"><span>說明</span><textarea v-model="contactForm.description" maxlength="255" rows="3" /></label>
        </fieldset>
        <div class="vendor-audit"><label>建立人<input :value="contactAudit.creator" readonly /></label><label>建立日期<input :value="contactAudit.createdAt" readonly /></label><label>修改人<input :value="contactAudit.reviser" readonly /></label><label>修改日期<input :value="contactAudit.revisedAt" readonly /></label></div>
        <footer class="vendor-dialog-actions"><button type="button" @click="contactMode = null">關閉</button><button class="primary" type="submit" :disabled="busy">確定</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.vendor-page{display:grid;gap:8px;min-width:0;padding:8px;color:#26251f;font-size:13px}
.vendor-query{display:flex;min-height:48px;align-items:center;gap:8px;flex-wrap:wrap;padding:5px 8px;border:1px solid #d6d2c7;background:#fff}
.vendor-state-filter{display:flex;align-items:center;gap:14px;margin:0 8px 0 0;padding:0;border:0}
.vendor-state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}
.vendor-state-filter label{display:flex;align-items:center;gap:4px;white-space:nowrap}
.vendor-state-filter input{accent-color:#b59a00}
.vendor-search-label{margin-left:6px}
.vendor-query>input{width:min(220px,35vw);height:29px;padding:4px 7px;border:1px solid #aaa;background:#fff}
.vendor-query>button,.vendor-actions button,.vendor-dialog-actions button{min-height:30px;padding:4px 10px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f4f3ef);color:#333}
.vendor-query>button:disabled,.vendor-actions button:disabled,.vendor-dialog-actions button:disabled{color:#aaa;cursor:not-allowed}
.vendor-panel{min-width:0;border:1px solid #c7c4bb;background:#fff}
.vendor-panel-heading{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:7px 9px;border-bottom:1px solid #dedbd2;background:#faf9f6}
.vendor-panel-heading h2{margin:0;font-size:14px;font-weight:600}
.vendor-panel-heading span{color:#777366;font-size:12px}
.vendor-list-layout{display:grid;grid-template-columns:minmax(0,1fr) 100px;gap:8px;padding:7px}
.vendor-table-scroll{min-width:0;overflow:auto}
.vendor-table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}
.vendor-table th{height:30px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;text-align:left;font-weight:500}
.vendor-table td{height:31px;padding:5px 8px;border:1px solid #d0d0d0;max-width:280px;overflow:hidden;text-overflow:ellipsis}
.vendor-table tbody tr{cursor:pointer}
.vendor-table tbody tr:hover{background:#faf7e7}
.vendor-table tbody tr.selected{background:#fff0b5;box-shadow:inset 3px 0 #b59a00}
.vendor-table tbody tr:focus-visible{outline:2px solid #b59a00;outline-offset:-2px}
.vendor-code{font-family:'Segoe UI',sans-serif}
.vendor-empty{text-align:center;color:#777}
.vendor-actions{display:flex;flex-direction:column;gap:5px}
.vendor-actions button{text-align:center}
.vendor-actions button.primary,.vendor-dialog-actions button.primary{border-color:#cf9b33;background:#ffb33b;color:#fff;font-weight:600}
.vendor-state{padding:4px;color:#777;text-align:center;font-size:11px}
.vendor-count{padding:0 9px 7px;color:#777;font-size:11px}
.vendor-count strong{margin-left:7px;color:#555;font-weight:500}
.contact-panel{margin-top:2px}
.mock-data-note{padding:6px 10px;border:1px solid #d8c77b;background:#fff9dd;color:#665810;font-size:12px}.vendor-message{padding:7px 10px;border:1px solid;font-size:12px}
.vendor-message.error{border-color:#d5a3a3;background:#fff5f3;color:#7a2424}
.vendor-message.success{border-color:#b6c9a8;background:#f5f8ef;color:#435c32}
.vendor-overlay{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:18px;background:rgb(20 20 16 / 35%)}
.vendor-dialog{width:min(690px,100%);max-height:min(90vh,760px);overflow:auto;border:1px solid #c4c1b8;background:#fff;box-shadow:0 8px 28px rgb(0 0 0 / 20%)}
.vendor-dialog>header{display:flex;align-items:center;justify-content:space-between;padding:9px 12px;border-bottom:1px solid #ddd;background:#f8f7f3}
.vendor-dialog>header button{width:30px;height:28px;border:1px solid #ccc;background:#fff;font-size:20px;line-height:1}
.vendor-dialog fieldset{display:grid;grid-template-columns:1fr 1fr;gap:12px 18px;margin:12px;padding:14px;border:1px solid #c8c5bc}
.vendor-dialog legend{padding:0 5px;color:#555}
.vendor-dialog fieldset>label{display:grid;grid-template-columns:105px minmax(0,1fr);align-items:center;gap:8px}
.vendor-dialog input,.vendor-dialog textarea{width:100%;min-height:30px;padding:5px 7px;border:1px solid #999;background:#fff0ae;color:#26251f;font:inherit}
.vendor-dialog input:read-only{background:#eee}
.vendor-dialog textarea{resize:vertical}
.vendor-dialog .vendor-description{grid-column:1/-1;align-items:start}
.vendor-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;margin:0 12px 12px;padding:10px;border:1px solid #e7c600;background:#fbfaf5}.vendor-audit label{display:grid;gap:4px;color:#666;font-size:11px}.vendor-audit input{width:100%;min-height:28px;padding:4px;border:1px solid #ccc;background:#eee;color:#555;font:inherit}.vendor-dialog-actions{display:flex;justify-content:flex-end;gap:8px;padding:4px 12px 12px}
.contact-dialog{width:min(760px,100%)}
.contact-dialog fieldset{grid-template-columns:1fr 1fr}
@media(max-width:760px){.vendor-list-layout{grid-template-columns:minmax(0,1fr) 88px}.vendor-actions button{padding:4px;font-size:11px}.vendor-table{min-width:900px}.vendor-state-filter{gap:8px}.vendor-dialog fieldset,.contact-dialog fieldset{grid-template-columns:1fr}.vendor-dialog .vendor-description{grid-column:auto}}
</style>
