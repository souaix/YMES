<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import {
  createEmsEquipmentCapabilityBasisService,
  type CapabilityIssueState,
  type EquipmentCapabilityBasisRow,
} from '../services/emsEquipmentCapabilityBasis'

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
type DialogMode = 'add' | 'edit' | 'copy'

const issueTabs: { state: CapabilityIssueState; label: string }[] = [
  { state: 0, label: '未凍結' },
  { state: 1, label: '簽核中' },
  { state: 2, label: '已核准' },
  { state: -1, label: '已停用' },
]
const activeState = ref<CapabilityIssueState>(0)
const searchInput = ref('')
const appliedSearch = ref('')
const stateCounts = ref<Record<string, number>>({ '0': 0, '1': 0, '2': 0, '-1': 0 })
const rows = ref<EquipmentCapabilityBasisRow[]>([])
const selectedCapItems = ref<string[]>([])
const activeCapItem = ref('')
const currentPage = ref(1)
const hasMore = ref(false)
const loading = ref(false)
const saving = ref(false)
const writeUncertain = ref(false)
const queried = ref(false)
const errorMessage = ref('')
const actionMessage = ref('')
const dialogMode = ref<DialogMode | null>(null)
const form = reactive({ capItem: '', description: '' })
const originalCapItem = ref('')
const originalDataStamp = ref(0)
const formError = ref('')
const formNotice = ref('')
const showDeleteConfirm = ref(false)
const showApproveConfirm = ref(false)

const activeRow = computed(() => rows.value.find(row => row.capItem === activeCapItem.value) ?? null)
const selectedRows = computed(() => rows.value.filter(row => selectedCapItems.value.includes(row.capItem)))
const canEdit = computed(() => activeRow.value?.issueState === 0 && !loading.value)
const canApprove = computed(() => selectedRows.value.length > 0 && selectedRows.value.every(row => row.issueState === 0) && !loading.value)
const statusMessage = computed(() => loading.value
  ? '讀取中…'
  : errorMessage.value || (queried.value ? `第 ${currentPage.value} 頁，共 ${rows.value.length} 筆${hasMore.value ? '，還有下一頁。' : '。'}` : '正在讀取設備能力項目。'))
const dialogTitle = computed(() => dialogMode.value === 'add' ? '新增設備能力項目' : dialogMode.value === 'edit' ? '編輯設備能力項目' : '複製設備能力項目')

function service() {
  return createEmsEquipmentCapabilityBasisService(props.apiBaseUrl, window.location.origin)
}

async function load(page = 1, preferredItem = ''): Promise<void> {
  if (loading.value) return
  loading.value = true
  errorMessage.value = ''
  try {
    const result = await service().list(activeState.value, appliedSearch.value, page, 50)
    rows.value = result.items
    stateCounts.value = result.stateCounts
    currentPage.value = result.page
    hasMore.value = result.hasMore
    activeCapItem.value = result.items.some(row => row.capItem === preferredItem)
      ? preferredItem
      : result.items.some(row => row.capItem === activeCapItem.value) ? activeCapItem.value : result.items[0]?.capItem ?? ''
    selectedCapItems.value = []
    queried.value = true
    writeUncertain.value = false
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備能力項目讀取失敗。'
    rows.value = []
    selectedCapItems.value = []
    activeCapItem.value = ''
    hasMore.value = false
    queried.value = true
  } finally {
    loading.value = false
  }
}

function setTab(state: CapabilityIssueState): void {
  activeState.value = state
  actionMessage.value = ''
  void load(1)
}

function search(): void {
  appliedSearch.value = searchInput.value.trim()
  actionMessage.value = ''
  void load(1)
}

function selectVisibleRows(event: Event): void {
  selectedCapItems.value = (event.target as HTMLInputElement).checked ? rows.value.map(row => row.capItem) : []
}

function toggleSelection(capItem: string): void {
  selectedCapItems.value = selectedCapItems.value.includes(capItem)
    ? selectedCapItems.value.filter(item => item !== capItem)
    : [...selectedCapItems.value, capItem]
  activeCapItem.value = capItem
}

function openForm(mode: DialogMode, row: EquipmentCapabilityBasisRow | null = null): void {
  if (mode === 'edit' && (!row || row.issueState !== 0)) return
  dialogMode.value = mode
  formError.value = ''
  formNotice.value = ''
  writeUncertain.value = false
  originalCapItem.value = row?.capItem ?? ''
  originalDataStamp.value = row?.dataStamp ?? 0
  form.capItem = row ? mode === 'copy' ? '' : row.capItem : ''
  form.description = row?.description ?? ''
}

