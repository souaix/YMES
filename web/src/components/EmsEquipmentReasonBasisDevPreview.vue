<script setup lang="ts">
import { computed, ref } from 'vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

type IssueState = 0 | 1 | 2 | -1
type DialogMode = 'add' | 'edit' | 'copy'
interface ReasonRow {
  reasonNo: string
  reasonName: string
  reasonType: 6
  reasonSubType: string
  reasonSubTypeName: string
  reasonLevel: number
  description: string
  issueState: IssueState
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}
interface ReasonForm {
  reasonNo: string
  reasonName: string
  reasonSubType: string
  reasonSubTypeName: string
  reasonLevel: number
  description: string
}

const issueTabs: { state: IssueState; label: string }[] = [
  { state: 0, label: '未凍結' },
  { state: 1, label: '簽核中' },
  { state: 2, label: '已核准' },
  { state: -1, label: '已停用' },
]
const subtypeOptions = [
  { code: 'BREAKDOWN', name: '故障原因' },
  { code: 'MAINTENANCE', name: '保養原因' },
  { code: 'PRODUCTION', name: '生產狀態說明' },
  { code: 'QUALITY', name: '品質異常' },
]
const now = new Date()
const dateString = (hoursAgo: number) => new Date(now.getTime() - hoursAgo * 3_600_000).toISOString()
const reasonRows = ref<ReasonRow[]>([
  { reasonNo: 'EQP-001', reasonName: '真空壓力異常', reasonType: 6, reasonSubType: 'BREAKDOWN', reasonSubTypeName: '故障原因', reasonLevel: 1, description: '檢查真空泵、管路及壓力感測器。', issueState: 0, creator: 'DEV_USER', createDate: dateString(35), reviser: '', reviseDate: '', dataStamp: 2 },
  { reasonNo: 'EQP-002', reasonName: '定期保養清潔', reasonType: 6, reasonSubType: 'MAINTENANCE', reasonSubTypeName: '保養原因', reasonLevel: 2, description: '完成腔體清潔與保養點檢。', issueState: 0, creator: 'DEV_USER', createDate: dateString(29), reviser: 'DEV_LEAD', reviseDate: dateString(18), dataStamp: 1 },
  { reasonNo: 'EQP-003', reasonName: '待工程人員確認', reasonType: 6, reasonSubType: 'BREAKDOWN', reasonSubTypeName: '故障原因', reasonLevel: 3, description: '待工程人員檢查並回報。', issueState: 1, creator: 'DEV_USER', createDate: dateString(20), reviser: 'DEV_USER', reviseDate: dateString(18), dataStamp: 1 },
  { reasonNo: 'EQP-010', reasonName: '換線調機', reasonType: 6, reasonSubType: 'PRODUCTION', reasonSubTypeName: '生產狀態說明', reasonLevel: 2, description: '產品切換時進行機台調整。', issueState: 2, creator: 'DEV_USER', createDate: dateString(120), reviser: 'DEV_LEAD', reviseDate: dateString(96), dataStamp: 3 },
  { reasonNo: 'EQP-011', reasonName: '設備待料', reasonType: 6, reasonSubType: 'PRODUCTION', reasonSubTypeName: '生產狀態說明', reasonLevel: 1, description: '等待生產物料或載具。', issueState: 2, creator: 'DEV_USER', createDate: dateString(210), reviser: 'DEV_LEAD', reviseDate: dateString(180), dataStamp: 2 },
  { reasonNo: 'EQP-020', reasonName: '舊版故障分類', reasonType: 6, reasonSubType: 'BREAKDOWN', reasonSubTypeName: '故障原因', reasonLevel: 5, description: '已由新版原因分類取代。', issueState: -1, creator: 'DEV_USER', createDate: dateString(420), reviser: 'DEV_LEAD', reviseDate: dateString(350), dataStamp: 4 },
])

