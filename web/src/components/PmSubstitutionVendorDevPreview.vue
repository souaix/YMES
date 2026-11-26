<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type FormMode = 'add' | 'edit' | 'copy'
interface Vendor {
  vendorNo: string; vendorName: string; description: string; creator: string; createDate: string
  reviser: string; reviseDate: string; issueState: IssueState; dataStamp: number
}
interface VendorContactor {
  vendorNo: string; contactorName: string; telNo: string; faxNo: string; title: string
  address: string; email: string; description: string; creator: string; createDate: string
  reviser: string; reviseDate: string; dataStamp: number
}
interface DevState { version: 1; approvalWorkflowConfigured: boolean; vendors: Vendor[]; contactors: VendorContactor[] }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.substitution-vendor.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]

function seedState(): DevState {
  const vendors: Vendor[] = [
    { vendorNo: 'SP-001', vendorName: '新泰工業', description: '設備備品供應', creator: 'PMUSER', createDate: '2026-11-18 09:15', reviser: '', reviseDate: '', issueState: 2, dataStamp: 2 },
    { vendorNo: 'SP-002', vendorName: '宏達精密', description: '馬達、軸承與傳動零件', creator: 'DEV02', createDate: '2026-11-20 10:30', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 },
    { vendorNo: 'SP-003', vendorName: '東昇機電', description: '待簽核供應商', creator: 'PMUSER', createDate: '2026-11-21 13:40', reviser: 'PMUSER', reviseDate: '2026-11-21 13:42', issueState: 1, dataStamp: 1 },
    { vendorNo: 'SP-004', vendorName: '立方設備', description: '停用供應商範例', creator: 'PMUSER', createDate: '2026-10-01 08:00', reviser: 'PMUSER', reviseDate: '2026-10-10 16:10', issueState: -1, dataStamp: 2 },
  ]
  const contactors: VendorContactor[] = [
    { vendorNo: 'SP-001', contactorName: '陳雅文', telNo: '02-2345-6789', faxNo: '02-2345-6790', title: '業務經理', address: '台北市中山區', email: 'ya-wen.chen@example.test', description: '備品詢價窗口', creator: 'PMUSER', createDate: '2026-11-18 09:20', reviser: '', reviseDate: '', dataStamp: 1 },
    { vendorNo: 'SP-001', contactorName: '林志明', telNo: '02-2345-6791', faxNo: '', title: '技術支援', address: '台北市中山區', email: 'service@example.test', description: '設備維修聯絡人', creator: 'PMUSER', createDate: '2026-11-18 09:25', reviser: '', reviseDate: '', dataStamp: 0 },
    { vendorNo: 'SP-002', contactorName: '許家豪', telNo: '04-2234-5511', faxNo: '', title: '業務代表', address: '台中市西屯區', email: 'sales@example.test', description: '軸承與馬達', creator: 'DEV02', createDate: '2026-11-20 10:35', reviser: '', reviseDate: '', dataStamp: 0 },
    { vendorNo: 'SP-003', contactorName: '王怡君', telNo: '03-555-1010', faxNo: '03-555-1011', title: '客服', address: '新竹縣竹北市', email: 'support@example.test', description: '', creator: 'PMUSER', createDate: '2026-11-21 13:45', reviser: '', reviseDate: '', dataStamp: 0 },
  ]
  return { version: 1, approvalWorkflowConfigured: false, vendors, contactors }
}