function byteLength(value: string): number {
  return new TextEncoder().encode(value).length
}

async function saveForm(closeAfter: boolean): Promise<void> {
  if (!dialogMode.value || saving.value || writeUncertain.value) return
  formError.value = ''
  const capItem = form.capItem.trim().toUpperCase()
  const description = form.description === '' ? null : form.description
  if (!capItem) { formError.value = 'CapItem 不可空白。'; return }
  if (byteLength(capItem) > 50 || (description !== null && byteLength(description) > 255)) {
    formError.value = 'CapItem 最多 50 個 UTF-8 位元組，Description 最多 255 個 UTF-8 位元組。'
    return
  }
  saving.value = true
  try {
    let saved: EquipmentCapabilityBasisRow
    if (dialogMode.value === 'edit') {
      saved = await service().update(originalCapItem.value, description, originalDataStamp.value)
    } else {
      saved = await service().create(capItem, description)
      activeState.value = 0
      searchInput.value = ''
      appliedSearch.value = ''
      originalCapItem.value = saved.capItem
      dialogMode.value = 'edit'
    }
    originalDataStamp.value = saved.dataStamp
    form.capItem = saved.capItem
    form.description = saved.description ?? ''
    activeCapItem.value = saved.capItem
    formNotice.value = '資料已儲存。'
    actionMessage.value = `已儲存 ${saved.capItem}。`
    await load(1, saved.capItem)
    if (closeAfter) dialogMode.value = null
  } catch (error) {
    const unknownWrite = typeof error === 'object' && error !== null && 'unknownWrite' in error && Boolean((error as { unknownWrite?: unknown }).unknownWrite)
    writeUncertain.value = unknownWrite
    formError.value = error instanceof Error ? error.message : '儲存設備能力項目失敗。'
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!selectedRows.value.length || loading.value) return
  actionMessage.value = ''
  loading.value = true
  try {
    const result = await service().deleteBatch(selectedRows.value.map(({ capItem, issueState, dataStamp }) => ({ capItem, issueState, dataStamp })))
    showDeleteConfirm.value = false
    actionMessage.value = `已處理 ${result.processedCount} 筆：實體刪除 ${result.deletedCount} 筆、轉停用 ${result.disabledCount} 筆。`
    loading.value = false
    await load(1)
  } catch (error) {
    showDeleteConfirm.value = false
    actionMessage.value = error instanceof Error ? error.message : '刪除設備能力項目失敗。'
  } finally {
    loading.value = false
  }
}

async function confirmApprove(): Promise<void> {
  if (!selectedRows.value.length || loading.value) return
  actionMessage.value = ''
  loading.value = true
  try {
    for (const row of selectedRows.value) await service().approve(row.capItem, row.dataStamp)
    showApproveConfirm.value = false
    actionMessage.value = '核准操作已完成。'
    loading.value = false
    await load(1)
  } catch (error) {
    showApproveConfirm.value = false
    actionMessage.value = error instanceof Error ? error.message : '核准設備能力項目失敗。'
  } finally {
    loading.value = false
  }
}

function dateText(value: string | null): string {
  if (!value) return '—'
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value.replace('T', ' ') : parsed.toLocaleString('zh-TW', { hour12: false })
}

onMounted(() => { void load(1) })
watch(() => props.apiBaseUrl, () => { void load(1) })
</script>