const activeState = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const selectedReasonNos = ref<string[]>([])
const activeReasonNo = ref('EQP-001')
const dialogMode = ref<DialogMode | null>(null)
const form = ref<ReasonForm>(blankForm())
const originalReasonNo = ref('')
const originalDataStamp = ref(0)
const formError = ref('')
const showSubtypePicker = ref(false)
const showDeleteConfirm = ref(false)
const showApproveDialog = ref(false)
const approvalRoute = ref<'direct' | 'workflow'>('direct')
const actionMessage = ref('')

function blankForm(): ReasonForm {
  return { reasonNo: '', reasonName: '', reasonSubType: '', reasonSubTypeName: '', reasonLevel: 0, description: '' }
}

const visibleRows = computed(() => reasonRows.value
  .filter((row) => row.issueState === activeState.value && (!appliedSearch.value || row.reasonNo.toUpperCase().includes(appliedSearch.value.toUpperCase())))
  .sort((a, b) => a.reasonNo.localeCompare(b.reasonNo)))
const activeRow = computed(() => visibleRows.value.find((row) => row.reasonNo === activeReasonNo.value) ?? null)
const selectedRows = computed(() => reasonRows.value.filter((row) => selectedReasonNos.value.includes(row.reasonNo)))
const dialogTitle = computed(() => dialogMode.value === 'add' ? '新增設備原因' : dialogMode.value === 'edit' ? '編輯設備原因' : '複製設備原因')

function stateCount(state: IssueState) {
  return reasonRows.value.filter((row) => row.issueState === state).length
}

function setTab(state: IssueState) {
  activeState.value = state
  selectedReasonNos.value = []
  actionMessage.value = ''
}

function toggleSelection(reasonNo: string) {
  selectedReasonNos.value = selectedReasonNos.value.includes(reasonNo)
    ? selectedReasonNos.value.filter((item) => item !== reasonNo)
    : [...selectedReasonNos.value, reasonNo]
  activeReasonNo.value = reasonNo
}

function search() {
  appliedSearch.value = searchInput.value.trim()
  selectedReasonNos.value = []
}

function selectVisibleRows(event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  selectedReasonNos.value = checked ? visibleRows.value.map((row) => row.reasonNo) : []
}

function openForm(mode: DialogMode, row: ReasonRow | null = null) {
  if (mode === 'edit' && (!row || row.issueState !== 0)) return
  dialogMode.value = mode
  formError.value = ''
  originalReasonNo.value = row?.reasonNo ?? ''
  originalDataStamp.value = row?.dataStamp ?? 0
  form.value = row
    ? { reasonNo: mode === 'copy' ? '' : row.reasonNo, reasonName: row.reasonName, reasonSubType: row.reasonSubType, reasonSubTypeName: row.reasonSubTypeName, reasonLevel: row.reasonLevel, description: row.description }
    : blankForm()
}

