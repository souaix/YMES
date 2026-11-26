<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type BasisMode = 'add' | 'edit' | 'copy'
type MapMode = 'add' | 'edit'
interface VendorMap {
  vendorNo: string; description: string; creator: string; createDate: string
  reviser: string; reviseDate: string; dataStamp: number
}
interface SubstitutionBasis {
  substitutionNo: string; substitutionName: string; substitutionType: string
  safeQty: number; unitType: string; unitNo: string; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string
  issueState: IssueState; dataStamp: number; vendors: VendorMap[]
}
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: SubstitutionBasis[] }
type BasisDraft = Omit<SubstitutionBasis, 'vendors'>

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.substitution-basis.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
const vendorChoices = ['V0001', 'V0002', 'V0015', 'V0032', 'V0100']
const typeChoices = ['BEARING', 'ELECTRICAL', 'MOTOR', 'PNEUMATIC']
const unitTypeChoices = ['PCS', 'LENGTH', 'WEIGHT']
const unitChoices: Record<string, string[]> = { PCS: ['EA', 'SET', 'BOX'], LENGTH: ['M', 'MM'], WEIGHT: ['KG', 'G'] }

function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { substitutionNo: 'BRG-6204', substitutionName: '深溝滾珠軸承 6204', substitutionType: 'BEARING', safeQty: 4, unitType: 'PCS', unitNo: 'EA', description: '主軸側常用替換件', creator: 'PMUSER', createDate: '2026-11-17 09:10', reviser: '', reviseDate: '', issueState: 2, dataStamp: 2, vendors: [
        { vendorNo: 'V0001', description: '原廠供應', creator: 'PMUSER', createDate: '2026-11-17 09:12', reviser: '', reviseDate: '', dataStamp: 0 },
        { vendorNo: 'V0015', description: '交期備援', creator: 'PMUSER', createDate: '2026-11-17 09:14', reviser: '', reviseDate: '', dataStamp: 1 },
      ] },
      { substitutionNo: 'MTR-1KW', substitutionName: 'AC 馬達 1kW', substitutionType: 'MOTOR', safeQty: 1, unitType: 'PCS', unitNo: 'EA', description: '', creator: 'DEV02', createDate: '2026-11-20 10:30', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0, vendors: [
        { vendorNo: 'V0002', description: '含安裝底座', creator: 'DEV02', createDate: '2026-11-20 10:31', reviser: '', reviseDate: '', dataStamp: 0 },
      ] },
      { substitutionNo: 'PNEU-SV01', substitutionName: '電磁閥維修組', substitutionType: 'PNEUMATIC', safeQty: 2.5, unitType: 'PCS', unitNo: 'SET', description: '含線圈與密封件', creator: 'PMUSER', createDate: '2026-11-21 13:40', reviser: 'PMUSER', reviseDate: '2026-11-22 08:10', issueState: 1, dataStamp: 1, vendors: [] },
      { substitutionNo: 'OLD-PUMP', substitutionName: '舊型泵浦備品', substitutionType: 'MOTOR', safeQty: 0, unitType: 'PCS', unitNo: 'EA', description: '已停用範例', creator: 'PMUSER', createDate: '2026-10-01 08:00', reviser: 'PMUSER', reviseDate: '2026-10-10 16:10', issueState: -1, dataStamp: 2, vendors: [] },
    ],
  }
}

function loadState(): DevState {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const parsed = JSON.parse(raw) as DevState
      if (parsed?.version === 1 && Array.isArray(parsed.records)) return parsed
    }
  } catch { /* Keep the screen usable when local storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchCode = ref('')
const activeCode = ref('')
const selectedCodes = ref<string[]>([])
const selectedVendorNos = ref<string[]>([])
const basisMode = ref<BasisMode | null>(null)
const basisDraft = ref<BasisDraft | null>(null)
const mapMode = ref<MapMode | null>(null)
const mapDraft = ref<VendorMap | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchCode.value.trim() || record.substitutionNo.toLocaleLowerCase().includes(searchCode.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.substitutionNo.localeCompare(b.substitutionNo)))
const activeRecord = computed(() => state.value.records.find((record) => record.substitutionNo === activeCode.value) ?? null)
const visibleVendors = computed(() => activeRecord.value?.vendors ?? [])
const selectedTargets = computed(() => {
  const selected = state.value.records.filter((record) => selectedCodes.value.includes(record.substitutionNo))
  return selected.length ? selected : activeRecord.value ? [activeRecord.value] : []
})
const selectedVendorTargets = computed(() => {
  const selected = visibleVendors.value.filter((record) => selectedVendorNos.value.includes(record.vendorNo))
  return selected.length ? selected : []
})
const canEdit = computed(() => activeRecord.value?.issueState === 0)
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((record) => selectedCodes.value.includes(record.substitutionNo)))
const issueStateLabel = (value: IssueState) => issueTabs.find((tab) => tab.value === value)?.label ?? '未知'

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory for this session. */ }
}, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((record) => record.substitutionNo === activeCode.value)) activeCode.value = rows[0]?.substitutionNo ?? ''
  selectedCodes.value = selectedCodes.value.filter((code) => rows.some((record) => record.substitutionNo === code))
}, { immediate: true })
watch(activeCode, () => { selectedVendorNos.value = [] })

