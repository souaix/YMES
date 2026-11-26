<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type IssueState = 0 | 1 | 2 | -1
type MainMode = 'add' | 'edit' | 'copy'
type DetailMode = 'add' | 'edit' | 'copy'
interface ToolDetail { toolNo: string; toolQty: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface SubstitutionDetail { substitutionNo: string; unitNo: string; substitutionQty: number; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface MaintenanceItem {
  pmItemNo: string; pmItemName: string; maintainNo: string; pmJobNo: string; description: string
  creator: string; createDate: string; reviser: string; reviseDate: string; issueState: IssueState; dataStamp: number
  tools: ToolDetail[]; substitutions: SubstitutionDetail[]
}
interface LookupRow { code: string; name: string; unitNo?: string }
interface DevState { version: 1; approvalWorkflowConfigured: boolean; records: MaintenanceItem[] }
interface MainDraft { pmItemNo: string; pmItemName: string; maintainNo: string; pmJobNo: string; description: string }
interface ToolDraft { toolNo: string; toolQty: number | string }
interface SubDraft { substitutionNo: string; unitNo: string; substitutionQty: number | string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.maintenance-item.v1'
const currentUser = 'DEV01'
const issueTabs: Array<{ value: IssueState; label: string }> = [
  { value: 0, label: '未凍結' }, { value: 1, label: '簽核中' }, { value: 2, label: '已生效' }, { value: -1, label: '未使用' },
]
const toolCatalog: LookupRow[] = [
  { code: 'TL-0001', name: '扭力扳手' }, { code: 'TL-0012', name: '數位卡尺' }, { code: 'TL-0038', name: '絕緣電阻計' }, { code: 'TL-0105', name: '潤滑油槍' },
]
const fallbackMaintainTypes: LookupRow[] = [
  { code: 'CALIBRATION', name: '校正保養' }, { code: 'CLEANING', name: '清潔保養' }, { code: 'LUBRICATION', name: '潤滑保養' },
]
const fallbackJobTypes: LookupRow[] = [
  { code: 'CALIBRATION', name: '設備校正' }, { code: 'CLEANING', name: '設備清潔' }, { code: 'LUBRICATION', name: '潤滑保養' },
]
const fallbackSubstitutions: LookupRow[] = [
  { code: 'BRG-6204', name: '深溝滾珠軸承 6204', unitNo: 'EA' }, { code: 'MTR-1KW', name: 'AC 馬達 1kW', unitNo: 'EA' }, { code: 'PNEU-SV01', name: '電磁閥維修組', unitNo: 'SET' },
]

function seedState(): DevState {
  return {
    version: 1,
    approvalWorkflowConfigured: false,
    records: [
      { pmItemNo: 'PM-CLEAN-01', pmItemName: '腔體定期清潔', maintainNo: 'CLEANING', pmJobNo: 'CLEANING', description: '清潔腔體與載台，完成後確認無殘留物。', creator: 'PMUSER', createDate: '2026-11-12 09:15', reviser: 'PMUSER', reviseDate: '2026-11-16 13:40', issueState: 0, dataStamp: 2,
        tools: [{ toolNo: 'TL-0012', toolQty: 1, creator: 'PMUSER', createDate: '2026-11-12 09:18', reviser: '', reviseDate: '', dataStamp: 0 }],
        substitutions: [{ substitutionNo: 'BRG-6204', unitNo: 'EA', substitutionQty: 2, creator: 'PMUSER', createDate: '2026-11-12 09:20', reviser: '', reviseDate: '', dataStamp: 0 }] },
      { pmItemNo: 'PM-LUBE-03', pmItemName: '主軸潤滑保養', maintainNo: 'LUBRICATION', pmJobNo: 'LUBRICATION', description: '依週期補充規定油脂並記錄用量。', creator: 'PMUSER', createDate: '2026-11-18 14:05', reviser: '', reviseDate: '', issueState: 0, dataStamp: 0,
        tools: [{ toolNo: 'TL-0105', toolQty: 1, creator: 'PMUSER', createDate: '2026-11-18 14:10', reviser: '', reviseDate: '', dataStamp: 0 }],
        substitutions: [{ substitutionNo: 'PNEU-SV01', unitNo: 'SET', substitutionQty: 1, creator: 'PMUSER', createDate: '2026-11-18 14:12', reviser: '', reviseDate: '', dataStamp: 0 }] },
      { pmItemNo: 'PM-CAL-02', pmItemName: '感測器校正', maintainNo: 'CALIBRATION', pmJobNo: 'CALIBRATION', description: '以標準器確認輸出值並貼附校正標籤。', creator: 'DEV02', createDate: '2026-11-21 08:30', reviser: '', reviseDate: '', issueState: 1, dataStamp: 1,
        tools: [{ toolNo: 'TL-0038', toolQty: 1, creator: 'DEV02', createDate: '2026-11-21 08:32', reviser: '', reviseDate: '', dataStamp: 0 }], substitutions: [] },
      { pmItemNo: 'PM-OLD-00', pmItemName: '舊式風扇檢查', maintainNo: 'CLEANING', pmJobNo: 'CLEANING', description: '停用範例。', creator: 'PMUSER', createDate: '2026-08-01 10:00', reviser: 'PMUSER', reviseDate: '2026-10-01 10:00', issueState: -1, dataStamp: 3, tools: [], substitutions: [] },
      { pmItemNo: 'PM-PUMP-01', pmItemName: '冷卻泵更換', maintainNo: 'CLEANING', pmJobNo: 'CLEANING', description: '已生效項目供狀態轉換操作示範。', creator: 'PMUSER', createDate: '2026-09-14 11:00', reviser: 'PMUSER', reviseDate: '2026-09-15 09:00', issueState: 2, dataStamp: 2, tools: [], substitutions: [] },
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
  } catch { /* Fall back to fixtures when local storage is unavailable. */ }
  return seedState()
}
function readLookup(key: string, fallback: LookupRow[], codeKey: string, nameKey: string, unitKey?: string): LookupRow[] {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(key) ?? 'null') as { records?: Record<string, unknown>[] } | null
    if (Array.isArray(parsed?.records)) return parsed.records.map((row) => ({
      code: String(row[codeKey] ?? ''), name: String(row[nameKey] ?? ''), ...(unitKey ? { unitNo: String(row[unitKey] ?? '') } : {}),
    })).filter((row) => row.code)
  } catch { /* Use a local DEV lookup fixture. */ }
  return fallback
}

const state = ref<DevState>(loadState())
const issueState = ref<IssueState>(0)
const searchCode = ref('')
const activeCode = ref('')
const selectedCodes = ref<string[]>([])
const selectedToolNos = ref<string[]>([])
const selectedSubstitutionNos = ref<string[]>([])
const mainMode = ref<MainMode | null>(null)
const mainDraft = ref<MainDraft | null>(null)
const toolMode = ref<DetailMode | null>(null)
const toolDraft = ref<ToolDraft | null>(null)
const substitutionMode = ref<DetailMode | null>(null)
const substitutionDraft = ref<SubDraft | null>(null)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const maintainChoices = computed(() => readLookup('ymes.dev.pm.maintenance-type.v1', fallbackMaintainTypes, 'maintainNo', 'maintainName'))
const jobChoices = computed(() => readLookup('ymes.dev.pm.job-type.v1', fallbackJobTypes, 'pmJobNo', 'pmJobName'))
const toolChoices = computed(() => readLookup('ymes.dev.eqp.tool-basis.v1', toolCatalog, 'toolNo', 'toolName'))
const substitutionChoices = computed(() => readLookup('ymes.dev.pm.substitution-basis.v1', fallbackSubstitutions, 'substitutionNo', 'substitutionName', 'unitNo'))
const visibleRows = computed(() => state.value.records
  .filter((record) => record.issueState === issueState.value)
  .filter((record) => !searchCode.value.trim() || record.pmItemNo.toLocaleLowerCase().includes(searchCode.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.pmItemNo.localeCompare(b.pmItemNo)))
const activeRecord = computed(() => state.value.records.find((record) => record.pmItemNo === activeCode.value) ?? null)
const activeTools = computed(() => activeRecord.value?.tools ?? [])
const activeSubstitutions = computed(() => activeRecord.value?.substitutions ?? [])
const selectedMainTargets = computed(() => {
  const selected = state.value.records.filter((record) => selectedCodes.value.includes(record.pmItemNo))
  return selected.length ? selected : activeRecord.value ? [activeRecord.value] : []
})
const selectedTools = computed(() => activeTools.value.filter((row) => selectedToolNos.value.includes(row.toolNo)))
const selectedSubstitutions = computed(() => activeSubstitutions.value.filter((row) => selectedSubstitutionNos.value.includes(row.substitutionNo)))
const allVisibleSelected = computed(() => visibleRows.value.length > 0 && visibleRows.value.every((record) => selectedCodes.value.includes(record.pmItemNo)))
const issueStateLabel = (value: IssueState) => issueTabs.find((tab) => tab.value === value)?.label ?? '未知'
const maintainName = (code: string) => maintainChoices.value.find((row) => row.code === code)?.name ?? ''
const jobName = (code: string) => jobChoices.value.find((row) => row.code === code)?.name ?? ''
const substitutionName = (code: string) => substitutionChoices.value.find((row) => row.code === code)?.name ?? ''

watch(state, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* Keep DEV edits in memory for this session. */ }
}, { deep: true })
watch(visibleRows, (rows) => {
  if (!rows.some((record) => record.pmItemNo === activeCode.value)) activeCode.value = rows[0]?.pmItemNo ?? ''
  selectedCodes.value = selectedCodes.value.filter((code) => rows.some((record) => record.pmItemNo === code))
}, { immediate: true })
watch(activeCode, () => { selectedToolNos.value = []; selectedSubstitutionNos.value = [] })

function toast(text: string, kind: 'success' | 'error' = 'success') {
  message.value = text
  messageKind.value = kind
  window.setTimeout(() => { if (message.value === text) message.value = '' }, 3600)
}
function selectIssueState(value: IssueState) { issueState.value = value; selectedCodes.value = [] }
function selectRecord(code: string) { activeCode.value = code; if (!selectedCodes.value.includes(code)) selectedCodes.value = [] }
function toggleRecord(code: string) { selectedCodes.value = selectedCodes.value.includes(code) ? selectedCodes.value.filter((value) => value !== code) : [...selectedCodes.value, code] }
function toggleAll() { selectedCodes.value = allVisibleSelected.value ? [] : visibleRows.value.map((record) => record.pmItemNo) }

function openMain(mode: MainMode) {
  const source = activeRecord.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.issueState !== 0) { toast('只有未凍結資料可以修改。', 'error'); return }
  mainMode.value = mode
  mainDraft.value = mode === 'add'
    ? { pmItemNo: '', pmItemName: '', maintainNo: '', pmJobNo: '', description: '' }
    : { pmItemNo: mode === 'copy' ? '' : source!.pmItemNo, pmItemName: source!.pmItemName, maintainNo: source!.maintainNo, pmJobNo: source!.pmJobNo, description: source!.description }
}
function saveMain(closeAfterSave: boolean) {
  const draft = mainDraft.value
  if (!draft) return
  draft.pmItemNo = draft.pmItemNo.toUpperCase()
  if (draft.pmItemNo === '' || draft.pmItemName === '' || draft.maintainNo === '' || draft.pmJobNo === '') {
    toast('PM Item No、PM Item Name、Maintain No 與 PM Job No 均為必填。', 'error')
    return
  }
  const oldCode = mainMode.value === 'edit' ? activeCode.value : ''
  if (state.value.records.some((record) => record.pmItemNo.toUpperCase() === draft.pmItemNo && record.pmItemNo !== oldCode)) { toast('PM Item No 已存在。', 'error'); return }
  if (mainMode.value === 'edit' && activeRecord.value) {
    const record = activeRecord.value
    record.pmItemName = draft.pmItemName
    record.maintainNo = draft.maintainNo
    record.pmJobNo = draft.pmJobNo
    // Legacy Core omits Description when the submitted value is empty, so blank does not clear it.
    if (draft.description !== '') record.description = draft.description
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
    toast('保養項目已在 DEV 假資料中修改。')
  } else {
    const source = mainMode.value === 'copy' ? activeRecord.value : null
    const record: MaintenanceItem = {
      pmItemNo: draft.pmItemNo, pmItemName: draft.pmItemName, maintainNo: draft.maintainNo, pmJobNo: draft.pmJobNo, description: draft.description,
      creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', issueState: 0, dataStamp: 0,
      tools: source ? source.tools.map((row) => ({ ...row, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })) : [],
      substitutions: source ? source.substitutions.map((row) => ({ ...row, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })) : [],
    }
    state.value.records.push(record)
    searchCode.value = ''
    issueState.value = 0
    activeCode.value = record.pmItemNo
    toast(source ? '保養項目與兩種明細已在 DEV 假資料中複製。' : '保養項目已新增至 DEV 假資料。')
  }
  if (closeAfterSave) { mainMode.value = null; mainDraft.value = null }
}
function deleteMainRows() {
  for (const record of selectedMainTargets.value) {
    if (record.issueState === 1) { toast(`簽核中資料 ${record.pmItemNo} 不可刪除；此前已成功的列不回復。`, 'error'); return }
    if (record.issueState === 2) {
      record.issueState = -1
      record.reviser = currentUser
      record.reviseDate = nowLabel.value
      record.dataStamp += 1
    } else {
      state.value.records = state.value.records.filter((item) => item.pmItemNo !== record.pmItemNo)
      if (activeCode.value === record.pmItemNo) activeCode.value = ''
    }
  }
  selectedCodes.value = []
  toast('所選主檔已依狀態刪除或移至未使用。')
}
function approveMainRows() {
  for (const record of selectedMainTargets.value) {
    if (record.issueState !== 0) { toast(`只有未凍結資料可核准；${record.pmItemNo} 之後的列未處理。`, 'error'); return }
    record.issueState = state.value.approvalWorkflowConfigured ? 1 : 2
    record.reviser = currentUser
    record.reviseDate = nowLabel.value
    record.dataStamp += 1
  }
  selectedCodes.value = []
  toast(state.value.approvalWorkflowConfigured ? '保養項目已模擬送入簽核中。' : '未設定簽核流程，保養項目直接模擬生效。')
}

