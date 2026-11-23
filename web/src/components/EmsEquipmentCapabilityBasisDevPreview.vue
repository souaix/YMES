<script setup lang="ts">
import { computed, ref } from 'vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

type IssueState = 0 | 1 | 2 | -1
type DialogMode = 'add' | 'edit' | 'copy'
interface CapItemRow {
  capItem: string
  description: string
  issueState: IssueState
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
}

const issueTabs: { state: IssueState; label: string }[] = [
  { state: 0, label: '未凍結' },
  { state: 1, label: '簽核中' },
  { state: 2, label: '已核准' },
  { state: -1, label: '已停用' },
]
const timeAgo = (hours: number) => new Date(Date.now() - hours * 3_600_000).toISOString()
const capRows = ref<CapItemRow[]>([
  { capItem: 'CHAMBER_TEMP', description: '腔體溫度能力項目', issueState: 0, creator: 'DEV_USER', createDate: timeAgo(42), reviser: 'DEV_LEAD', reviseDate: timeAgo(16), dataStamp: 1 },
  { capItem: 'RF_POWER', description: '射頻輸出功率設定', issueState: 0, creator: 'DEV_USER', createDate: timeAgo(38), reviser: '', reviseDate: '', dataStamp: 0 },
  { capItem: 'VACUUM_LEVEL', description: '製程真空能力範圍', issueState: 1, creator: 'DEV_USER', createDate: timeAgo(31), reviser: 'DEV_LEAD', reviseDate: timeAgo(20), dataStamp: 1 },
  { capItem: 'WAFER_CAPACITY', description: '腔體可處理晶圓數', issueState: 2, creator: 'DEV_USER', createDate: timeAgo(360), reviser: 'DEV_LEAD', reviseDate: timeAgo(300), dataStamp: 2 },
  { capItem: 'RF_POWER_OLD', description: '已由新功率規格取代', issueState: -1, creator: 'DEV_USER', createDate: timeAgo(720), reviser: 'DEV_LEAD', reviseDate: timeAgo(600), dataStamp: 3 },
])

const activeState = ref<IssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const selectedCapItems = ref<string[]>([])
const activeCapItem = ref('CHAMBER_TEMP')
const dialogMode = ref<DialogMode | null>(null)
const form = ref({ capItem: '', description: '' })
const originalCapItem = ref('')
const originalDataStamp = ref(0)
const formError = ref('')
const formNotice = ref('')
const actionMessage = ref('')
const showDeleteConfirm = ref(false)
const showApproveDialog = ref(false)
const approvalRoute = ref<'direct' | 'workflow'>('direct')

const visibleRows = computed(() => capRows.value
  .filter((row) => row.issueState === activeState.value && (!appliedSearch.value || row.capItem.toUpperCase().includes(appliedSearch.value.toUpperCase())))
  .sort((a, b) => a.capItem.localeCompare(b.capItem)))
const activeRow = computed(() => visibleRows.value.find((row) => row.capItem === activeCapItem.value) ?? null)
const selectedRows = computed(() => capRows.value.filter((row) => selectedCapItems.value.includes(row.capItem)))
const dialogTitle = computed(() => dialogMode.value === 'add' ? '新增設備能力項目' : dialogMode.value === 'edit' ? '編輯設備能力項目' : '複製設備能力項目')
const canEdit = computed(() => activeRow.value?.issueState === 0)
const canApprove = computed(() => selectedRows.value.length > 0 && selectedRows.value.every((row) => row.issueState === 0))

function stateCount(state: IssueState) {
  return capRows.value.filter((row) => row.issueState === state).length
}

function setTab(state: IssueState) {
  activeState.value = state
  selectedCapItems.value = []
  actionMessage.value = ''
  activeCapItem.value = visibleRows.value[0]?.capItem ?? ''
}

function search() {
  appliedSearch.value = searchInput.value.trim()
  selectedCapItems.value = []
  activeCapItem.value = ''
}

function toggleSelection(capItem: string) {
  selectedCapItems.value = selectedCapItems.value.includes(capItem)
    ? selectedCapItems.value.filter((item) => item !== capItem)
    : [...selectedCapItems.value, capItem]
  activeCapItem.value = capItem
}

function selectVisibleRows(event: Event) {
  selectedCapItems.value = (event.target as HTMLInputElement).checked ? visibleRows.value.map((row) => row.capItem) : []
}