function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3600)
}
function selectIssueState(value: IssueState) {
  issueState.value = value
  selectedCodes.value = []
}
function selectRecord(code: string) {
  activeCode.value = code
  if (!selectedCodes.value.includes(code)) selectedCodes.value = []
}
function toggleRecord(code: string) {
  selectedCodes.value = selectedCodes.value.includes(code)
    ? selectedCodes.value.filter((value) => value !== code)
    : [...selectedCodes.value, code]
}
function toggleAll() {
  selectedCodes.value = allVisibleSelected.value ? [] : visibleRows.value.map((record) => record.substitutionNo)
}
function openBasis(mode: BasisMode) {
  const source = activeRecord.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.issueState !== 0) {
    toast('只有未凍結資料可以修改。', 'error')
    return
  }
  basisMode.value = mode
  basisDraft.value = mode === 'add'
    ? { substitutionNo: '', substitutionName: '', substitutionType: '', safeQty: 0, unitType: '', unitNo: '', description: '', creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0 }
    : { ...source!, substitutionNo: mode === 'copy' ? '' : source!.substitutionNo, creator: mode === 'copy' ? currentUser : source!.creator, createDate: mode === 'copy' ? nowLabel.value : source!.createDate, reviser: mode === 'copy' ? '' : source!.reviser, reviseDate: mode === 'copy' ? '' : source!.reviseDate, issueState: mode === 'copy' ? 0 : source!.issueState, dataStamp: mode === 'copy' ? 0 : source!.dataStamp }
}
function saveBasis() {
  const draft = basisDraft.value
  if (!draft) return
  draft.substitutionNo = draft.substitutionNo.toUpperCase()
  if (!draft.substitutionNo.trim() || !draft.substitutionName.trim() || !draft.substitutionType.trim() || !draft.unitType.trim() || !draft.unitNo.trim()) {
    toast('備品代碼、名稱、類別、單位類別與單位皆為必填。', 'error')
    return
  }
  const qty = Number(draft.safeQty)
  if (!Number.isFinite(qty) || qty < 0) {
    toast('安全庫存量需為大於或等於 0 的數字。', 'error')
    return
  }
  const duplicate = state.value.records.some((record) => record.substitutionNo.toLocaleUpperCase() === draft.substitutionNo && record.substitutionNo !== (basisMode.value === 'edit' ? activeCode.value : ''))
  if (duplicate) {
    toast('備品代碼已存在。', 'error')
    return
  }
  if (basisMode.value === 'edit' && activeRecord.value) {
    Object.assign(activeRecord.value, draft, { safeQty: qty, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: activeRecord.value.dataStamp + 1 })
    toast('備品資料已在 DEV 假資料中修改。')
  } else {
    const record: SubstitutionBasis = { ...draft, safeQty: qty, issueState: 0, dataStamp: 0, vendors: [] }
    if (basisMode.value === 'copy' && activeRecord.value) {
      record.vendors = activeRecord.value.vendors.map((vendor) => ({ ...vendor, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 }))
    }
    state.value.records.push(record)
    searchCode.value = ''
    activeCode.value = record.substitutionNo
    issueState.value = 0
    toast(basisMode.value === 'copy' ? `備品已複製，並帶入 ${record.vendors.length} 筆供應商映射。` : '備品已新增至 DEV 假資料。')
  }
  basisMode.value = null
  basisDraft.value = null
}
function deleteBases() {
  const targets = selectedTargets.value
  if (!targets.length) return
  let stoppedAtPending = false
  for (const record of targets) {
    if (record.issueState === 1) {
      toast(`簽核中資料 ${record.substitutionNo} 不可刪除；已依序處理前面的選取列。`, 'error')
      stoppedAtPending = true
      break
    }
    if (record.issueState === 2) {
      record.issueState = -1
      record.dataStamp += 1
      record.reviser = currentUser
      record.reviseDate = nowLabel.value
    } else {
      state.value.records = state.value.records.filter((item) => item.substitutionNo !== record.substitutionNo)
      if (activeCode.value === record.substitutionNo) activeCode.value = ''
    }
  }
  selectedCodes.value = []
  if (!stoppedAtPending) toast('所選資料已依狀態刪除或移至未使用。')
}
function approveBases() {
  for (const record of selectedTargets.value) {
    if (record.issueState !== 0) {
      toast(`只有未凍結資料可核准；${record.substitutionNo} 之後的資料未處理。`, 'error')
      return
    }
    record.issueState = state.value.approvalWorkflowConfigured ? 1 : 2
    record.dataStamp += 1
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
  }
  selectedCodes.value = []
  toast(state.value.approvalWorkflowConfigured ? '資料已送入 DEV 簽核中。' : '未設定簽核流程，資料直接模擬生效。')
}
function openMap(mode: MapMode) {
  if (!activeRecord.value) return
  const source = mode === 'edit' ? selectedVendorTargets.value[0] : null
  if (mode === 'edit' && !source) {
    toast('請先選取一筆供應商映射。', 'error')
    return
  }
  mapMode.value = mode
  mapDraft.value = source ? { ...source } : { vendorNo: '', description: '', creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 }
}
function saveMap() {
  const draft = mapDraft.value
  const parent = activeRecord.value
  if (!draft || !parent) return
  const savingMode = mapMode.value
  if (!draft.vendorNo) {
    toast('請選擇供應商。', 'error')
    return
  }
  if (mapMode.value === 'add') {
    if (parent.vendors.some((record) => record.vendorNo === draft.vendorNo)) {
      toast('此備品已對應該供應商。', 'error')
      return
    }
    parent.vendors.push({ ...draft })
  } else {
    const record = parent.vendors.find((item) => item.vendorNo === draft.vendorNo)
    if (!record) return
    Object.assign(record, draft, { reviser: currentUser, reviseDate: nowLabel.value, dataStamp: record.dataStamp + 1 })
  }
  mapMode.value = null
  mapDraft.value = null
  selectedVendorNos.value = []
  toast(savingMode === 'edit' ? '供應商映射已修改。' : '供應商映射已新增。')
}
function deleteMaps() {
  const parent = activeRecord.value
  const targets = selectedVendorTargets.value
  if (!parent || !targets.length) return
  parent.vendors = parent.vendors.filter((record) => !targets.some((target) => target.vendorNo === record.vendorNo))
  selectedVendorNos.value = []
  toast(`已刪除 ${targets.length} 筆供應商映射。`)
}
</script>