function saveForm(closeAfter: boolean) {
  formError.value = ''
  const reasonNo = form.value.reasonNo.trim().toUpperCase()
  const reasonName = form.value.reasonName.trim()
  if (!reasonNo) { formError.value = 'ReasonNo 不可空白。'; return }
  if (!reasonName) { formError.value = 'ReasonName 不可空白。'; return }
  if (reasonNo.length > 20 || reasonName.length > 100 || form.value.description.length > 255) { formError.value = '欄位長度超過來源資料表限制。'; return }
  if (!Number.isInteger(form.value.reasonLevel) || form.value.reasonLevel < 0 || form.value.reasonLevel > 9) { formError.value = 'ReasonLevel 必須是 0 到 9 的整數。'; return }

  const duplicate = reasonRows.value.some((row) => row.reasonNo.toUpperCase() === reasonNo && (dialogMode.value !== 'edit' || row.reasonNo !== originalReasonNo.value))
  if (duplicate) { formError.value = 'ReasonNo 已存在。'; return }

  if (dialogMode.value === 'edit') {
    const row = reasonRows.value.find((item) => item.reasonNo === originalReasonNo.value)
    if (!row || row.issueState !== 0) { formError.value = '僅未凍結資料可編輯。'; return }
    if (row.dataStamp !== originalDataStamp.value) { formError.value = '資料已被其他人更新，請重新載入後再編輯。'; return }
    Object.assign(row, { reasonName, reasonSubType: form.value.reasonSubType, reasonSubTypeName: form.value.reasonSubTypeName, reasonLevel: form.value.reasonLevel, description: form.value.description, reviser: 'DEV_USER', reviseDate: new Date().toISOString(), dataStamp: row.dataStamp + 1 })
    originalDataStamp.value = row.dataStamp
  } else {
    reasonRows.value.unshift({ reasonNo, reasonName, reasonType: 6, reasonSubType: form.value.reasonSubType, reasonSubTypeName: form.value.reasonSubTypeName, reasonLevel: form.value.reasonLevel, description: form.value.description, issueState: 0, creator: 'DEV_USER', createDate: new Date().toISOString(), reviser: '', reviseDate: '', dataStamp: 0 })
    activeState.value = 0
    activeReasonNo.value = reasonNo
    selectedReasonNos.value = []
  }

  form.value.reasonNo = reasonNo
  actionMessage.value = `${dialogMode.value === 'edit' ? '已模擬更新' : dialogMode.value === 'copy' ? '已模擬複製新增' : '已模擬新增'} ${reasonNo}。DEV 資料只保存在目前畫面。`
  if (closeAfter) dialogMode.value = null
}

function closeForm() {
  dialogMode.value = null
  formError.value = ''
}

function dateText(value: string) {
  if (!value) return '—'
  return new Date(value).toLocaleString('zh-TW', { hour12: false })
}

function confirmDelete() {
  actionMessage.value = ''
  let changed = 0
  for (const target of selectedRows.value) {
    const row = reasonRows.value.find((item) => item.reasonNo === target.reasonNo)
    if (!row) continue
    if (row.issueState === 0 || row.issueState === -1) {
      reasonRows.value = reasonRows.value.filter((item) => item.reasonNo !== row.reasonNo)
      changed += 1
    } else if (row.issueState === 2) {
      row.issueState = -1
      row.reviser = 'DEV_USER'
      row.reviseDate = new Date().toISOString()
      row.dataStamp += 1
      changed += 1
    } else {
      actionMessage.value = `${row.reasonNo} 為簽核中資料，來源不允許刪除；先前已成功的列不會自動回復。`
      break
    }
  }
  if (!actionMessage.value) actionMessage.value = `已模擬處理 ${changed} 筆：未凍結／停用資料實體刪除，已核准資料轉為停用。`
  selectedReasonNos.value = []
  showDeleteConfirm.value = false
}

function confirmApprove() {
  actionMessage.value = ''
  let changed = 0
  for (const target of selectedRows.value) {
    const row = reasonRows.value.find((item) => item.reasonNo === target.reasonNo)
    if (!row) continue
    if (row.issueState !== 0) {
      actionMessage.value = `${row.reasonNo} 不是未凍結狀態，無法核准；先前已成功的列不會自動回復。`
      break
    }
    row.issueState = approvalRoute.value === 'workflow' ? 1 : 2
    row.reviser = 'DEV_USER'
    row.reviseDate = new Date().toISOString()
    row.dataStamp += 1
    changed += 1
  }
  if (!actionMessage.value) actionMessage.value = approvalRoute.value === 'workflow'
    ? `已模擬送簽 ${changed} 筆，狀態轉為簽核中。`
    : `已模擬直接核准 ${changed} 筆，狀態轉為已核准。`
  selectedReasonNos.value = []
  showApproveDialog.value = false
}