function openTool(mode: DetailMode) {
  if (!activeRecord.value) return
  const source = mode === 'edit' ? selectedTools.value[0] : mode === 'copy' ? activeTools.value.find((row) => row.toolNo === selectedToolNos.value[0]) : undefined
  if (mode !== 'add' && !source) { toast('請先選取一筆工具明細。', 'error'); return }
  toolMode.value = mode
  toolDraft.value = source ? { toolNo: source.toolNo, toolQty: source.toolQty } : { toolNo: '', toolQty: 1 }
}
function saveTool(closeAfterSave: boolean) {
  const parent = activeRecord.value
  const draft = toolDraft.value
  if (!parent || !draft) return
  const qty = Number(draft.toolQty)
  if (!draft.toolNo || !Number.isInteger(qty) || qty <= 0) { toast('請選擇 Tool No，並輸入大於 0 的整數 Tool Qty。', 'error'); return }
  const original = toolMode.value === 'edit' ? selectedTools.value[0] : null
  if (parent.tools.some((row) => row.toolNo === draft.toolNo && row !== original)) { toast('此保養項目已存在該工具。', 'error'); return }
  if (original) {
    Object.assign(original, { toolQty: qty, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: original.dataStamp + 1 })
    toast('工具數量已更新。')
  } else {
    parent.tools.push({ toolNo: draft.toolNo, toolQty: qty, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })
    toast(toolMode.value === 'copy' ? '工具明細已複製。' : '工具明細已新增。')
  }
  selectedToolNos.value = []
  if (closeAfterSave) { toolMode.value = null; toolDraft.value = null }
}
function deleteTools() {
  const parent = activeRecord.value
  const targets = selectedTools.value
  if (!parent || !targets.length) return
  parent.tools = parent.tools.filter((row) => !targets.includes(row))
  selectedToolNos.value = []
  toast(`已刪除 ${targets.length} 筆工具明細。`)
}