<template>
  <section class="pm-basis" aria-label="備品基本資料 DEV 預覽">
    <header class="pm-basis__header">
      <div><p>PM 設定／SUBSTITUTION BASIS</p><h1>備品基本資料</h1></div>
      <div class="pm-basis__header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div>
    </header>

    <div class="pm-basis__filters">
      <label>備品代碼 <input v-model="searchCode" autocomplete="off" placeholder="輸入 SubstitutionNo"></label>
      <span class="pm-basis__count">{{ visibleRows.length }} 筆</span>
      <button type="button" class="workflow" @click="state.approvalWorkflowConfigured = !state.approvalWorkflowConfigured">簽核流程：{{ state.approvalWorkflowConfigured ? '送簽' : '直接生效' }}</button>
    </div>

    <nav class="pm-basis__tabs" aria-label="核准狀態">
      <button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }} <span>{{ state.records.filter((row) => row.issueState === tab.value).length }}</span></button>
    </nav>

    <div class="pm-basis__actions">
      <div><button type="button" class="primary" @click="openBasis('add')">新增</button><button type="button" :disabled="!canEdit" @click="openBasis('edit')">修改</button><button type="button" :disabled="!activeRecord" @click="openBasis('copy')">複製</button><button type="button" :disabled="selectedTargets.length === 0" @click="deleteBases">刪除</button><button type="button" :disabled="selectedTargets.length === 0" @click="approveBases">核准</button></div>
      <small>預設排序：備品代碼；核准流程可切換模擬。</small>
    </div>

    <div class="pm-basis__table-wrap master-wrap">
      <table class="master-table"><thead><tr><th class="check"><input type="checkbox" :checked="allVisibleSelected" aria-label="選取全部備品" @change="toggleAll"></th><th>SubstitutionNo</th><th>SubstitutionName</th><th>SubstitutionType</th><th>SafeQty</th><th>UnitNo</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>UnitType</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="record in visibleRows" :key="record.substitutionNo" :class="{ selected: activeCode === record.substitutionNo }" @click="selectRecord(record.substitutionNo)"><td class="check" @click.stop><input type="checkbox" :checked="selectedCodes.includes(record.substitutionNo)" :aria-label="`選取 ${record.substitutionNo}`" @change="toggleRecord(record.substitutionNo)"></td><td>{{ record.substitutionNo }}</td><td>{{ record.substitutionName }}</td><td>{{ record.substitutionType }}</td><td class="number">{{ Number(record.safeQty).toFixed(4).replace(/0+$/, '').replace(/\.$/, '') }}</td><td>{{ record.unitNo }}</td><td>{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.unitType }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td></tr>
          <tr v-if="visibleRows.length === 0"><td colspan="12" class="empty">沒有符合條件的備品資料</td></tr>
        </tbody>
      </table>
    </div>

    <section class="pm-basis__detail">
      <header><div><h2>供應商映射</h2><span>{{ activeRecord?.substitutionNo ?? '請先選取備品' }} · {{ visibleVendors.length }} 筆</span></div><div class="detail-actions"><button type="button" :disabled="!activeRecord" @click="openMap('add')">新增供應商</button><button type="button" :disabled="!selectedVendorNos.length" @click="openMap('edit')">修改映射</button><button type="button" :disabled="!selectedVendorNos.length" @click="deleteMaps">刪除映射</button></div></header>
      <div class="pm-basis__table-wrap detail-wrap"><table><thead><tr><th class="check">選取</th><th>SubstitutionNo</th><th>VendorNo</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
        <tbody><tr v-for="record in visibleVendors" :key="record.vendorNo" :class="{ selected: selectedVendorNos.includes(record.vendorNo) }" @click="selectedVendorNos = selectedVendorNos.includes(record.vendorNo) ? selectedVendorNos.filter((value) => value !== record.vendorNo) : [record.vendorNo]"><td class="check"><input type="checkbox" :checked="selectedVendorNos.includes(record.vendorNo)" :aria-label="`選取供應商 ${record.vendorNo}`" @click.stop="selectedVendorNos = selectedVendorNos.includes(record.vendorNo) ? selectedVendorNos.filter((value) => value !== record.vendorNo) : [...selectedVendorNos, record.vendorNo]"></td><td>{{ activeRecord?.substitutionNo }}</td><td>{{ record.vendorNo }}</td><td>{{ record.description }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td></tr><tr v-if="activeRecord && visibleVendors.length === 0"><td colspan="8" class="empty">此備品尚未設定供應商</td></tr><tr v-if="!activeRecord"><td colspan="8" class="empty">選取上方備品後查看供應商映射</td></tr></tbody>
      </table></div>
    </section>

    <p v-if="message" class="pm-basis__toast" :class="messageKind" role="status">{{ message }}</p>
    <p class="pm-basis__note">以上資料只存在目前瀏覽器 DEV localStorage。操作是本機模擬，不會寫入正式 MES 或資料庫。</p>

    <div v-if="basisMode && basisDraft" class="pm-basis__backdrop" @click.self="basisMode = null"><section class="pm-basis__dialog" role="dialog" aria-modal="true" :aria-label="basisMode === 'edit' ? '修改備品資料' : basisMode === 'copy' ? '複製備品資料' : '新增備品資料'">
      <header><h2>{{ basisMode === 'edit' ? '修改備品資料' : basisMode === 'copy' ? '複製備品資料' : '新增備品資料' }}</h2><button type="button" aria-label="關閉" @click="basisMode = null">×</button></header>
      <form @submit.prevent="saveBasis"><div class="form-grid">
        <label>備品代碼<input v-model="basisDraft.substitutionNo" :readonly="basisMode === 'edit'" required autocomplete="off" @blur="basisDraft.substitutionNo = basisDraft.substitutionNo.toUpperCase()"></label>
        <label>備品名稱<input v-model="basisDraft.substitutionName" required></label>
        <label>備品類別<select v-model="basisDraft.substitutionType" required><option value="" disabled>選擇類別</option><option v-for="option in typeChoices" :key="option">{{ option }}</option></select></label>
        <label>單位類別<select v-model="basisDraft.unitType" required @change="basisDraft.unitNo = ''"><option value="" disabled>選擇單位類別</option><option v-for="option in unitTypeChoices" :key="option">{{ option }}</option></select></label>
        <label>單位<select v-model="basisDraft.unitNo" required :disabled="!basisDraft.unitType"><option value="" disabled>選擇單位</option><option v-for="option in unitChoices[basisDraft.unitType] ?? []" :key="option">{{ option }}</option></select></label>
        <label>安全庫存量<input v-model.number="basisDraft.safeQty" type="number" min="0" step="0.0001"><small>留白或 0 會以 0 儲存</small></label>
        <label class="wide">說明<textarea v-model="basisDraft.description" rows="3"></textarea></label>
      </div><footer><button type="button" @click="basisMode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form>
    </section></div>

    <div v-if="mapMode && mapDraft" class="pm-basis__backdrop" @click.self="mapMode = null"><section class="pm-basis__dialog map-dialog" role="dialog" aria-modal="true" :aria-label="mapMode === 'edit' ? '修改供應商映射' : '新增供應商映射'">
      <header><h2>{{ mapMode === 'edit' ? '修改供應商映射' : '新增供應商映射' }}</h2><button type="button" aria-label="關閉" @click="mapMode = null">×</button></header>
      <form @submit.prevent="saveMap"><div class="form-grid"><label>備品代碼<input :value="activeRecord?.substitutionNo" readonly></label><label>供應商代碼<select v-model="mapDraft.vendorNo" :disabled="mapMode === 'edit'" required><option value="" disabled>選擇供應商</option><option v-for="option in vendorChoices" :key="option">{{ option }}</option></select></label><label class="wide">說明<textarea v-model="mapDraft.description" rows="3"></textarea></label></div><footer><button type="button" @click="mapMode = null">取消</button><button class="primary" type="submit">儲存</button></footer></form>
    </section></div>
  </section>
</template>

<style scoped>
.pm-basis { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 8px; padding: 10px 12px; box-sizing: border-box; background: #f7f5ee; color: #292820; font-size: 12px; }
.pm-basis button, .pm-basis input, .pm-basis select, .pm-basis textarea { font: inherit; }
.pm-basis button { min-height: 27px; padding: 4px 10px; border: 1px solid #bcb9ad; border-radius: 3px; background: #fffefa; color: #292820; cursor: pointer; white-space: nowrap; }
.pm-basis button:hover:not(:disabled) { background: #efede3; }
.pm-basis button:disabled { opacity: .48; cursor: not-allowed; }
.pm-basis button.primary { border-color: #3b3930; background: #3b3930; color: #fff; }
.pm-basis__header, .pm-basis__header-actions, .pm-basis__filters, .pm-basis__actions, .pm-basis__detail > header, .pm-basis__detail > header > div, .detail-actions { display: flex; align-items: center; }
.pm-basis__header { justify-content: space-between; border-bottom: 1px solid #d7d4c9; padding-bottom: 6px; }
.pm-basis__header p { margin: 0 0 2px; color: #777366; font-size: 10px; letter-spacing: .06em; }
.pm-basis__header h1 { margin: 0; font-size: 19px; }
.pm-basis__header-actions { gap: 9px; }
.dev-tag { color: #775b12; background: #f6edcf; border: 1px solid #e6d49d; border-radius: 20px; padding: 3px 8px; }
.pm-basis__filters { gap: 9px; }
.pm-basis__filters label { display: flex; align-items: center; gap: 7px; }
.pm-basis__filters input { width: 220px; height: 26px; padding: 3px 7px; border: 1px solid #c9c6bb; border-radius: 3px; background: #fffefa; }
.pm-basis__count, .pm-basis__actions small, .pm-basis__detail header span { color: #777366; }
.pm-basis__filters .workflow { margin-left: auto; }
.pm-basis__tabs { display: flex; gap: 3px; border-bottom: 1px solid #bcb9ad; }
.pm-basis__tabs button { border: 1px solid transparent; border-bottom: 0; border-radius: 3px 3px 0 0; background: transparent; }
.pm-basis__tabs button.active { border-color: #bcb9ad; background: #fffefa; font-weight: 650; }
.pm-basis__tabs span { color: #777366; padding-left: 3px; }
.pm-basis__actions { justify-content: space-between; gap: 10px; }
.pm-basis__actions > div { display: flex; gap: 5px; }
.pm-basis__table-wrap { min-height: 70px; overflow: auto; border: 1px solid #ceccc2; background: #fffefa; }
.master-wrap { flex: 1.2; }
.pm-basis table { width: 100%; border-collapse: collapse; white-space: nowrap; }
.pm-basis table.master-table { min-width: 1380px; }
.pm-basis th, .pm-basis td { padding: 5px 8px; border-bottom: 1px solid #e4e2d9; text-align: left; vertical-align: top; }
.pm-basis th { position: sticky; top: 0; z-index: 1; background: #eeece3; font-weight: 650; }
.pm-basis tbody tr { cursor: pointer; }
.pm-basis tbody tr:hover { background: #f7f5ec; }
.pm-basis tbody tr.selected { background: #e8edf2; }
.pm-basis .check { width: 30px; text-align: center; }
.pm-basis .number { text-align: right; font-variant-numeric: tabular-nums; }
.pm-basis .empty { padding: 16px; color: #888477; text-align: center; cursor: default; }
.pm-basis__detail { display: flex; flex: .8; min-height: 130px; flex-direction: column; gap: 6px; }
.pm-basis__detail > header { justify-content: space-between; gap: 10px; }
.pm-basis__detail > header > div:first-child { gap: 8px; }
.pm-basis__detail h2 { margin: 0; font-size: 14px; }
.detail-actions { gap: 5px; }
.detail-wrap { flex: 1; }
.pm-basis__toast { margin: 0; padding: 7px 9px; border: 1px solid #c9d9c8; background: #edf5eb; color: #315430; }
.pm-basis__toast.error { border-color: #e4c5bd; background: #fbefec; color: #863c2e; }
.pm-basis__note { margin: 0; color: #777366; font-size: 11px; }
.pm-basis__backdrop { position: fixed; inset: 0; z-index: 5000; display: grid; place-items: center; padding: 18px; background: #1f1e1a66; }
.pm-basis__dialog { width: min(590px, 96vw); max-height: 90vh; overflow: auto; border: 1px solid #aaa79b; border-radius: 5px; background: #fffefa; box-shadow: 0 15px 50px #0003; }
.pm-basis__dialog > header { display: flex; align-items: center; justify-content: space-between; padding: 11px 15px; border-bottom: 1px solid #dddacf; background: #f4f2e9; }
.pm-basis__dialog h2 { margin: 0; font-size: 15px; }
.pm-basis__dialog > header button { min-height: 24px; padding: 0 7px; font-size: 18px; }
.pm-basis__dialog form { padding: 15px 17px; }
.pm-basis .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 15px; }
.pm-basis .form-grid label { display: flex; flex-direction: column; gap: 5px; }
.pm-basis .form-grid .wide { grid-column: 1 / -1; }
.pm-basis .form-grid input, .pm-basis .form-grid select, .pm-basis .form-grid textarea { width: 100%; min-height: 29px; box-sizing: border-box; padding: 5px 7px; border: 1px solid #c9c6bb; border-radius: 3px; background: white; }
.pm-basis .form-grid textarea { resize: vertical; }
.pm-basis .form-grid small { color: #888477; font-size: 11px; }
.pm-basis__dialog form > footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 18px; padding-top: 12px; border-top: 1px solid #dddacf; }
@media (max-width: 720px) { .pm-basis { padding: 8px; } .pm-basis__actions { align-items: flex-start; flex-direction: column; } .pm-basis__detail > header { align-items: flex-start; flex-direction: column; } .pm-basis__filters input { width: min(44vw, 220px); } .pm-basis .form-grid { grid-template-columns: 1fr; } .pm-basis .form-grid .wide { grid-column: auto; } }
</style>