function openForm(mode: DialogMode, row: CapItemRow | null = null) {
  if (mode === 'edit' && (!row || row.issueState !== 0)) return
  dialogMode.value = mode
  formError.value = ''
  formNotice.value = ''
  originalCapItem.value = row?.capItem ?? ''
  originalDataStamp.value = row?.dataStamp ?? 0
  form.value = row
    ? { capItem: mode === 'copy' ? '' : row.capItem, description: row.description }
    : { capItem: '', description: '' }
}

function saveForm(closeAfter: boolean) {
  formError.value = ''
  const capItem = form.value.capItem.trim().toUpperCase()
  const description = form.value.description
  if (!capItem) { formError.value = 'CapItem 不可空白。'; return }
  if (capItem.length > 50 || description.length > 255) { formError.value = '欄位長度超過來源資料表限制。'; return }
  if (capRows.value.some((row) => row.capItem.toUpperCase() === capItem && (dialogMode.value !== 'edit' || row.capItem !== originalCapItem.value))) {
    formError.value = 'CapItem 已存在。'
    return
  }

  if (dialogMode.value === 'edit') {
    const row = capRows.value.find((item) => item.capItem === originalCapItem.value)
    if (!row || row.issueState !== 0) { formError.value = '僅未凍結資料可編輯。'; return }
    if (row.dataStamp !== originalDataStamp.value) { formError.value = '資料已被其他人更新，請重新載入後再編輯。'; return }
    Object.assign(row, { description, reviser: 'DEV_USER', reviseDate: new Date().toISOString(), dataStamp: row.dataStamp + 1 })
    originalDataStamp.value = row.dataStamp
  } else {
    capRows.value.unshift({ capItem, description, issueState: 0, creator: 'DEV_USER', createDate: new Date().toISOString(), reviser: '', reviseDate: '', dataStamp: 0 })
    activeState.value = 0
    activeCapItem.value = capItem
    selectedCapItems.value = []
  }

  form.value.capItem = capItem
  actionMessage.value = `${dialogMode.value === 'edit' ? '已模擬更新' : dialogMode.value === 'copy' ? '已模擬複製新增' : '已模擬新增'} ${capItem}。DEV 資料只保存在目前畫面。`
  formNotice.value = actionMessage.value
  if (closeAfter) dialogMode.value = null
}

function confirmDelete() {
  let changed = 0
  actionMessage.value = ''
  for (const target of selectedRows.value) {
    const row = capRows.value.find((item) => item.capItem === target.capItem)
    if (!row) continue
    if (row.issueState === 0 || row.issueState === -1) {
      capRows.value = capRows.value.filter((item) => item.capItem !== row.capItem)
      changed += 1
    } else if (row.issueState === 2) {
      row.issueState = -1
      row.reviser = 'DEV_USER'
      row.reviseDate = new Date().toISOString()
      row.dataStamp += 1
      changed += 1
    } else {
      actionMessage.value = `${row.capItem} 為簽核中資料，來源不允許刪除；前面已完成的列不會自動回復。`
      break
    }
  }
  if (!actionMessage.value) actionMessage.value = `已模擬處理 ${changed} 筆：未凍結／停用資料實體刪除，已核准資料轉為停用。`
  selectedCapItems.value = []
  activeCapItem.value = ''
  showDeleteConfirm.value = false
}

function confirmApprove() {
  let changed = 0
  actionMessage.value = ''
  for (const target of selectedRows.value) {
    const row = capRows.value.find((item) => item.capItem === target.capItem)
    if (!row) continue
    if (row.issueState !== 0) {
      actionMessage.value = `${row.capItem} 不是未凍結狀態，無法核准；前面已完成的列不會自動回復。`
      break
    }
    row.issueState = approvalRoute.value === 'workflow' ? 1 : 2
    row.reviser = 'DEV_USER'
    row.reviseDate = new Date().toISOString()
    row.dataStamp += 1
    changed += 1
  }
  if (!actionMessage.value) actionMessage.value = approvalRoute.value === 'workflow'
    ? `已模擬送簽 ${changed} 筆，狀態轉為簽核中。正式簽核方式由伺服器設定決定。`
    : `已模擬直接核准 ${changed} 筆。正式簽核方式由伺服器設定決定。`
  selectedCapItems.value = []
  showApproveDialog.value = false
}