function chooseSubstitution(code: string) {
  if (!substitutionDraft.value) return
  substitutionDraft.value.substitutionNo = code
  substitutionDraft.value.unitNo = substitutionChoices.value.find((row) => row.code === code)?.unitNo ?? ''
}
function openSubstitution(mode: DetailMode) {
  if (!activeRecord.value) return
  const source = mode === 'add' ? undefined : activeSubstitutions.value.find((row) => row.substitutionNo === selectedSubstitutionNos.value[0])
  if (mode !== 'add' && !source) { toast('請先選取一筆替代備品明細。', 'error'); return }
  substitutionMode.value = mode
  substitutionDraft.value = source
    ? { substitutionNo: source.substitutionNo, unitNo: source.unitNo, substitutionQty: source.substitutionQty }
    : { substitutionNo: '', unitNo: '', substitutionQty: 1 }
}
function saveSubstitution(closeAfterSave: boolean) {
  const parent = activeRecord.value
  const draft = substitutionDraft.value
  if (!parent || !draft) return
  const qty = Number(draft.substitutionQty)
  if (!draft.substitutionNo || !draft.unitNo || !Number.isInteger(qty) || qty <= 0) { toast('請選擇 Substitution No，並輸入大於 0 的整數數量。', 'error'); return }
  const original = substitutionMode.value === 'edit' ? activeSubstitutions.value.find((row) => row.substitutionNo === selectedSubstitutionNos.value[0]) : null
  if (parent.substitutions.some((row) => row.substitutionNo === draft.substitutionNo && row !== original)) { toast('此保養項目已存在該替代備品。', 'error'); return }
  if (original) {
    Object.assign(original, { unitNo: draft.unitNo, substitutionQty: qty, reviser: currentUser, reviseDate: nowLabel.value, dataStamp: original.dataStamp + 1 })
    toast('替代備品數量已更新。')
  } else {
    parent.substitutions.push({ substitutionNo: draft.substitutionNo, unitNo: draft.unitNo, substitutionQty: qty, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })
    toast(substitutionMode.value === 'copy' ? '替代備品明細已複製。' : '替代備品明細已新增。')
  }
  selectedSubstitutionNos.value = []
  if (closeAfterSave) { substitutionMode.value = null; substitutionDraft.value = null }
}
function deleteSubstitutions() {
  const parent = activeRecord.value
  const targets = selectedSubstitutions.value
  if (!parent || !targets.length) return
  parent.substitutions = parent.substitutions.filter((row) => !targets.includes(row))
  selectedSubstitutionNos.value = []
  toast(`已刪除 ${targets.length} 筆替代備品明細。`)
}
</script>