function selectSubtype(option: { code: string; name: string }) {
  form.value.reasonSubType = option.code
  form.value.reasonSubTypeName = option.name
  showSubtypePicker.value = false
}
</script>

<template>
  <section v-if="developmentPreview" class="reason-basis" aria-label="設備狀態變更原因資料 DEV 預覽">
    <header class="page-heading">
      <div><p class="eyebrow">EMS / EMS CONFIG · ES106</p><h1>設備狀態變更原因資料</h1><p>只維護 ReasonType = 6（EQP）的原因主檔。</p></div>
      <span class="dev-badge">DEV MOCK</span>
    </header>

    <section class="workspace-panel">
      <nav class="status-tabs" aria-label="簽核狀態">
        <button v-for="tab in issueTabs" :key="tab.state" type="button" :class="{ active: activeState === tab.state }" @click="setTab(tab.state)">{{ tab.label }} <span>{{ stateCount(tab.state) }}</span></button>
      </nav>
      <div class="toolbar">
        <div class="search-box"><label for="reason-search">ReasonNo</label><input id="reason-search" v-model="searchInput" type="search" maxlength="20" placeholder="輸入原因代碼" @keyup.enter="search"><button type="button" class="secondary-button" @click="search">查詢</button></div>
        <div class="action-buttons">
          <button type="button" class="primary-button" @click="openForm('add')">新增</button>
          <button type="button" class="secondary-button" :disabled="!activeRow || activeRow.issueState !== 0" @click="openForm('edit', activeRow)">編輯</button>
          <button type="button" class="secondary-button" :disabled="!activeRow" @click="openForm('copy', activeRow)">複製</button>
          <button type="button" class="secondary-button danger-button" :disabled="selectedReasonNos.length === 0" @click="showDeleteConfirm = true">刪除</button>
          <button type="button" class="secondary-button" :disabled="selectedReasonNos.length === 0" @click="showApproveDialog = true">核准</button>
        </div>
      </div>

      <div class="table-wrap">
        <table>
          <thead><tr><th class="check-cell"><input type="checkbox" aria-label="選取目前列表" :checked="visibleRows.length > 0 && visibleRows.every((row) => selectedReasonNos.includes(row.reasonNo))" @change="selectVisibleRows"></th><th>ReasonNo</th><th>ReasonName</th><th>ReasonType</th><th>ReasonSubType</th><th>ReasonLevel</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.reasonNo" :class="{ selected: activeReasonNo === row.reasonNo }" @click="activeReasonNo = row.reasonNo">
              <td class="check-cell"><input type="checkbox" :checked="selectedReasonNos.includes(row.reasonNo)" :aria-label="`選取 ${row.reasonNo}`" @click.stop @change="toggleSelection(row.reasonNo)"></td>
              <td class="code-cell">{{ row.reasonNo }}</td><td>{{ row.reasonName }}</td><td><span class="type-chip">EQP</span></td><td>{{ row.reasonSubType }}</td><td class="number-cell">{{ row.reasonLevel }}</td><td class="description-cell">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '—' }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="visibleRows.length === 0"><td colspan="11" class="empty-state">目前狀態沒有符合 ReasonNo 的設備原因。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>{{ visibleRows.length }} 筆</span><span>DataStamp 用於 DEV 樂觀並發示意；ReasonType 固定 6 / EQP。</span></footer>
    </section>

    <div v-if="actionMessage" class="action-message" role="status"><span>{{ actionMessage }}</span><button type="button" aria-label="關閉訊息" @click="actionMessage = ''">×</button></div>

    <div v-if="dialogMode" class="dialog-backdrop" @click.self="closeForm">
      <section class="reason-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header><div><p class="eyebrow">{{ dialogMode === 'edit' ? 'EDIT' : dialogMode === 'copy' ? 'COPY' : 'ADD' }}</p><h2>{{ dialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeForm">×</button></header>
        <div class="form-grid">
          <label>ReasonNo<input v-model="form.reasonNo" type="text" maxlength="20" :readonly="dialogMode === 'edit'" :class="{ readonly: dialogMode === 'edit' }" @blur="form.reasonNo = form.reasonNo.toUpperCase()"></label>
          <label>ReasonName<input v-model="form.reasonName" type="text" maxlength="100" autofocus></label>
          <label>ReasonType<div class="fixed-type"><input type="radio" checked disabled> EQP (6)</div></label>
          <label>ReasonSubType<div class="lookup-field"><input :value="form.reasonSubType ? `${form.reasonSubType} · ${form.reasonSubTypeName}` : ''" type="text" readonly placeholder="請選擇設備原因子類別"><button type="button" class="secondary-button" @click="showSubtypePicker = true">選擇…</button></div></label>
          <label class="level-field">ReasonLevel<input v-model.number="form.reasonLevel" type="number" min="0" max="9" step="1"></label>
          <label class="description-field">Description<textarea v-model="form.description" maxlength="255" rows="4"></textarea><small>{{ form.description.length }}/255</small></label>
        </div>
        <p v-if="formError" class="error-message" role="alert">{{ formError }}</p>
        <footer class="dialog-footer"><span>Apply 儲存並留在視窗；Confirm 儲存並關閉。</span><button type="button" class="secondary-button" @click="closeForm">Close</button><button type="button" class="secondary-button" @click="saveForm(false)">Apply</button><button type="button" class="primary-button" @click="saveForm(true)">Confirm</button></footer>
      </section>
    </div>

    <div v-if="showSubtypePicker" class="dialog-backdrop nested-backdrop" @click.self="showSubtypePicker = false">
      <section class="small-dialog" role="dialog" aria-modal="true" aria-label="選擇設備原因子類別">
        <header><h2>選擇設備原因子類別</h2><button type="button" class="icon-button" aria-label="關閉" @click="showSubtypePicker = false">×</button></header>
        <button type="button" class="subtype-option" @click="form.reasonSubType = ''; form.reasonSubTypeName = ''; showSubtypePicker = false"><strong>（空白）</strong><span>不指定子類別</span></button>
        <button v-for="option in subtypeOptions" :key="option.code" type="button" class="subtype-option" @click="selectSubtype(option)"><strong>{{ option.code }}</strong><span>{{ option.name }}</span></button>
      </section>
    </div>

    <div v-if="showDeleteConfirm" class="dialog-backdrop" @click.self="showDeleteConfirm = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除設備原因"><h2>確認刪除</h2><p>將依狀態實體刪除未凍結／停用資料，或把已核准資料改為停用。簽核中資料會依來源規則拒絕。</p><p class="target-list">{{ selectedReasonNos.join('、') }}</p><footer><button type="button" class="secondary-button" @click="showDeleteConfirm = false">取消</button><button type="button" class="danger-primary" @click="confirmDelete">確認處理</button></footer></section>
    </div>

    <div v-if="showApproveDialog" class="dialog-backdrop" @click.self="showApproveDialog = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="核准設備原因"><h2>核准設備原因</h2><p>來源依 Issue Job Map 決定直接核准或建立簽核流程。此選擇只用於 DEV 模擬。</p><label class="route-choice"><input v-model="approvalRoute" type="radio" value="direct"> 無簽核流程設定：直接轉為已核准</label><label class="route-choice"><input v-model="approvalRoute" type="radio" value="workflow"> 有簽核流程設定：送簽並轉為簽核中</label><p class="target-list">{{ selectedReasonNos.join('、') }}</p><footer><button type="button" class="secondary-button" @click="showApproveDialog = false">取消</button><button type="button" class="primary-button" @click="confirmApprove">確認核准</button></footer></section>
    </div>
  </section>