function dateText(value: string) {
  return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '—'
}
</script>

<template>
  <section class="cap-basis" aria-label="設備能力項目設定 DEV 預覽" :data-development-preview="developmentPreview ? 'true' : 'false'">
    <header class="page-heading">
      <div><p class="eyebrow">EMS / EMS CONFIG · ES107</p><h1>設備能力項目設定</h1><p>維護設備能力項目代碼及說明，供產品設備能力設定查找。</p></div>
      <span class="dev-badge">DEV MOCK</span>
    </header>

    <section class="workspace-panel">
      <nav class="status-tabs" aria-label="簽核狀態">
        <button v-for="tab in issueTabs" :key="tab.state" type="button" :class="{ active: activeState === tab.state }" @click="setTab(tab.state)">{{ tab.label }} <span>{{ stateCount(tab.state) }}</span></button>
      </nav>
      <div class="toolbar">
        <form class="search-box" @submit.prevent="search">
          <label for="cap-item-search">CapItem</label>
          <input id="cap-item-search" v-model="searchInput" maxlength="50" autocomplete="off" placeholder="輸入能力項目代碼" />
          <button class="primary-button" type="submit">查詢</button>
          <button class="secondary-button" type="button" @click="searchInput = ''; search()">清除</button>
        </form>
        <div class="action-buttons">
          <button type="button" class="primary-button" @click="openForm('add')">新增</button>
          <button type="button" :disabled="!canEdit" @click="openForm('edit', activeRow)">修改</button>
          <button type="button" :disabled="!activeRow" @click="openForm('copy', activeRow)">複製</button>
          <button type="button" class="danger-button" :disabled="selectedRows.length === 0" @click="showDeleteConfirm = true">刪除</button>
          <button type="button" :disabled="!canApprove" @click="showApproveDialog = true">核准</button>
          <button type="button" @click="emit('close')">關閉</button>
        </div>
      </div>

      <div v-if="actionMessage" class="action-message" role="status"><span>{{ actionMessage }}</span><button type="button" aria-label="關閉訊息" @click="actionMessage = ''">×</button></div>

      <div class="table-wrap">
        <table>
          <thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="全選目前資料" :checked="visibleRows.length > 0 && selectedCapItems.length === visibleRows.length" @change="selectVisibleRows" /></th>
            <th>CapItem</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.capItem" :class="{ selected: activeCapItem === row.capItem }" @click="activeCapItem = row.capItem">
              <td class="check-cell"><input type="checkbox" :aria-label="`選取 ${row.capItem}`" :checked="selectedCapItems.includes(row.capItem)" @click.stop @change="toggleSelection(row.capItem)" /></td>
              <td class="code-cell">{{ row.capItem }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '—' }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="visibleRows.length === 0"><td colspan="7" class="empty-state">查無設備能力項目。</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>共 {{ visibleRows.length }} 筆</span><span>狀態與 DataStamp 為 DEV 假資料；不連正式 API。</span></footer>
    </section>

    <div v-if="dialogMode" class="dialog-backdrop" @click.self="dialogMode = null">
      <section class="edit-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header><div><p class="eyebrow">設備能力主檔</p><h2>{{ dialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="dialogMode = null">×</button></header>
        <div class="form-grid">
          <label>CapItem <span class="required">*</span><input v-model="form.capItem" :maxlength="50" :readonly="dialogMode === 'edit'" :class="{ readonly: dialogMode === 'edit' }" autocomplete="off" @blur="form.capItem = form.capItem.trim().toUpperCase()" /></label>
          <label class="description-field">Description <textarea v-model="form.description" :maxlength="255" rows="4" /><small>{{ form.description.length }} / 255</small></label>
        </div>
        <p v-if="formError" class="error-message" role="alert">{{ formError }}</p>
        <p v-if="formNotice && !formError" role="status" style="margin: 0 14px 10px; color: #526746; font-size: 12px">{{ formNotice }}</p>
        <footer class="dialog-footer"><span>新增及複製建立未凍結資料；修改僅限未凍結資料。</span><button type="button" @click="dialogMode = null">取消</button><button type="button" @click="saveForm(false)">套用</button><button type="button" class="primary-button" @click="saveForm(true)">確定</button></footer>
      </section>
    </div>

    <div v-if="showDeleteConfirm" class="dialog-backdrop" @click.self="showDeleteConfirm = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除設備能力項目">
        <h2>確認刪除 {{ selectedRows.length }} 筆資料？</h2>
        <p>未凍結／停用資料會模擬實體刪除；已核准資料會模擬轉為停用。簽核中資料不允許刪除。</p>
        <div class="target-list">{{ selectedRows.map((row) => row.capItem).join('、') }}</div>
        <footer><button type="button" @click="showDeleteConfirm = false">取消</button><button type="button" class="danger-primary" @click="confirmDelete">確認刪除</button></footer>
      </section>
    </div>

    <div v-if="showApproveDialog" class="dialog-backdrop" @click.self="showApproveDialog = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="模擬核准設備能力項目">
        <h2>模擬核准 {{ selectedRows.length }} 筆資料</h2>
        <p>舊 Service 依 EMS_EQP_CAPBASIS 簽核設定決定直接核准或進入簽核流程；此選項只控制 DEV 假資料。</p>
        <label class="route-choice"><input v-model="approvalRoute" type="radio" value="direct" />直接核准，狀態轉為已核准</label>
        <label class="route-choice"><input v-model="approvalRoute" type="radio" value="workflow" />送簽，狀態轉為簽核中</label>
        <div class="target-list">{{ selectedRows.map((row) => row.capItem).join('、') }}</div>
        <footer><button type="button" @click="showApproveDialog = false">取消</button><button type="button" class="primary-button" @click="confirmApprove">執行模擬</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.cap-basis { min-height: 100%; box-sizing: border-box; padding: 14px; color: #302f29; background: #f5f3eb; font: inherit; }
.page-heading,.toolbar,.search-box,.action-buttons,.status-tabs,.grid-footer,.action-message,.edit-dialog header,.dialog-footer,.confirm-dialog footer { display: flex; align-items: center; }
.page-heading { justify-content: space-between; gap: 14px; margin-bottom: 12px; }.page-heading h1 { margin: 2px 0 4px; font-size: 20px; }.page-heading p { margin: 0; color: #777466; font-size: 12px; }.eyebrow { color: #79745d !important; font-size: 10px !important; letter-spacing: .08em; }.dev-badge { padding: 4px 8px; border: 1px solid #b7aa65; border-radius: 2px; color: #605525; background: #f7f0cc; font-size: 10px; font-weight: 700; }
.workspace-panel { display: flex; flex-direction: column; min-height: 350px; border: 1px solid #d0cdc2; background: #fbfaf6; }.status-tabs { gap: 2px; padding: 7px 9px 0; border-bottom: 1px solid #d8d5ca; background: #f0eee6; }.status-tabs button { padding: 8px 11px; border: 1px solid transparent; border-bottom: 0; color: #575449; background: transparent; font: inherit; font-size: 12px; cursor: pointer; }.status-tabs button.active { border-color: #d2cec1; color: #28271f; background: #fbfaf6; font-weight: 700; }.status-tabs span { margin-left: 4px; color: #898575; font-size: 10px; }
.toolbar { justify-content: space-between; flex-wrap: wrap; gap: 9px; padding: 10px; border-bottom: 1px solid #e0ddd3; }.search-box { gap: 6px; }.search-box label { color: #5b584d; font-size: 12px; }.search-box input { width: 220px; min-height: 30px; box-sizing: border-box; padding: 5px 7px; border: 1px solid #bdb9ad; background: #fffefa; font: inherit; }.action-buttons { flex-wrap: wrap; gap: 5px; }.cap-basis button { min-height: 29px; padding: 4px 9px; border: 1px solid #aaa69a; border-radius: 2px; color: #333127; background: #f6f5f0; font: inherit; font-size: 12px; cursor: pointer; }.cap-basis button:hover:not(:disabled) { background: #ebe9df; }.cap-basis button:disabled { cursor: not-allowed; opacity: .45; }.cap-basis .primary-button { border-color: #746525; color: white; background: #746525; }.cap-basis .danger-button { color: #963f34; }.cap-basis .danger-primary { border-color: #8a392e; color: white; background: #994637; }.cap-basis button:focus-visible,.cap-basis input:focus-visible,.cap-basis textarea:focus-visible { outline: 2px solid #a48b27; outline-offset: 1px; }
.action-message { justify-content: space-between; gap: 10px; margin: 8px 10px 0; padding: 8px 10px; border: 1px solid #c5bc94; color: #5d512e; background: #f7f0d5; font-size: 12px; }.action-message button { min-height: auto; padding: 0 4px; border: 0; color: inherit; background: transparent; font-size: 18px; }
.table-wrap { overflow: auto; }.table-wrap table { width: 100%; min-width: 850px; border-collapse: collapse; font-size: 12px; }.table-wrap th,.table-wrap td { padding: 7px 8px; border-bottom: 1px solid #e2dfd5; text-align: left; white-space: nowrap; }.table-wrap th { position: sticky; top: 0; z-index: 1; color: #555246; background: #eeece3; font-size: 11px; font-weight: 650; }.table-wrap tbody tr { cursor: pointer; }.table-wrap tbody tr:hover { background: #f5f3e9; }.table-wrap tbody tr.selected { background: #ecebdc; }.table-wrap input[type="checkbox"] { accent-color: #786b2e; }.check-cell { width: 30px; text-align: center !important; }.code-cell { font-family: Consolas, monospace; font-weight: 650; }.description-cell { max-width: 300px; overflow: hidden; text-overflow: ellipsis; }.empty-state { height: 90px; color: #777466; text-align: center !important; }.grid-footer { justify-content: space-between; gap: 8px; padding: 7px 10px; border-top: 1px solid #dedbd1; color: #777466; font-size: 11px; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 1200; display: grid; place-items: center; padding: 18px; background: #17170fb0; }.edit-dialog,.confirm-dialog { width: min(600px, 100%); max-height: 92vh; overflow: auto; border: 1px solid #aaa596; border-radius: 4px; background: #fbfaf6; box-shadow: 0 16px 48px #0004; }.edit-dialog header { justify-content: space-between; gap: 12px; padding: 12px 14px; border-bottom: 1px solid #d4d0c4; }.edit-dialog header h2 { margin: 0; font-size: 16px; }.edit-dialog header .eyebrow { margin: 0; }.icon-button { min-width: 30px; padding: 0 !important; font-size: 20px !important; }.form-grid { display: grid; grid-template-columns: 1fr; gap: 12px; padding: 14px; }.form-grid label { display: grid; gap: 5px; color: #4b493f; font-weight: 600; }.form-grid input,.form-grid textarea { width: 100%; min-height: 32px; box-sizing: border-box; padding: 6px 8px; border: 1px solid #b7b3a8; border-radius: 2px; color: #302f29; background: #fffefa; font: inherit; font-weight: 400; }.form-grid .readonly { color: #777466; background: #f0efe9; }.form-grid textarea { resize: vertical; }.form-grid small { color: #777466; text-align: right; font-weight: 400; }.required,.error-message { color: #9c2f21; }.error-message { margin: 0 14px 10px; }.dialog-footer { justify-content: flex-end; flex-wrap: wrap; gap: 7px; padding: 10px 14px; border-top: 1px solid #d4d0c4; }.dialog-footer span { flex: 1; min-width: 200px; color: #777466; font-size: 11px; }.confirm-dialog { width: min(470px, 100%); padding: 16px; box-sizing: border-box; }.confirm-dialog h2 { margin: 0 0 8px; font-size: 16px; }.confirm-dialog p { color: #686558; font-size: 12px; }.confirm-dialog footer { justify-content: flex-end; gap: 7px; margin-top: 14px; }.target-list { padding: 8px; border: 1px solid #dedbd1; background: #f4f2ea; font-family: Consolas, monospace; font-size: 12px; overflow-wrap: anywhere; }.route-choice { display: flex; align-items: flex-start; gap: 8px; margin: 9px 0; font-size: 12px; }
@media (max-width: 760px) { .cap-basis { padding: 9px; }.toolbar { align-items: flex-start; flex-direction: column; }.search-box { width: 100%; flex-wrap: wrap; }.search-box input { flex: 1; min-width: 120px; }.action-buttons { flex-wrap: wrap; }.status-tabs { overflow-x: auto; }.grid-footer { align-items: flex-start; flex-direction: column; }.dialog-footer span { flex-basis: 100%; } }
</style>