<template>
  <section class="cap-basis-live" aria-label="設備能力項目設定" :aria-busy="loading">
    <header class="page-heading">
      <div><p class="eyebrow">EMS / EMS CONFIG · ES107</p><h1>設備能力項目設定</h1><p>維護設備能力項目代碼及說明，供產品設備能力設定查找。</p></div>
      <span class="live-badge">資料庫</span>
    </header>

    <section class="workspace-panel">
      <nav class="status-tabs" aria-label="簽核狀態">
        <button v-for="tab in issueTabs" :key="tab.state" type="button" :class="{ active: activeState === tab.state }" :aria-current="activeState === tab.state ? 'page' : undefined" :disabled="loading" @click="setTab(tab.state)">{{ tab.label }} <span>{{ stateCounts[String(tab.state)] ?? 0 }}</span></button>
      </nav>
      <div class="toolbar">
        <form class="search-box" @submit.prevent="search">
          <label for="cap-item-search-live">CapItem</label>
          <input id="cap-item-search-live" v-model="searchInput" maxlength="50" autocomplete="off" placeholder="輸入能力項目代碼">
          <button class="primary-button" type="submit" :disabled="loading">查詢</button>
          <button class="secondary-button" type="button" :disabled="loading && !searchInput" @click="searchInput = ''; search()">清除</button>
          <button class="secondary-button" type="button" :disabled="loading" @click="load(currentPage)">重新載入</button>
        </form>
        <div class="action-buttons">
          <button type="button" class="primary-button" :disabled="loading" @click="openForm('add')">新增</button>
          <button type="button" :disabled="!canEdit" @click="openForm('edit', activeRow)">修改</button>
          <button type="button" :disabled="!activeRow || loading" @click="openForm('copy', activeRow)">複製</button>
          <button type="button" class="danger-button" :disabled="selectedRows.length === 0 || loading" @click="showDeleteConfirm = true">刪除</button>
          <button type="button" :disabled="!canApprove" @click="showApproveConfirm = true">核准</button>
          <button type="button" :disabled="loading" @click="emit('close')">關閉</button>
        </div>
      </div>

      <div v-if="actionMessage" class="action-message" role="status"><span>{{ actionMessage }}</span><button type="button" aria-label="關閉訊息" @click="actionMessage = ''">×</button></div>
      <p class="status-message" role="status" aria-live="polite">{{ statusMessage }}</p>

      <div class="table-wrap">
        <table>
          <thead><tr>
            <th class="check-cell"><input type="checkbox" aria-label="全選目前資料" :checked="rows.length > 0 && selectedCapItems.length === rows.length" :disabled="loading || rows.length === 0" @change="selectVisibleRows"></th>
            <th>CapItem</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in rows" :key="row.capItem" :class="{ selected: activeCapItem === row.capItem }" @click="activeCapItem = row.capItem">
              <td class="check-cell"><input type="checkbox" :aria-label="`選取 ${row.capItem}`" :checked="selectedCapItems.includes(row.capItem)" @click.stop @change="toggleSelection(row.capItem)"></td>
              <td class="code-cell">{{ row.capItem }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ dateText(row.createDate) }}</td><td>{{ row.reviser || '—' }}</td><td>{{ dateText(row.reviseDate) }}</td>
            </tr>
            <tr v-if="queried && !loading && rows.length === 0"><td colspan="7" class="empty-state">{{ errorMessage || '查無設備能力項目。' }}</td></tr>
            <tr v-if="!queried"><td colspan="7" class="empty-state">讀取設備能力項目中…</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="grid-footer"><span>目前頁 {{ rows.length }} 筆</span><nav class="pagination" aria-label="設備能力項目分頁"><button type="button" :disabled="loading || currentPage <= 1" @click="load(currentPage - 1)">上一頁</button><span>第 {{ currentPage }} 頁</span><button type="button" :disabled="loading || !hasMore" @click="load(currentPage + 1)">下一頁</button></nav></footer>
    </section>

    <div v-if="dialogMode" class="dialog-backdrop" @click.self="dialogMode = null; writeUncertain = false">
      <section class="edit-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header><div><p class="eyebrow">設備能力主檔</p><h2>{{ dialogTitle }}</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="dialogMode = null; writeUncertain = false">×</button></header>
        <div class="form-grid">
          <label>CapItem <span class="required">*</span><input v-model="form.capItem" maxlength="50" :readonly="dialogMode === 'edit'" :class="{ readonly: dialogMode === 'edit' }" autocomplete="off" @blur="form.capItem = form.capItem.trim().toUpperCase()"></label>
          <label class="description-field">Description <textarea v-model="form.description" maxlength="255" rows="4"></textarea><small>{{ form.description.length }} / 255</small></label>
        </div>
        <p v-if="formError" class="error-message" role="alert">{{ formError }}</p>
        <p v-if="formNotice && !formError" class="success-message" role="status">{{ formNotice }}</p>
        <p v-if="writeUncertain" class="error-message" role="alert">寫入回應未確認。請關閉視窗後重新載入資料，再確認是否已完成。</p>
        <footer class="dialog-footer"><span>新增／複製建立未凍結資料；修改僅限未凍結資料。</span><button type="button" :disabled="saving" @click="dialogMode = null; writeUncertain = false">取消</button><button type="button" :disabled="saving || writeUncertain" @click="saveForm(false)">套用</button><button type="button" class="primary-button" :disabled="saving || writeUncertain" @click="saveForm(true)">{{ saving ? '儲存中…' : '確定' }}</button></footer>
      </section>
    </div>

    <div v-if="showDeleteConfirm" class="dialog-backdrop" @click.self="showDeleteConfirm = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除設備能力項目">
        <h2>確認處理 {{ selectedRows.length }} 筆資料？</h2>
        <p>未凍結／停用資料會實體刪除；已核准資料會轉為停用；簽核中資料不可刪除。整批在同一交易處理，任何一筆不符合時整批不變更。</p>
        <div class="target-list">{{ selectedRows.map(row => row.capItem).join('、') }}</div>
        <footer><button type="button" :disabled="loading" @click="showDeleteConfirm = false">取消</button><button type="button" class="danger-primary" :disabled="loading" @click="confirmDelete">{{ loading ? '處理中…' : '確認' }}</button></footer>
      </section>
    </div>

    <div v-if="showApproveConfirm" class="dialog-backdrop" @click.self="showApproveConfirm = false">
      <section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="確認核准設備能力項目">
        <h2>核准 {{ selectedRows.length }} 筆資料</h2>
        <p>核准方式由 MES 的 EMS_EQP_CAPBASIS 簽核設定決定；若流程設定未同步，系統會拒絕操作且不修改資料。</p>
        <div class="target-list">{{ selectedRows.map(row => row.capItem).join('、') }}</div>
        <footer><button type="button" :disabled="loading" @click="showApproveConfirm = false">取消</button><button type="button" class="primary-button" :disabled="loading" @click="confirmApprove">{{ loading ? '確認設定中…' : '確認核准' }}</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.cap-basis-live { min-height:100%; box-sizing:border-box; padding:14px; color:#302f29; background:#f5f3eb; font:inherit; }
.page-heading,.toolbar,.search-box,.action-buttons,.status-tabs,.grid-footer,.action-message,.edit-dialog header,.dialog-footer,.confirm-dialog footer,.pagination { display:flex; align-items:center; }
.page-heading { justify-content:space-between; gap:14px; margin-bottom:12px; }.page-heading h1 { margin:2px 0 4px; font-size:20px; }.page-heading p { margin:0; color:#777466; font-size:12px; }.eyebrow { color:#79745d !important; font-size:10px !important; letter-spacing:.08em; }.live-badge { padding:4px 8px; border:1px solid #b7aa65; border-radius:2px; color:#605525; background:#f7f0cc; font-size:10px; font-weight:700; }
.workspace-panel { display:flex; flex-direction:column; min-height:350px; border:1px solid #d0cdc2; background:#fbfaf6; }.status-tabs { gap:2px; padding:7px 9px 0; border-bottom:1px solid #d8d5ca; background:#f0eee6; }.status-tabs button { padding:8px 11px; border:1px solid transparent; border-bottom:0; color:#575449; background:transparent; font:inherit; font-size:12px; cursor:pointer; }.status-tabs button.active { border-color:#d2cec1; color:#28271f; background:#fbfaf6; font-weight:700; }.status-tabs span { margin-left:4px; color:#898575; font-size:10px; }
.toolbar { justify-content:space-between; flex-wrap:wrap; gap:9px; padding:10px; border-bottom:1px solid #e0ddd3; }.search-box { gap:6px; }.search-box label { color:#5b584d; font-size:12px; }.search-box input { width:200px; min-height:30px; box-sizing:border-box; padding:5px 7px; border:1px solid #bdb9ad; background:#fffefa; font:inherit; }.action-buttons { flex-wrap:wrap; gap:5px; }.cap-basis-live button { min-height:29px; padding:4px 9px; border:1px solid #aaa69a; border-radius:2px; color:#333127; background:#f6f5f0; font:inherit; font-size:12px; cursor:pointer; }.cap-basis-live button:hover:not(:disabled) { background:#ebe9df; }.cap-basis-live button:disabled { cursor:not-allowed; opacity:.45; }.cap-basis-live .primary-button { border-color:#746525; color:white; background:#746525; }.cap-basis-live .danger-button { color:#963f34; }.cap-basis-live .danger-primary { border-color:#8a392e; color:white; background:#994637; }.cap-basis-live button:focus-visible,.cap-basis-live input:focus-visible,.cap-basis-live textarea:focus-visible { outline:2px solid #a48b27; outline-offset:1px; }
.status-message { margin:7px 10px 0; color:#777466; font-size:11px; }.action-message { justify-content:space-between; gap:10px; margin:8px 10px 0; padding:8px 10px; border:1px solid #c5bc94; color:#5d512e; background:#f7f0d5; font-size:12px; }.action-message button { min-height:auto; padding:0 4px; border:0; color:inherit; background:transparent; font-size:18px; }
.table-wrap { overflow:auto; }.table-wrap table { width:100%; min-width:850px; border-collapse:collapse; font-size:12px; }.table-wrap th,.table-wrap td { padding:7px 8px; border-bottom:1px solid #e2dfd5; text-align:left; white-space:nowrap; }.table-wrap th { position:sticky; top:0; z-index:1; color:#555246; background:#eeece3; font-size:11px; font-weight:650; }.table-wrap tbody tr { cursor:pointer; }.table-wrap tbody tr:hover { background:#f5f3e9; }.table-wrap tbody tr.selected { background:#ecebdc; }.table-wrap input[type="checkbox"] { accent-color:#786b2e; }.check-cell { width:30px; text-align:center !important; }.code-cell { font-family:Consolas,monospace; font-weight:650; }.description-cell { max-width:300px; overflow:hidden; text-overflow:ellipsis; }.empty-state { height:90px; color:#777466; text-align:center !important; }.grid-footer { justify-content:space-between; gap:8px; padding:7px 10px; border-top:1px solid #dedbd1; color:#777466; font-size:11px; }.pagination { gap:8px; }.pagination button { min-height:25px; padding:2px 7px; }
.dialog-backdrop { position:fixed; inset:0; z-index:1200; display:grid; place-items:center; padding:18px; background:#17170fb0; }.edit-dialog,.confirm-dialog { width:min(600px,100%); max-height:92vh; overflow:auto; border:1px solid #aaa596; border-radius:4px; background:#fbfaf6; box-shadow:0 16px 48px #0004; }.edit-dialog header { justify-content:space-between; gap:12px; padding:12px 14px; border-bottom:1px solid #d4d0c4; }.edit-dialog header h2 { margin:0; font-size:16px; }.edit-dialog header .eyebrow { margin:0; }.icon-button { min-width:30px; padding:0 !important; font-size:20px !important; }.form-grid { display:grid; grid-template-columns:1fr; gap:12px; padding:14px; }.form-grid label { display:grid; gap:5px; color:#4b493f; font-weight:600; }.form-grid input,.form-grid textarea { width:100%; min-height:32px; box-sizing:border-box; padding:6px 8px; border:1px solid #b7b3a8; border-radius:2px; color:#302f29; background:#fffefa; font:inherit; font-weight:400; }.form-grid .readonly { color:#777466; background:#f0efe9; }.form-grid textarea { resize:vertical; }.form-grid small { color:#777466; text-align:right; font-weight:400; }.required,.error-message { color:#9c2f21; }.error-message { margin:0 14px 10px; }.success-message { margin:0 14px 10px; color:#526746; font-size:12px; }.dialog-footer { justify-content:flex-end; flex-wrap:wrap; gap:7px; padding:10px 14px; border-top:1px solid #d4d0c4; }.dialog-footer span { flex:1; min-width:200px; color:#777466; font-size:11px; }.confirm-dialog { width:min(470px,100%); padding:16px; box-sizing:border-box; }.confirm-dialog h2 { margin:0 0 8px; font-size:16px; }.confirm-dialog p { color:#686558; font-size:12px; }.confirm-dialog footer { justify-content:flex-end; gap:7px; margin-top:14px; }.target-list { padding:8px; border:1px solid #dedbd1; background:#f4f2ea; font-family:Consolas,monospace; font-size:12px; overflow-wrap:anywhere; }
@media(max-width:760px) { .cap-basis-live { padding:9px; }.toolbar { align-items:flex-start; flex-direction:column; }.search-box { width:100%; flex-wrap:wrap; }.search-box input { flex:1; min-width:120px; }.action-buttons { flex-wrap:wrap; }.status-tabs { overflow-x:auto; }.grid-footer { align-items:flex-start; flex-direction:column; }.dialog-footer span { flex-basis:100%; } }
</style>