</template>

<style scoped>
.reason-basis { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 12px; padding: 14px; overflow: auto; box-sizing: border-box; color: #29281f; background: #f4f2ea; font: 13px/1.4 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-heading,.toolbar,.search-box,.action-buttons,.status-tabs,.grid-footer,.reason-dialog header,.dialog-footer,.lookup-field,.confirm-dialog footer,.small-dialog header { display: flex; align-items: center; }
.page-heading { justify-content: space-between; gap: 16px; }.page-heading h1 { margin: 0; font-size: 20px; font-weight: 650; }.page-heading p { margin: 3px 0 0; color: #6b685c; }.page-heading .eyebrow,.reason-dialog .eyebrow { margin: 0 0 4px; color: #716b54; font-size: 11px; letter-spacing: .1em; }
.dev-badge { padding: 4px 8px; border: 1px solid #b2a477; border-radius: 3px; color: #6b5722; background: #f5edcf; font-size: 10px; font-weight: 700; white-space: nowrap; }
.workspace-panel { min-height: 230px; display: flex; flex-direction: column; border: 1px solid #c9c5b8; background: #fbfaf6; box-shadow: 0 1px 2px #2724140c; }
.status-tabs { gap: 2px; padding: 7px 9px 0; border-bottom: 1px solid #d4d0c4; background: #f5f3ec; }.status-tabs button { position: relative; display: flex; gap: 7px; align-items: center; padding: 8px 12px; border: 1px solid transparent; border-bottom: 0; border-radius: 3px 3px 0 0; color: #605d53; background: transparent; font: inherit; cursor: pointer; }.status-tabs button.active { border-color: #c9c5b8; color: #29281f; background: #fbfaf6; font-weight: 650; }.status-tabs span { min-width: 18px; padding: 1px 5px; border-radius: 9px; background: #e8e5da; text-align: center; font-size: 10px; }
.toolbar { justify-content: space-between; flex-wrap: wrap; gap: 9px; padding: 9px 10px; border-bottom: 1px solid #dedbd1; }.search-box,.action-buttons { gap: 6px; }.search-box label { font-weight: 600; }.search-box input { width: 180px; }.primary-button,.secondary-button,.icon-button,.danger-primary { min-height: 30px; padding: 4px 10px; border: 1px solid #aaa596; border-radius: 2px; color: #343229; background: #f7f6f0; font: inherit; cursor: pointer; white-space: nowrap; }.primary-button { border-color: #48543b; color: white; background: #59684a; }.primary-button:hover { background: #4c5b3d; }.secondary-button:hover,.icon-button:hover { background: #ebe9df; }.danger-button { color: #983d31; }.danger-primary { border-color: #8a392e; color: #fff; background: #994637; }button:disabled { cursor: not-allowed; opacity: .45; }button:focus-visible,input:focus-visible,textarea:focus-visible { outline: 2px solid #a48b27; outline-offset: 1px; }
.table-wrap { overflow: auto; }.table-wrap table { width: 100%; min-width: 1180px; border-collapse: collapse; font-size: 12px; }.table-wrap th,.table-wrap td { padding: 7px 8px; border-bottom: 1px solid #e2dfd5; text-align: left; white-space: nowrap; }.table-wrap th { position: sticky; top: 0; z-index: 1; color: #555246; background: #eeece3; font-size: 11px; font-weight: 650; }.table-wrap tbody tr { cursor: pointer; }.table-wrap tbody tr:hover { background: #f5f3e9; }.table-wrap tbody tr.selected { background: #ecebdc; }.check-cell { width: 30px; text-align: center !important; }.code-cell { font-family: Consolas, monospace; font-weight: 650; }.type-chip { padding: 2px 6px; border-radius: 9px; color: #44573c; background: #e3eadf; }.number-cell { text-align: right !important; font-variant-numeric: tabular-nums; }.description-cell { max-width: 250px; overflow: hidden; text-overflow: ellipsis; }.empty-state { height: 90px; color: #777466; text-align: center !important; }.grid-footer { justify-content: space-between; padding: 7px 10px; border-top: 1px solid #dedbd1; color: #777466; font-size: 11px; }
.action-message { display: flex; justify-content: space-between; gap: 12px; padding: 9px 11px; border: 1px solid #c5bc94; border-radius: 2px; color: #5d512e; background: #f7f0d5; }.action-message button { border: 0; color: inherit; background: transparent; font: inherit; font-size: 18px; cursor: pointer; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 18px; background: #17170fb0; }.reason-dialog,.confirm-dialog,.small-dialog { width: min(650px, 100%); max-height: 92vh; overflow: auto; border: 1px solid #aaa596; border-radius: 4px; background: #fbfaf6; box-shadow: 0 16px 48px #0004; }.reason-dialog header,.small-dialog header { justify-content: space-between; gap: 12px; padding: 12px 14px; border-bottom: 1px solid #d4d0c4; }.reason-dialog header h2,.small-dialog header h2 { margin: 0; font-size: 16px; }.reason-dialog header .eyebrow { margin: 0; }.icon-button { min-width: 30px; padding: 0; font-size: 20px; line-height: 1; }.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 11px 13px; padding: 14px; }.form-grid label { display: grid; align-content: start; gap: 5px; color: #4b493f; font-weight: 600; }.form-grid input[type="text"],.form-grid input[type="number"],.form-grid textarea { width: 100%; min-height: 32px; box-sizing: border-box; padding: 6px 8px; border: 1px solid #b7b3a8; border-radius: 2px; color: #302f29; background: #fffefa; font: inherit; font-weight: 400; }.form-grid input.readonly { color: #777466; background: #f0efe9; }.form-grid textarea { resize: vertical; }.fixed-type { min-height: 31px; display: flex; align-items: center; gap: 6px; color: #58634b; font-weight: 500; }.lookup-field { gap: 5px; }.lookup-field input { flex: 1; min-width: 0; }.level-field { max-width: 150px; }.description-field { grid-column: 1 / -1; }.description-field small { color: #777466; text-align: right; font-weight: 400; }.error-message { margin: 0 14px 10px; color: #9c2f21; }.dialog-footer { justify-content: flex-end; flex-wrap: wrap; gap: 7px; padding: 10px 14px; border-top: 1px solid #d4d0c4; }.dialog-footer span { flex: 1; min-width: 200px; color: #777466; font-size: 11px; }
.small-dialog { width: min(420px, 100%); padding-bottom: 8px; }.small-dialog header { margin-bottom: 5px; }.subtype-option { width: calc(100% - 16px); display: flex; justify-content: space-between; margin: 3px 8px; padding: 9px 10px; border: 0; border-radius: 2px; color: #302f29; background: transparent; font: inherit; text-align: left; cursor: pointer; }.subtype-option:hover { background: #eeece3; }.subtype-option strong { font-family: Consolas, monospace; }.confirm-dialog { width: min(480px, 100%); padding: 16px; box-sizing: border-box; }.confirm-dialog h2 { margin: 0 0 8px; font-size: 16px; }.confirm-dialog p { color: #686558; }.confirm-dialog footer { justify-content: flex-end; gap: 7px; margin-top: 14px; }.target-list { padding: 8px; border: 1px solid #dedbd1; background: #f4f2ea; font-family: Consolas, monospace; }.route-choice { display: flex; align-items: flex-start; gap: 8px; margin: 9px 0; }
.nested-backdrop { z-index: 1210; }
@media (max-width: 700px) { .reason-basis { padding: 9px; }.toolbar { align-items: flex-start; flex-direction: column; }.search-box { width: 100%; }.search-box input { flex: 1; min-width: 0; }.action-buttons { flex-wrap: wrap; }.status-tabs { overflow-x: auto; }.form-grid { grid-template-columns: 1fr; }.description-field { grid-column: auto; }.dialog-footer span { flex-basis: 100%; } }
</style>