<template>
  <section class="pm-maintenance-item" aria-label="預防保養項目資料 DEV 預覽">
    <header class="page-header"><div><p>PM 設定／PM ITEM</p><h1>預防保養項目資料</h1></div><div class="header-actions"><span class="dev-tag">DEV 假資料</span><button type="button" class="close" @click="emit('close')">Close (X)</button></div></header>
    <div class="filter-row"><label>PM Item No <input v-model="searchCode" autocomplete="off" placeholder="搜尋保養項目代碼"></label><span class="row-count">{{ visibleRows.length }} 筆</span></div>
    <nav class="issue-tabs" aria-label="簽核狀態"><button v-for="tab in issueTabs" :key="tab.value" type="button" :class="{ active: issueState === tab.value }" @click="selectIssueState(tab.value)">{{ tab.label }}</button></nav>
    <div class="toolbar"><button type="button" @click="openMain('add')">新增</button><button type="button" @click="openMain('edit')">修改</button><button type="button" @click="openMain('copy')">複製</button><button type="button" :disabled="selectedMainTargets.length === 0" @click="deleteMainRows">刪除</button><button type="button" :disabled="selectedMainTargets.length === 0" @click="approveMainRows">核准</button><label class="workflow-toggle"><input v-model="state.approvalWorkflowConfigured" type="checkbox"> DEV 模擬已設定簽核流程</label></div>
    <div class="master-wrap"><table><thead><tr><th class="check-col"><input type="checkbox" :checked="allVisibleSelected" aria-label="全選" @change="toggleAll"></th><th>PM Item No</th><th>PM Item Name</th><th>Maintain No</th><th>Maintain Name</th><th>PM Job No</th><th>PM Job Name</th><th>Creator</th><th>Create Date</th><th>Description</th><th>Reviser</th><th>Revise Date</th><th>狀態</th></tr></thead><tbody>
      <tr v-for="record in visibleRows" :key="record.pmItemNo" :class="{ selected: activeCode === record.pmItemNo }" @click="selectRecord(record.pmItemNo)"><td class="check-col" @click.stop><input type="checkbox" :checked="selectedCodes.includes(record.pmItemNo)" :aria-label="`選取 ${record.pmItemNo}`" @change="toggleRecord(record.pmItemNo)"></td><td>{{ record.pmItemNo }}</td><td>{{ record.pmItemName }}</td><td>{{ record.maintainNo }}</td><td>{{ maintainName(record.maintainNo) }}</td><td>{{ record.pmJobNo }}</td><td>{{ jobName(record.pmJobNo) }}</td><td>{{ record.creator }}</td><td>{{ record.createDate }}</td><td class="description-cell">{{ record.description }}</td><td>{{ record.reviser }}</td><td>{{ record.reviseDate }}</td><td><span class="state-pill" :class="`state-${record.issueState}`">{{ issueStateLabel(record.issueState) }}</span></td></tr>
      <tr v-if="visibleRows.length === 0"><td colspan="13" class="empty">沒有符合條件的保養項目。</td></tr>
    </tbody></table></div>

    <div class="detail-panels">
      <section class="detail-panel"><header><div><p>TOOL USED</p><h2>工具明細 <small>{{ activeTools.length }} 筆</small></h2></div><div class="detail-actions"><button type="button" :disabled="!activeRecord" @click="openTool('add')">新增</button><button type="button" :disabled="selectedToolNos.length !== 1" @click="openTool('edit')">修改</button><button type="button" :disabled="selectedToolNos.length !== 1" @click="openTool('copy')">複製</button><button type="button" :disabled="!selectedToolNos.length" @click="deleteTools">刪除</button></div></header><div class="detail-table"><table><thead><tr><th><span class="sr-only">選取</span></th><th>Tool No</th><th>Tool Qty</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead><tbody>
        <tr v-for="row in activeTools" :key="row.toolNo" :class="{ selected: selectedToolNos.includes(row.toolNo) }" @click="selectedToolNos = [row.toolNo]"><td @click.stop><input type="checkbox" :checked="selectedToolNos.includes(row.toolNo)" :aria-label="`選取工具 ${row.toolNo}`" @change="selectedToolNos = selectedToolNos.includes(row.toolNo) ? selectedToolNos.filter((value) => value !== row.toolNo) : [...selectedToolNos, row.toolNo]"></td><td>{{ row.toolNo }}</td><td>{{ row.toolQty }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td></tr>
        <tr v-if="!activeTools.length"><td colspan="7" class="empty">{{ activeRecord ? '此項目尚無工具明細。' : '請先選取保養項目。' }}</td></tr>
      </tbody></table></div></section>

      <section class="detail-panel"><header><div><p>SUBSTITUTION USED</p><h2>替代備品明細 <small>{{ activeSubstitutions.length }} 筆</small></h2></div><div class="detail-actions"><button type="button" :disabled="!activeRecord" @click="openSubstitution('add')">新增</button><button type="button" :disabled="selectedSubstitutionNos.length !== 1" @click="openSubstitution('edit')">修改</button><button type="button" :disabled="selectedSubstitutionNos.length !== 1" @click="openSubstitution('copy')">複製</button><button type="button" :disabled="!selectedSubstitutionNos.length" @click="deleteSubstitutions">刪除</button></div></header><div class="detail-table"><table><thead><tr><th><span class="sr-only">選取</span></th><th>Substitution No</th><th>Unit No</th><th>Qty</th><th>Creator</th><th>Create Date</th><th>Reviser</th><th>Revise Date</th></tr></thead><tbody>
        <tr v-for="row in activeSubstitutions" :key="row.substitutionNo" :class="{ selected: selectedSubstitutionNos.includes(row.substitutionNo) }" @click="selectedSubstitutionNos = [row.substitutionNo]"><td @click.stop><input type="checkbox" :checked="selectedSubstitutionNos.includes(row.substitutionNo)" :aria-label="`選取替代備品 ${row.substitutionNo}`" @change="selectedSubstitutionNos = selectedSubstitutionNos.includes(row.substitutionNo) ? selectedSubstitutionNos.filter((value) => value !== row.substitutionNo) : [...selectedSubstitutionNos, row.substitutionNo]"></td><td>{{ row.substitutionNo }}</td><td>{{ row.unitNo }}</td><td>{{ row.substitutionQty }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td></tr>
        <tr v-if="!activeSubstitutions.length"><td colspan="8" class="empty">{{ activeRecord ? '此項目尚無替代備品明細。' : '請先選取保養項目。' }}</td></tr>
      </tbody></table></div></section>
    </div>
    <p v-if="message" class="toast" :class="messageKind" role="status">{{ message }}</p>

    <div v-if="mainMode && mainDraft" class="modal-backdrop" @click.self="mainMode = null; mainDraft = null"><section class="edit-dialog" role="dialog" aria-modal="true" aria-labelledby="main-title"><header><div><p>PM ITEM / {{ mainMode.toUpperCase() }}</p><h2 id="main-title">{{ mainMode === 'add' ? '新增保養項目' : mainMode === 'edit' ? '修改保養項目' : '複製保養項目' }}</h2></div><button type="button" class="close" aria-label="關閉" @click="mainMode = null; mainDraft = null">×</button></header>
      <label>PM Item No <input v-model="mainDraft.pmItemNo" :readonly="mainMode === 'edit'" autocomplete="off" @blur="mainDraft.pmItemNo = mainDraft.pmItemNo.toUpperCase()"></label><label>PM Item Name <input v-model="mainDraft.pmItemName" autocomplete="off"></label><label>Maintain No <select v-model="mainDraft.maintainNo"><option value="">選擇維護方式</option><option v-for="row in maintainChoices" :key="row.code" :value="row.code">{{ row.code }} — {{ row.name }}</option></select></label><label>PM Job No <select v-model="mainDraft.pmJobNo"><option value="">選擇工作形式</option><option v-for="row in jobChoices" :key="row.code" :value="row.code">{{ row.code }} — {{ row.name }}</option></select></label><label>Description <textarea v-model="mainDraft.description" rows="4"></textarea></label><footer><button type="button" class="secondary" @click="mainMode = null; mainDraft = null">取消</button><button type="button" class="secondary" @click="saveMain(false)">Apply</button><button type="button" @click="saveMain(true)">Confirm</button></footer>
    </section></div>

    <div v-if="toolMode && toolDraft" class="modal-backdrop" @click.self="toolMode = null; toolDraft = null"><section class="edit-dialog compact" role="dialog" aria-modal="true" aria-labelledby="tool-title"><header><div><p>TOOL USED / {{ toolMode.toUpperCase() }}</p><h2 id="tool-title">工具使用明細</h2></div><button type="button" class="close" aria-label="關閉" @click="toolMode = null; toolDraft = null">×</button></header><label>PM Item No <input :value="activeRecord?.pmItemNo" readonly></label><label>Tool No <select v-model="toolDraft.toolNo" :disabled="toolMode === 'edit'"><option value="">選擇工具</option><option v-for="row in toolChoices" :key="row.code" :value="row.code">{{ row.code }} — {{ row.name }}</option></select></label><label>Tool Qty <input v-model="toolDraft.toolQty" type="number" min="1" step="1"></label><footer><button type="button" class="secondary" @click="toolMode = null; toolDraft = null">取消</button><button type="button" class="secondary" @click="saveTool(false)">Apply</button><button type="button" @click="saveTool(true)">Confirm</button></footer></section></div>

    <div v-if="substitutionMode && substitutionDraft" class="modal-backdrop" @click.self="substitutionMode = null; substitutionDraft = null"><section class="edit-dialog compact" role="dialog" aria-modal="true" aria-labelledby="sub-title"><header><div><p>SUBSTITUTION USED / {{ substitutionMode.toUpperCase() }}</p><h2 id="sub-title">替代備品使用明細</h2></div><button type="button" class="close" aria-label="關閉" @click="substitutionMode = null; substitutionDraft = null">×</button></header><label>PM Item No <input :value="activeRecord?.pmItemNo" readonly></label><label>Substitution No <select :value="substitutionDraft.substitutionNo" :disabled="substitutionMode === 'edit'" @change="chooseSubstitution(($event.target as HTMLSelectElement).value)"><option value="">選擇替代備品</option><option v-for="row in substitutionChoices" :key="row.code" :value="row.code">{{ row.code }} — {{ row.name }}</option></select></label><label>Unit No <input v-model="substitutionDraft.unitNo" readonly></label><label>Substitution Qty <input v-model="substitutionDraft.substitutionQty" type="number" min="1" step="1"></label><footer><button type="button" class="secondary" @click="substitutionMode = null; substitutionDraft = null">取消</button><button type="button" class="secondary" @click="saveSubstitution(false)">Apply</button><button type="button" @click="saveSubstitution(true)">Confirm</button></footer></section></div>

    <footer class="page-footer"><span>DEV localStorage：{{ storageKey }}</span><span>PMItemNo 排序｜DataStamp {{ activeRecord?.dataStamp ?? '—' }}｜明細可由各自的維護視窗編輯</span></footer>
  </section>
</template>

<style scoped>
.pm-maintenance-item { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 9px; padding: 12px; box-sizing: border-box; background: #f7f5ee; color: #26251f; font: 12px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-header, .header-actions, .filter-row, .toolbar, .page-footer, .detail-panel > header, .detail-actions, .edit-dialog header, .edit-dialog footer { display: flex; align-items: center; }
.page-header { justify-content: space-between; border-bottom: 1px solid #d6d2c6; padding-bottom: 8px; }.page-header p, .detail-panel header p, .edit-dialog header p { margin: 0; color: #777366; font-size: 10px; letter-spacing: .06em; }h1 { margin: 2px 0 0; font-size: 20px; font-weight: 600; }.header-actions { gap: 9px; }.dev-tag { padding: 3px 8px; border-radius: 3px; background: #dce9de; color: #28533a; font-size: 10px; }
button { min-height: 27px; padding: 4px 9px; border: 1px solid #454237; border-radius: 3px; background: #454237; color: #fff; font: inherit; cursor: pointer; white-space: nowrap; }button:disabled { opacity: .42; cursor: default; }button.secondary, .close { border-color: #bab6a8; background: #fffef9; color: #333126; }
.filter-row { gap: 10px; }.filter-row label, .edit-dialog label { display: grid; gap: 3px; color: #514f45; }.filter-row input { width: 235px; }.row-count { margin-top: 14px; color: #777366; }input:not([type=checkbox]), textarea, select { min-height: 28px; padding: 4px 7px; box-sizing: border-box; border: 1px solid #bbb7a9; border-radius: 3px; background: #fffef9; color: #26251f; font: inherit; }textarea { resize: vertical; }.issue-tabs, .toolbar { display: flex; gap: 5px; align-items: center; flex-wrap: wrap; }.issue-tabs button { border-color: #c4c0b4; background: #eeece4; color: #49473e; }.issue-tabs button.active { border-color: #555144; background: #555144; color: #fff; }.workflow-toggle { margin-left: auto; display: inline-flex; align-items: center; gap: 4px; color: #6c695d; font-size: 11px; }
.master-wrap, .detail-table { min-height: 70px; overflow: auto; border: 1px solid #d0ccbf; background: #fffef9; }.master-wrap { flex: 1; }table { width: 100%; border-collapse: collapse; white-space: nowrap; }th, td { padding: 5px 7px; border-bottom: 1px solid #e7e4db; text-align: left; }th { position: sticky; top: 0; z-index: 1; background: #e9e6dc; color: #555246; font-weight: 600; }tbody tr { cursor: pointer; }tbody tr:hover { background: #f5f2e8; }tbody tr.selected { background: #e8edf0; }.check-col, .detail-table th:first-child, .detail-table td:first-child { width: 25px; text-align: center; }.description-cell { min-width: 180px; max-width: 310px; white-space: normal; }.state-pill { display: inline-block; padding: 2px 7px; border-radius: 10px; background: #e8e6df; color: #555246; font-size: 10px; }.state-0 { background: #eee6cf; color: #705716; }.state-1 { background: #e2e8f1; color: #394e71; }.state-2 { background: #dce9de; color: #28533a; }.state--1 { background: #ece8e5; color: #78685d; }.empty { padding: 14px; text-align: center; color: #777366; }
.detail-panels { height: 220px; min-height: 150px; display: grid; grid-template-columns: minmax(320px, .8fr) minmax(480px, 1.2fr); gap: 8px; }.detail-panel { min-height: 0; display: flex; flex-direction: column; gap: 6px; padding: 7px; border: 1px solid #d0ccbf; background: #f1efe7; }.detail-panel > header { justify-content: space-between; gap: 6px; }.detail-panel h2 { margin: 1px 0 0; font-size: 14px; font-weight: 600; }.detail-panel h2 small { color: #777366; font-size: 10px; font-weight: 400; }.detail-actions { gap: 4px; }.detail-actions button { min-height: 24px; padding: 2px 7px; font-size: 10px; }.detail-table { flex: 1; min-height: 0; }.detail-table th, .detail-table td { padding: 4px 6px; }.toast { margin: 0; padding: 7px 9px; border-radius: 3px; background: #e4eee4; color: #28533a; }.toast.error { background: #f4e5df; color: #8a3527; }.page-footer { justify-content: space-between; color: #8a877c; font-size: 10px; }.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; }
.modal-backdrop { position: fixed; inset: 0; z-index: 120; display: grid; place-items: center; padding: 16px; background: #28271f80; }.edit-dialog { width: min(500px, 100%); display: grid; gap: 10px; padding: 14px; border: 1px solid #b8b4a7; border-radius: 5px; background: #f7f5ee; box-shadow: 0 18px 60px #0004; }.edit-dialog.compact { width: min(420px, 100%); }.edit-dialog header { justify-content: space-between; border-bottom: 1px solid #d6d2c6; padding-bottom: 8px; }.edit-dialog h2 { margin: 2px 0 0; font-size: 17px; }.edit-dialog footer { justify-content: flex-end; gap: 6px; padding-top: 4px; }
@media (max-width: 1000px) { .detail-panels { height: 300px; grid-template-columns: 1fr; }.master-wrap { min-height: 190px; }.pm-maintenance-item { padding: 8px; }.workflow-toggle { margin-left: 0; }.page-footer { align-items: flex-start; flex-direction: column; gap: 2px; } }
</style>