function loadState(): DevState {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.vendors) && Array.isArray(parsed.contactors)) return parsed
    }
  } catch { /* Fall back to local DEV fixtures when storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchVendorNo = ref('')
const activeVendorNo = ref('')
const activeContactorName = ref('')
const selectedVendorNos = ref<string[]>([])
const selectedContactorKeys = ref<string[]>([])
const vendorMode = ref<FormMode | null>(null)
const contactorMode = ref<FormMode | null>(null)
const vendorDraft = ref<Vendor | null>(null)
const contactorDraft = ref<VendorContactor | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const mailPreview = ref('')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleVendors = computed(() => state.value.vendors
  .filter((vendor) => vendor.issueState === issueState.value)
  .filter((vendor) => !searchVendorNo.value.trim() || vendor.vendorNo.toLocaleLowerCase().includes(searchVendorNo.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.vendorNo.localeCompare(b.vendorNo)))
const activeVendor = computed(() => state.value.vendors.find((vendor) => vendor.vendorNo === activeVendorNo.value) ?? null)
const visibleContactors = computed(() => state.value.contactors.filter((contactor) => contactor.vendorNo === activeVendorNo.value))
const selectedVendor = computed(() => activeVendor.value)
const selectedTargets = computed(() => {
  const selected = state.value.vendors.filter((vendor) => selectedVendorNos.value.includes(vendor.vendorNo))
  return selected.length ? selected : activeVendor.value ? [activeVendor.value] : []
})
const canEditVendor = computed(() => selectedVendor.value?.issueState === 0)
const selectedContact = computed(() => visibleContactors.value.find((contactor) => contactor.contactorName === activeContactorName.value) ?? null)
const allVisibleSelected = computed(() => visibleVendors.value.length > 0 && visibleVendors.value.every((vendor) => selectedVendorNos.value.includes(vendor.vendorNo)))
const issueStateLabel = (value: IssueState) => issueTabs.find((tab) => tab.value === value)?.label ?? '未知'
const contactKey = (contactor: VendorContactor) => `${contactor.vendorNo}|${contactor.contactorName}`

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV actions available in memory for this page session. */ }
}, { deep: true })
watch(visibleVendors, (vendors) => {
  if (!vendors.some((vendor) => vendor.vendorNo === activeVendorNo.value)) {
    activeVendorNo.value = vendors[0]?.vendorNo ?? ''
    activeContactorName.value = ''
    selectedContactorKeys.value = []
  }
}, { immediate: true })

function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3600)
}

function selectVendor(vendorNo: string) {
  activeVendorNo.value = vendorNo
  activeContactorName.value = ''
  selectedContactorKeys.value = []
}

function selectIssueState(value: IssueState) {
  issueState.value = value
  selectedVendorNos.value = []
}

function toggleVendor(vendorNo: string) {
  selectedVendorNos.value = selectedVendorNos.value.includes(vendorNo)
    ? selectedVendorNos.value.filter((value) => value !== vendorNo)
    : [...selectedVendorNos.value, vendorNo]
}

function toggleAllVendors() {
  const visible = visibleVendors.value.map((vendor) => vendor.vendorNo)
  selectedVendorNos.value = allVisibleSelected.value
    ? selectedVendorNos.value.filter((vendorNo) => !visible.includes(vendorNo))
    : [...new Set([...selectedVendorNos.value, ...visible])]
}

function selectContactorRow(contactorName: string) {
  activeContactorName.value = contactorName
  if (!selectedContactorKeys.value.includes(contactorName)) selectedContactorKeys.value = [...selectedContactorKeys.value, contactorName]
}

function toggleContactor(contactorName: string) {
  activeContactorName.value = contactorName
  selectedContactorKeys.value = selectedContactorKeys.value.includes(contactorName)
    ? selectedContactorKeys.value.filter((value) => value !== contactorName)
    : [...selectedContactorKeys.value, contactorName]
}

function emptyVendor(): Vendor {
  return { vendorNo: '', vendorName: '', description: '', creator: '', createDate: '', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
}

function openVendorForm(nextMode: FormMode) {
  if (nextMode !== 'add' && !activeVendor.value) return
  vendorMode.value = nextMode
  vendorDraft.value = nextMode === 'add' ? emptyVendor() : { ...activeVendor.value! }
}

function saveVendor() {
  if (!vendorDraft.value || !vendorMode.value) return
  const mode = vendorMode.value
  const draft = { ...vendorDraft.value, vendorNo: vendorDraft.value.vendorNo.trim().toUpperCase(), vendorName: vendorDraft.value.vendorName.trim() }
  if (!draft.vendorNo) return toast('請輸入供應商代碼。', 'error')
  if (!draft.vendorName) return toast('請輸入供應商名稱。', 'error')
  const original = activeVendor.value
  if (mode === 'edit' && !original) return
  if (state.value.vendors.some((vendor) => vendor.vendorNo === draft.vendorNo && (mode !== 'edit' || vendor.vendorNo !== original?.vendorNo))) return toast('供應商代碼已存在。', 'error')
  if (mode === 'edit') {
    const index = state.value.vendors.findIndex((vendor) => vendor.vendorNo === original!.vendorNo)
    if (index < 0 || state.value.vendors[index].dataStamp !== original!.dataStamp) return toast('資料已變更，請重新選取後再修改。', 'error')
    state.value.vendors[index] = { ...original!, vendorName: draft.vendorName, description: draft.description, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: original!.dataStamp + 1 }
    toast('供應商資料已修改。')
  } else {
    const created: Vendor = { ...draft, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
    state.value.vendors.unshift(created)
    if (mode === 'copy' && original) {
      const copiedContactors = state.value.contactors.filter((contactor) => contactor.vendorNo === original.vendorNo).map((contactor) => ({
        ...contactor, vendorNo: created.vendorNo, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0,
      }))
      state.value.contactors.push(...copiedContactors)
    }
    issueState.value = 0
    activeVendorNo.value = created.vendorNo
    selectedVendorNos.value = [created.vendorNo]
    toast(mode === 'copy' ? '已建立供應商並複製聯絡人。' : '供應商已新增。')
  }
  vendorMode.value = null
  vendorDraft.value = null
}

function approveVendors() {
  const targets = selectedTargets.value.filter((vendor) => vendor.issueState === 0)
  if (!targets.length) return toast('請選取未凍結供應商。', 'error')
  const next: IssueState = state.value.approvalWorkflowConfigured ? 1 : 2
  if (!window.confirm(`將 ${targets.length} 筆供應商資料${next === 1 ? '送簽' : '直接生效'}？`)) return
  for (const vendor of targets) {
    vendor.issueState = next
    vendor.reviser = currentUser
    vendor.reviseDate = nowLabel.value
    vendor.dataStamp += 1
  }
  selectedVendorNos.value = []
  toast(next === 1 ? 'DEV 已模擬送入簽核中；沒有寄信或呼叫簽核服務。' : 'DEV 已模擬直接生效。')
}

function deleteVendors() {
  if (!selectedTargets.value.length) return toast('請先選取供應商。', 'error')
  if (selectedTargets.value.some((vendor) => vendor.issueState === 1)) return toast('簽核中的供應商不可刪除。', 'error')
  if (!window.confirm(`確認處理 ${selectedTargets.value.length} 筆？未凍結／未使用會刪除供應商、聯絡人及備品對應；已生效會改為未使用。`)) return
  const removed = new Set(selectedTargets.value.filter((vendor) => vendor.issueState === 0 || vendor.issueState === -1).map((vendor) => vendor.vendorNo))
  for (const vendor of selectedTargets.value) {
    if (vendor.issueState === 2) {
      vendor.issueState = -1
      vendor.reviser = currentUser
      vendor.reviseDate = nowLabel.value
      vendor.dataStamp += 1
    }
  }
  state.value.vendors = state.value.vendors.filter((vendor) => !removed.has(vendor.vendorNo))
  state.value.contactors = state.value.contactors.filter((contactor) => !removed.has(contactor.vendorNo))
  selectedVendorNos.value = []
  toast('供應商資料處理完成。')
}

function emptyContactor(): VendorContactor {
  return { vendorNo: activeVendorNo.value, contactorName: '', telNo: '', faxNo: '', title: '', address: '', email: '', description: '', creator: '', createDate: '', reviser: '', reviseDate: '', dataStamp: 0 }
}

function openContactorForm(nextMode: FormMode) {
  if (!activeVendor.value || (nextMode !== 'add' && !selectedContact.value)) return
  contactorMode.value = nextMode
  contactorDraft.value = nextMode === 'add' ? emptyContactor() : { ...selectedContact.value! }
}

function saveContactor() {
  if (!contactorDraft.value || !contactorMode.value) return
  const mode = contactorMode.value
  const draft = { ...contactorDraft.value, contactorName: contactorDraft.value.contactorName.trim() }
  if (!draft.vendorNo) return toast('缺少供應商代碼。', 'error')
  if (!draft.contactorName) return toast('請輸入聯絡人姓名。', 'error')
  const original = selectedContact.value
  const duplicate = state.value.contactors.some((contactor) => contactor.vendorNo === draft.vendorNo && contactor.contactorName === draft.contactorName && (mode !== 'edit' || contactor.contactorName !== original?.contactorName))
  if (duplicate) return toast('此供應商已存在相同聯絡人姓名。', 'error')
  if (mode === 'edit') {
    if (!original) return
    const index = state.value.contactors.findIndex((contactor) => contactor.vendorNo === original.vendorNo && contactor.contactorName === original.contactorName)
    if (index < 0 || state.value.contactors[index].dataStamp !== original.dataStamp) return toast('聯絡人資料已變更，請重新選取。', 'error')
    state.value.contactors[index] = { ...original, ...draft, vendorNo: original.vendorNo, contactorName: original.contactorName, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: original.dataStamp + 1 }
    toast('聯絡人資料已修改。')
  } else {
    const created: VendorContactor = { ...draft, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 }
    state.value.contactors.push(created)
    selectedContactorKeys.value = [created.contactorName]
    activeContactorName.value = created.contactorName
    toast(mode === 'copy' ? '已複製聯絡人。' : '聯絡人已新增。')
  }
  contactorMode.value = null
  contactorDraft.value = null
}

function deleteContactors() {
  if (!selectedContactorKeys.value.length) return toast('請先選取聯絡人。', 'error')
  const names = [...selectedContactorKeys.value]
  if (!window.confirm(`確認刪除 ${names.length} 筆聯絡人？`)) return
  state.value.contactors = state.value.contactors.filter((contactor) => contactor.vendorNo !== activeVendorNo.value || !names.includes(contactor.contactorName))
  selectedContactorKeys.value = []
  activeContactorName.value = ''
  toast('聯絡人已刪除。')
}

function previewEmail() {
  const contact = selectedContact.value
  if (!contact) return toast('請先選取聯絡人。', 'error')
  mailPreview.value = contact.email || '此聯絡人沒有 Email。'
}

function resetFixtures() {
  if (!window.confirm('重設本頁 DEV 假資料？')) return
  state.value = seedState()
  issueState.value = 0
  searchVendorNo.value = ''
  selectedVendorNos.value = []
  selectedContactorKeys.value = []
  activeContactorName.value = ''
  toast('DEV 假資料已重設。')
}
</script>

<template>
  <section class="pm-vendor" aria-label="備品供應商基本資料 DEV 預覽">
    <header class="pm-vendor__header">
      <div><div class="pm-vendor__title-row"><span class="pm-vendor__app-mark">PM</span><h1>備品供應商基本資料</h1><span class="pm-vendor__dev-tag">DEV 假資料</span></div><p>維護備品供應商、聯絡人及簽核狀態。</p></div>
      <button type="button" class="pm-vendor__close" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="pm-vendor__toolbar">
      <button class="primary" type="button" @click="openVendorForm('add')">新增供應商</button>
      <button type="button" :disabled="!canEditVendor" @click="openVendorForm('edit')">修改</button>
      <button type="button" :disabled="!activeVendor" @click="openVendorForm('copy')">複製供應商</button>
      <button type="button" :disabled="!selectedTargets.length || selectedTargets.some((vendor) => vendor.issueState === 1)" @click="deleteVendors">刪除</button>
      <button type="button" :disabled="!selectedTargets.some((vendor) => vendor.issueState === 0)" @click="approveVendors">核准／送簽</button>
      <span class="spacer"></span>
      <label class="workflow"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label>
      <button type="button" class="quiet" @click="resetFixtures">重設假資料</button>
    </div>
    <nav class="pm-vendor__tabs" aria-label="簽核狀態">
      <button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}<span>{{ state.vendors.filter((vendor) => vendor.issueState === tab.value).length }}</span></button>
    </nav>
    <div class="pm-vendor__filter"><label>供應商代碼 <input v-model="searchVendorNo" type="search" placeholder="輸入 VendorNo 搜尋"></label><span>查詢欄位依舊系統：VendorNo</span><b>{{ visibleVendors.length }} 筆</b></div>

    <section class="pm-vendor__master" aria-label="供應商清單">
      <div class="pm-vendor__section-title"><h2>供應商</h2><span>選取主檔可載入下方聯絡人</span></div>
      <div class="pm-vendor__table-wrap">
        <table class="master-table"><thead><tr><th class="check"><input type="checkbox" :checked="allVisibleSelected" aria-label="選取全部供應商" @change="toggleAllVendors"></th><th>VendorNo</th><th>供應商名稱</th><th>說明</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th></tr></thead>
          <tbody><tr v-for="vendor in visibleVendors" :key="vendor.vendorNo" :class="{ active: activeVendorNo === vendor.vendorNo }" @click="selectVendor(vendor.vendorNo)"><td class="check" @click.stop><input type="checkbox" :checked="selectedVendorNos.includes(vendor.vendorNo)" :aria-label="'選取 ' + vendor.vendorNo" @change="toggleVendor(vendor.vendorNo)"></td><td class="mono">{{ vendor.vendorNo }}</td><td>{{ vendor.vendorName }}</td><td class="desc">{{ vendor.description }}</td><td>{{ vendor.creator }}</td><td>{{ vendor.createDate }}</td><td>{{ vendor.reviser || '—' }}</td><td>{{ vendor.reviseDate || '—' }}</td></tr><tr v-if="!visibleVendors.length"><td colspan="8" class="empty">沒有符合條件的供應商。</td></tr></tbody>
        </table>
      </div>
    </section>

    <section class="pm-vendor__contacts" aria-label="供應商聯絡人">
      <div class="pm-vendor__section-title"><h2>聯絡人 <span v-if="activeVendor">— {{ activeVendor.vendorNo }} {{ activeVendor.vendorName }}</span></h2><span>{{ visibleContactors.length }} 筆</span></div>
      <div class="pm-vendor__contact-toolbar"><button type="button" :disabled="!activeVendor" @click="openContactorForm('add')">新增</button><button type="button" :disabled="!selectedContact" @click="openContactorForm('edit')">修改</button><button type="button" :disabled="!selectedContact" @click="openContactorForm('copy')">複製</button><button type="button" :disabled="!selectedContactorKeys.length" @click="deleteContactors">刪除</button><button type="button" :disabled="!selectedContact" @click="previewEmail">Email 預覽</button></div>
      <div class="pm-vendor__table-wrap contacts-wrap"><table><thead><tr><th class="check">選取</th><th>聯絡人</th><th>職稱</th><th>電話</th><th>傳真</th><th>Email</th><th>地址</th><th>說明</th><th>建立者</th><th>建立時間</th><th>修改者</th><th>修改時間</th></tr></thead>
        <tbody><tr v-for="contactor in visibleContactors" :key="contactKey(contactor)" :class="{ active: activeContactorName === contactor.contactorName }" @click="selectContactorRow(contactor.contactorName)"><td class="check" @click.stop><input type="checkbox" :checked="selectedContactorKeys.includes(contactor.contactorName)" :aria-label="'選取 ' + contactor.contactorName" @change="toggleContactor(contactor.contactorName)"></td><td>{{ contactor.contactorName }}</td><td>{{ contactor.title }}</td><td>{{ contactor.telNo }}</td><td>{{ contactor.faxNo }}</td><td>{{ contactor.email }}</td><td>{{ contactor.address }}</td><td class="desc">{{ contactor.description }}</td><td>{{ contactor.creator }}</td><td>{{ contactor.createDate }}</td><td>{{ contactor.reviser || '—' }}</td><td>{{ contactor.reviseDate || '—' }}</td></tr><tr v-if="!activeVendor || !visibleContactors.length"><td colspan="12" class="empty">{{ activeVendor ? '此供應商目前沒有聯絡人。' : '請先選取供應商。' }}</td></tr></tbody>
      </table></div>
    </section>

    <footer class="pm-vendor__footer"><span>狀態：<strong>{{ issueStateLabel(issueState) }}</strong></span><span>DEV 操作只保存在此瀏覽器 localStorage；Email 僅預覽，不啟動郵件程式。</span></footer>
    <Transition><div v-if="message" class="pm-vendor__toast" :class="messageKind" role="status">{{ message }}</div></Transition>
    <div v-if="mailPreview" class="pm-vendor__backdrop" @click.self="mailPreview = ''"><section class="pm-vendor__dialog" role="dialog" aria-modal="true" aria-label="Email 預覽"><header><h2>Email 預覽</h2><button type="button" @click="mailPreview = ''">×</button></header><p>{{ mailPreview }}</p><footer><button type="button" @click="mailPreview = ''">關閉</button></footer></section></div>

    <div v-if="vendorMode && vendorDraft" class="pm-vendor__backdrop" @click.self="vendorMode = null"><section class="pm-vendor__dialog" role="dialog" aria-modal="true" aria-label="供應商編輯">
      <header><div><h2>{{ vendorMode === 'add' ? '新增供應商' : vendorMode === 'copy' ? '複製供應商' : '修改供應商' }}</h2><p>主檔與聯絡人來源為 PM102 的供應商及聯絡人資料。</p></div><button type="button" @click="vendorMode = null">×</button></header>
      <form @submit.prevent="saveVendor"><div class="form-grid"><label>供應商代碼<input v-model="vendorDraft.vendorNo" :readonly="vendorMode === 'edit'" required autocomplete="off" @blur="vendorDraft.vendorNo = vendorDraft.vendorNo.toUpperCase()"></label><label>供應商名稱<input v-model="vendorDraft.vendorName" required></label><label class="wide">說明<textarea v-model="vendorDraft.description" rows="4"></textarea></label></div><footer><button type="button" @click="vendorMode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form>
    </section></div>

    <div v-if="contactorMode && contactorDraft" class="pm-vendor__backdrop" @click.self="contactorMode = null"><section class="pm-vendor__dialog" role="dialog" aria-modal="true" aria-label="聯絡人編輯">
      <header><div><h2>{{ contactorMode === 'add' ? '新增聯絡人' : contactorMode === 'copy' ? '複製聯絡人' : '修改聯絡人' }}</h2><p>複合鍵為 VendorNo + ContactorName。</p></div><button type="button" @click="contactorMode = null">×</button></header>
      <form @submit.prevent="saveContactor"><div class="form-grid"><label>供應商代碼<input :value="contactorDraft.vendorNo" readonly></label><label>聯絡人姓名<input v-model="contactorDraft.contactorName" :readonly="contactorMode === 'edit'" required autocomplete="off"></label><label>職稱<input v-model="contactorDraft.title"></label><label>電話<input v-model="contactorDraft.telNo" type="tel"></label><label>傳真<input v-model="contactorDraft.faxNo" type="tel"></label><label>Email<input v-model="contactorDraft.email" type="email"></label><label class="wide">地址<input v-model="contactorDraft.address"></label><label class="wide">說明<textarea v-model="contactorDraft.description" rows="3"></textarea></label></div><footer><button type="button" @click="contactorMode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form>
    </section></div>
  </section>
</template>

<style scoped>
.pm-vendor { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 8px; padding: 14px; box-sizing: border-box; background: #f4f3ee; color: #26251f; font: 12px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.pm-vendor button, .pm-vendor input, .pm-vendor textarea { font: inherit; }
.pm-vendor button { min-height: 28px; padding: 4px 10px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: #333229; cursor: pointer; }
.pm-vendor button:hover:not(:disabled) { border-color: #787467; background: #f5f3e9; }
.pm-vendor button:disabled { opacity: .45; cursor: not-allowed; }
.pm-vendor button.primary { border-color: #425746; background: #425746; color: white; }
.pm-vendor button.quiet { background: transparent; }
.pm-vendor__header { display: flex; align-items: flex-start; justify-content: space-between; padding-bottom: 8px; border-bottom: 1px solid #d9d7ce; }
.pm-vendor__title-row { display: flex; align-items: center; gap: 8px; }
.pm-vendor__header h1 { margin: 0; font-size: 19px; font-weight: 600; }
.pm-vendor__header p { margin: 4px 0 0 39px; color: #777366; }
.pm-vendor__app-mark { display: grid; place-items: center; width: 30px; height: 27px; border-radius: 3px; background: #526855; color: white; font-weight: 700; }
.pm-vendor__dev-tag { padding: 2px 7px; border-radius: 10px; background: #e8e2bd; color: #574d1f; font-size: 10px; }
.pm-vendor .pm-vendor__close { min-width: 31px; padding: 0; font-size: 21px; }
.pm-vendor__toolbar, .pm-vendor__contact-toolbar { display: flex; align-items: center; gap: 5px; flex-wrap: wrap; }
.pm-vendor .spacer { flex: 1; }
.pm-vendor .workflow { display: inline-flex; align-items: center; gap: 5px; color: #68655b; font-size: 11px; }
.pm-vendor .workflow input { accent-color: #526855; }
.pm-vendor__tabs { display: flex; gap: 2px; border-bottom: 1px solid #cac8be; }
.pm-vendor__tabs button { border: 0; border-radius: 3px 3px 0 0; background: transparent; color: #716e63; }
.pm-vendor__tabs button.active { border-bottom: 2px solid #4a674e; color: #2d4732; font-weight: 600; }
.pm-vendor__tabs span { margin-left: 6px; padding: 1px 5px; border-radius: 9px; background: #e5e3da; font-size: 10px; }
.pm-vendor__filter { display: flex; align-items: center; gap: 10px; color: #817e73; font-size: 11px; }
.pm-vendor__filter label { display: flex; align-items: center; gap: 6px; color: #39372e; }
.pm-vendor input:not([type=checkbox]), .pm-vendor textarea { min-height: 28px; box-sizing: border-box; padding: 5px 7px; border: 1px solid #c9c7bd; border-radius: 3px; background: #fffefa; color: inherit; }
.pm-vendor__filter input { width: 190px; }
.pm-vendor__filter b { margin-left: auto; font-weight: 400; }
.pm-vendor__master { flex: 0 1 40%; min-height: 110px; display: flex; flex-direction: column; }
.pm-vendor__contacts { flex: 1 1 45%; min-height: 120px; display: flex; flex-direction: column; }
.pm-vendor__section-title { display: flex; align-items: baseline; justify-content: space-between; padding: 2px 1px 5px; color: #777366; font-size: 11px; }
.pm-vendor__section-title h2 { margin: 0; color: #3d3a31; font-size: 13px; font-weight: 600; }
.pm-vendor__section-title h2 span { color: #7a776a; font-size: 11px; font-weight: 400; }
.pm-vendor__table-wrap { flex: 1; min-height: 0; overflow: auto; border: 1px solid #ceccc2; background: #fffefa; }
.pm-vendor table { width: 100%; border-collapse: collapse; white-space: nowrap; }
.pm-vendor table.master-table { min-width: 780px; }
.pm-vendor .contacts-wrap table { min-width: 1260px; }
.pm-vendor th { position: sticky; top: 0; z-index: 1; padding: 6px 7px; border-bottom: 1px solid #c8c6bc; background: #eae9e1; color: #4e4b41; text-align: left; font-weight: 600; }
.pm-vendor td { padding: 6px 7px; border-bottom: 1px solid #eeece5; }
.pm-vendor tbody tr { cursor: pointer; }
.pm-vendor tbody tr:hover { background: #f6f5ef; }
.pm-vendor tbody tr.active { background: #e8eee5; }
.pm-vendor .check { width: 29px; text-align: center; }
.pm-vendor .mono { font-family: Consolas, monospace; }
.pm-vendor .desc { max-width: 240px; overflow: hidden; text-overflow: ellipsis; }
.pm-vendor .empty { padding: 22px; color: #858174; text-align: center; }
.pm-vendor__contact-toolbar { margin-bottom: 5px; }
.pm-vendor__footer { display: flex; justify-content: space-between; color: #817e73; font-size: 10px; }
.pm-vendor__footer strong { color: #47443a; }
.pm-vendor__toast { position: fixed; right: 22px; bottom: 22px; z-index: 30; padding: 10px 14px; border: 1px solid #c4d2c1; border-radius: 4px; background: #f0f7ee; box-shadow: 0 4px 18px #0002; }
.pm-vendor__toast.error { border-color: #d7b7ad; background: #fcf1ed; color: #842d20; }
.pm-vendor__backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 16px; background: #25251f99; }
.pm-vendor__dialog { width: min(650px, 100%); max-height: min(92vh, 740px); overflow: auto; border: 1px solid #a9a79d; border-radius: 5px; background: #f9f8f3; box-shadow: 0 12px 40px #0004; }
.pm-vendor__dialog > header { display: flex; align-items: flex-start; justify-content: space-between; padding: 13px 17px 10px; border-bottom: 1px solid #dddacf; }
.pm-vendor__dialog h2 { margin: 0; font-size: 16px; }
.pm-vendor__dialog header p { margin: 4px 0 0; color: #817e73; font-size: 10px; }
.pm-vendor__dialog header button { min-width: 27px; padding: 0; border: 0; background: transparent; font-size: 19px; }
.pm-vendor__dialog form, .pm-vendor__dialog > p { padding: 14px 17px; }
.pm-vendor__dialog > p { margin: 0; overflow-wrap: anywhere; }
.pm-vendor .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.pm-vendor .form-grid label { display: flex; flex-direction: column; gap: 4px; }
.pm-vendor .form-grid .wide { grid-column: 1 / -1; }
.pm-vendor .form-grid textarea { resize: vertical; }
.pm-vendor__dialog form > footer, .pm-vendor__dialog > footer { display: flex; justify-content: flex-end; gap: 6px; padding-top: 12px; margin-top: 14px; border-top: 1px solid #dddacf; }
.v-enter-active, .v-leave-active { transition: opacity .14s ease; }
.v-enter-from, .v-leave-to { opacity: 0; }
@media (max-width: 900px) { .pm-vendor { padding: 9px; } .pm-vendor__toolbar .spacer, .pm-vendor .workflow { width: 100%; flex-basis: 100%; } .pm-vendor__footer { flex-direction: column; gap: 3px; } }
</style>
