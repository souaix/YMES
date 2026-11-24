<script setup lang="ts">
import { computed, ref, watch } from 'vue'

interface LocatorSlot {
  locatorNo: string
  verticalPosition: number
  horizontalPosition: number
}
interface StockerRow {
  stockerNo: string
  factoryNo: string
  areaNo: string
  verticalNumber: number
  horizontalNumber: number
  capacityCtrl: number
  qtyLimit: number | null
  description: string
  issueState: number
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
  dataStamp: number
  slots: LocatorSlot[]
}
interface StockerDraft {
  stockerNo: string
  areaNo: string
  verticalNumber: string
  horizontalNumber: string
  capacityCtrl: boolean
  qtyLimit: string
  description: string
}
type FormMode = 'add' | 'edit' | 'copy' | null

const props = withDefaults(defineProps<{ developmentPreview: boolean; currentUserNo?: string }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.inv.stocker-basis.v1'
const maxPreviewSlots = 500
const issueStates = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]
const areas = [
  { areaNo: 'WIP-A1', areaName: '一廠在製區 A1', factoryNo: 'F01' },
  { areaNo: 'WIP-A2', areaName: '一廠在製區 A2', factoryNo: 'F01' },
  { areaNo: 'WIP-B1', areaName: '二廠在製區 B1', factoryNo: 'F02' },
]

function createSlots(stockerNo: string, verticalNumber: number, horizontalNumber: number): LocatorSlot[] {
  const slots: LocatorSlot[] = []
  for (let horizontal = 1; horizontal <= horizontalNumber; horizontal += 1) {
    for (let vertical = 1; vertical <= verticalNumber; vertical += 1) {
      const sequence = (horizontal - 1) * verticalNumber + vertical
      slots.push({ locatorNo: `${stockerNo}${sequence}`, verticalPosition: vertical, horizontalPosition: horizontal })
    }
  }
  return slots
}
function fixture(stockerNo: string, factoryNo: string, areaNo: string, verticalNumber: number, horizontalNumber: number, issueState: number, capacityCtrl: number, qtyLimit: number | null, description: string, creator: string, createDate: string, slots?: LocatorSlot[]): StockerRow {
  return {
    stockerNo, factoryNo, areaNo, verticalNumber, horizontalNumber, capacityCtrl, qtyLimit, description,
    issueState, creator, createDate, reviser: issueState === 2 ? 'DEV002' : '', reviseDate: issueState === 2 ? '2026-10-21 09:10' : '',
    dataStamp: issueState === 2 ? 3 : 1, slots: slots ?? createSlots(stockerNo, verticalNumber, horizontalNumber),
  }
}
const seedRows: StockerRow[] = [
  fixture('STK-DEV-A', 'F01', 'WIP-A1', 4, 3, 0, 1, 12, 'DEV 假資料：在製區 A1 主貨架。', 'DEV001', '2026-10-12 08:15'),
  fixture('STK-DEV-B', 'F01', 'WIP-A2', 3, 2, 2, 0, null, 'DEV 假資料：線邊暫存架。', 'DEV001', '2026-10-16 10:20'),
  fixture('STK-DEV-C', 'F02', 'WIP-B1', 3, 4, 1, 1, 10, 'DEV 假資料：簽核中貨架。', 'DEV003', '2026-10-20 13:40'),
  fixture('STK-DEV-D', 'F02', 'WIP-B1', 2, 2, -1, 0, null, 'DEV 假資料：不使用貨架示例。', 'DEV002', '2026-10-25 16:20'),
]
function readRows(): StockerRow[] {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (raw) {
      const value = JSON.parse(raw) as StockerRow[]
      if (Array.isArray(value) && value.every((row) => row && typeof row.stockerNo === 'string' && Array.isArray(row.slots))) return value
    }
  } catch { /* use fixtures when local storage is unavailable */ }
  return structuredClone(seedRows)
}

const rows = ref<StockerRow[]>(readRows())
const issueState = ref(0)
const queryStockerNo = ref('')
const appliedStockerNo = ref('')
const selectedNo = ref(rows.value[0]?.stockerNo ?? '')
const selectedNos = ref<string[]>([])
const notice = ref('DEV 假資料：不會呼叫 MES 或正式 API。')
const currentUser = computed(() => props.currentUserNo.trim() || 'DEVUSER')
const formMode = ref<FormMode>(null)
const draft = ref<StockerDraft>(blankDraft())
const draftSlots = ref<LocatorSlot[]>([])
const formError = ref('')
const stockerNoBaseline = ref('')
const verticalBaseline = ref('')
const horizontalBaseline = ref('')
const selectedSlotKey = ref('')
const renameSlotOpen = ref(false)
const renameSlotDraft = ref('')

function blankDraft(): StockerDraft {
  return { stockerNo: '', areaNo: '', verticalNumber: '', horizontalNumber: '', capacityCtrl: false, qtyLimit: '', description: '' }
}
const visibleRows = computed(() => {
  const query = appliedStockerNo.value.trim().toLocaleUpperCase()
  return rows.value.filter((row) => row.issueState === issueState.value && (!query || row.stockerNo.toLocaleUpperCase().includes(query)))
    .sort((a, b) => a.stockerNo.localeCompare(b.stockerNo))
})
const activeStocker = computed(() => visibleRows.value.find((row) => row.stockerNo === selectedNo.value) ?? null)
const selectedRows = computed(() => visibleRows.value.filter((row) => selectedNos.value.includes(row.stockerNo)))
const actionRows = computed(() => {
  const selected = [...selectedRows.value]
  const active = visibleRows.value.find((row) => row.stockerNo === selectedNo.value)
  if (active && !selected.some((row) => row.stockerNo === active.stockerNo)) selected.push(active)
  return selected
})
const activeSlots = computed(() => [...(activeStocker.value?.slots ?? [])].sort((a, b) => a.locatorNo.localeCompare(b.locatorNo)))
const canEdit = computed(() => activeStocker.value?.issueState === 0)
const selectedDraftSlot = computed(() => draftSlots.value.find((slot) => `${slot.verticalPosition}-${slot.horizontalPosition}` === selectedSlotKey.value) ?? null)
const areaName = (areaNo: string) => areas.find((area) => area.areaNo === areaNo)?.areaName ?? areaNo
const factoryForArea = (areaNo: string) => areas.find((area) => area.areaNo === areaNo)?.factoryNo ?? ''

function runQuery() { appliedStockerNo.value = queryStockerNo.value.trim() }
watch(visibleRows, (visible) => {
  if (!visible.some((row) => row.stockerNo === selectedNo.value)) selectedNo.value = visible[0]?.stockerNo ?? ''
  selectedNos.value = selectedNos.value.filter((no) => visible.some((row) => row.stockerNo === no))
})
watch(() => draft.value.capacityCtrl, (enabled) => {
  if (!enabled) draft.value.qtyLimit = ''
})
function selectRow(row: StockerRow) { selectedNo.value = row.stockerNo }
function toggleRow(row: StockerRow) {
  selectedNo.value = row.stockerNo
  selectedNos.value = selectedNos.value.includes(row.stockerNo) ? selectedNos.value.filter((no) => no !== row.stockerNo) : [...selectedNos.value, row.stockerNo]
}
function openForm(mode: Exclude<FormMode, null>) {
  const source = activeStocker.value
  if (mode !== 'add' && !source) return
  if (mode === 'edit' && source?.issueState !== 0) { notice.value = '只有未簽核貨架可以修改。'; return }
  formError.value = ''
  formMode.value = mode
  if (mode === 'add' || !source) {
    draft.value = blankDraft()
    draftSlots.value = []
    stockerNoBaseline.value = ''
    verticalBaseline.value = ''
    horizontalBaseline.value = ''
  } else {
    draft.value = {
      stockerNo: source.stockerNo,
      areaNo: source.areaNo,
      verticalNumber: String(source.verticalNumber),
      horizontalNumber: String(source.horizontalNumber),
      capacityCtrl: source.capacityCtrl === 1,
      qtyLimit: source.qtyLimit === null ? '' : String(source.qtyLimit),
      description: source.description,
    }
    draftSlots.value = structuredClone(source.slots)
    stockerNoBaseline.value = source.stockerNo
    verticalBaseline.value = String(source.verticalNumber)
    horizontalBaseline.value = String(source.horizontalNumber)
  }
}
function regenerateDraftSlots() {
  const vertical = Number(draft.value.verticalNumber)
  const horizontal = Number(draft.value.horizontalNumber)
  if (!Number.isInteger(vertical) || !Number.isInteger(horizontal) || vertical < 1 || horizontal < 1 || vertical * horizontal > maxPreviewSlots) {
    draftSlots.value = []
    return
  }
  draftSlots.value = createSlots(draft.value.stockerNo.trim().toLocaleUpperCase(), vertical, horizontal)
}
function stockerNoBlur() {
  draft.value.stockerNo = draft.value.stockerNo.trim().toLocaleUpperCase()
  if (draft.value.stockerNo !== stockerNoBaseline.value) {
    stockerNoBaseline.value = draft.value.stockerNo
    regenerateDraftSlots()
  }
}
function dimensionsChanged() {
  const vertical = String(draft.value.verticalNumber)
  const horizontal = String(draft.value.horizontalNumber)
  if (vertical === verticalBaseline.value && horizontal === horizontalBaseline.value) return
  verticalBaseline.value = vertical
  horizontalBaseline.value = horizontal
  regenerateDraftSlots()
}
function openSlotRename(slot: LocatorSlot) {
  selectedSlotKey.value = `${slot.verticalPosition}-${slot.horizontalPosition}`
  renameSlotDraft.value = slot.locatorNo
  renameSlotOpen.value = true
}
function saveSlotRename() {
  if (!selectedDraftSlot.value) return
  const locatorNo = renameSlotDraft.value.trim()
  if (!locatorNo) { formError.value = 'LocatorNo 不可空白。'; return }
  selectedDraftSlot.value.locatorNo = locatorNo
  renameSlotOpen.value = false
  formError.value = ''
}
function saveForm(closeAfterSave: boolean) {
  const stockerNo = draft.value.stockerNo.trim().toLocaleUpperCase()
  const vertical = Number(draft.value.verticalNumber)
  const horizontal = Number(draft.value.horizontalNumber)
  const qtyLimit = Number(draft.value.qtyLimit)
  const existing = rows.value.find((row) => row.stockerNo === stockerNo)
  if (!stockerNo) { formError.value = 'StockerNo 為必填。'; return }
  if (!draft.value.areaNo) { formError.value = 'AreaNo 為必填，請從 Area 清單選取。'; return }
  if (!Number.isInteger(vertical) || vertical < 1 || !Number.isInteger(horizontal) || horizontal < 1) { formError.value = 'V Number 與 H Number 必須是大於 0 的整數。'; return }
  if (vertical * horizontal > maxPreviewSlots) { formError.value = `DEV 預覽最多支援 ${maxPreviewSlots} 個儲位格。`; return }
  if (draft.value.capacityCtrl && (!Number.isInteger(qtyLimit) || qtyLimit < 1)) { formError.value = '勾選 QtyLimit 時，必須輸入大於 0 的整數。舊程式錯誤訊息雖提到 999 上限，驗證程式實際只有下限。'; return }
  if (existing && !(formMode.value === 'edit' && existing.stockerNo === activeStocker.value?.stockerNo)) { formError.value = 'StockerNo 已存在。'; return }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  const slots = draftSlots.value.length === vertical * horizontal ? structuredClone(draftSlots.value) : createSlots(stockerNo, vertical, horizontal)
  if (formMode.value === 'edit' && activeStocker.value) {
    Object.assign(activeStocker.value, {
      factoryNo: factoryForArea(draft.value.areaNo), areaNo: draft.value.areaNo, verticalNumber: vertical, horizontalNumber: horizontal,
      capacityCtrl: draft.value.capacityCtrl ? 1 : 0, qtyLimit: draft.value.capacityCtrl ? qtyLimit : null,
      description: draft.value.description, slots, reviser: currentUser.value, reviseDate: now, dataStamp: activeStocker.value.dataStamp + 1,
    })
    notice.value = `已模擬修改 ${stockerNo}。儲位明細會整組替換，和舊 Service 的處理方式一致。`
  } else {
    const created: StockerRow = {
      stockerNo, factoryNo: factoryForArea(draft.value.areaNo), areaNo: draft.value.areaNo, verticalNumber: vertical, horizontalNumber: horizontal,
      capacityCtrl: draft.value.capacityCtrl ? 1 : 0, qtyLimit: draft.value.capacityCtrl ? qtyLimit : null,
      description: draft.value.description, issueState: 0, creator: currentUser.value, createDate: now,
      reviser: '', reviseDate: '', dataStamp: 1, slots,
    }
    rows.value = [...rows.value, created].sort((a, b) => a.stockerNo.localeCompare(b.stockerNo))
    selectedNo.value = stockerNo
    selectedNos.value = [stockerNo]
    notice.value = formMode.value === 'copy' ? `已模擬複製貨架 ${stockerNo}；按舊程式帶入的 LocatorNo，未自動加新編號前綴。` : `已模擬新增 ${stockerNo}，狀態為未簽核。`
  }
  if (closeAfterSave) formMode.value = null
  formError.value = ''
}
function deleteSelected() {
  if (!actionRows.value.length) { notice.value = '請先選取貨架。'; return }
  if (!window.confirm(`確定刪除選取的 ${actionRows.value.length} 筆貨架資料？`)) return
  const targets = [...actionRows.value]
  const hardDelete = targets.filter((row) => row.issueState === 0 || row.issueState === -1)
  const softDelete = targets.filter((row) => row.issueState === 2)
  const blocked = targets.filter((row) => row.issueState === 1)
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of softDelete) Object.assign(row, { issueState: -1, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  rows.value = rows.value.filter((row) => !hardDelete.some((target) => target.stockerNo === row.stockerNo))
  selectedNos.value = selectedNos.value.filter((no) => !hardDelete.some((row) => row.stockerNo === no))
  const removed = hardDelete.length + softDelete.length
  notice.value = blocked.length
    ? `已模擬處理 ${removed} 筆（未簽核／不使用實體刪除，已簽核改為不使用）；${blocked.length} 筆簽核中資料被拒絕。舊 Form 逐筆呼叫，遇錯停止，可能部分成功。`
    : `已模擬處理 ${removed} 筆貨架（未簽核／不使用實體刪除，已簽核改為不使用）。`
}
function approveSelected() {
  const targets = actionRows.value.filter((row) => row.issueState === 0)
  if (!targets.length) { notice.value = '只有未簽核貨架可以核准。'; return }
  if (!window.confirm(`確定核准 ${targets.length} 筆貨架？DEV 會直接模擬 Active。`)) return
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of targets) Object.assign(row, { issueState: 2, reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 })
  notice.value = `已模擬核准 ${targets.length} 筆；正式流程仍需核對 IssueManagement 與簽核設定。`
}
watch(rows, (value) => {
  try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { notice.value = '瀏覽器未允許 localStorage，資料只保留在目前頁面。' }
}, { deep: true })
</script>

<template>
  <main class="stocker-basis" aria-label="在製貨架基本資料 DEV">
    <header class="page-heading"><div><p>INV · IV103 · ActiveModule WIP</p><h1>在製貨架基本資料</h1></div><button type="button" class="close-icon" aria-label="Close" @click="emit('close')">×</button></header>
    <div class="query-strip">
      <fieldset class="state-filter"><legend>Issue State</legend><label v-for="state in issueStates" :key="state.value"><input v-model="issueState" type="radio" :value="state.value" />{{ state.label }}</label></fieldset>
      <label class="query-label">StockerNo <input v-model="queryStockerNo" type="search" placeholder="輸入貨架編號" @keydown.enter.prevent="runQuery" /></label>
      <button type="button" @click="runQuery">Query</button><span class="preview-note">Development Preview · 假資料</span>
    </div>
    <div v-if="notice" class="notice" role="status">{{ notice }}</div>
    <div class="work-area">
      <section class="main-panel" aria-label="貨架主檔與儲位">
        <div class="table-scroll"><table class="basis-table"><thead><tr><th class="selector"></th><th>StockerNo</th><th>FactoryNo</th><th>AreaNo</th><th>V Number</th><th>H Number</th><th>Capacity Ctrl</th><th>Description</th><th>QtyLimit</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in visibleRows" :key="row.stockerNo" :class="{ active: row.stockerNo === selectedNo, checked: selectedNos.includes(row.stockerNo) }" @click="selectRow(row)" @dblclick="row.issueState === 0 && openForm('edit')">
            <td class="selector"><input type="checkbox" :checked="selectedNos.includes(row.stockerNo)" :aria-label="`選取 ${row.stockerNo}`" @click.stop="toggleRow(row)" /></td>
            <td class="mono">{{ row.stockerNo }}</td><td>{{ row.factoryNo }}</td><td>{{ row.areaNo }}</td><td class="number">{{ row.verticalNumber }}</td><td class="number">{{ row.horizontalNumber }}</td><td>{{ row.capacityCtrl === 1 ? 'Yes' : 'No' }}</td><td class="description">{{ row.description }}</td><td class="number">{{ row.qtyLimit ?? '' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate }}</td>
          </tr>
          <tr v-if="!visibleRows.length"><td class="empty" colspan="13">沒有符合條件的貨架資料</td></tr>
        </tbody></table></div>
        <footer class="grid-footer"><span>{{ visibleRows.length }} 筆 · DataStamp {{ activeStocker?.dataStamp ?? '—' }}</span><span>儲位列會依選取貨架重新查詢</span></footer>
        <div class="detail-heading"><strong>{{ activeStocker ? `${activeStocker.stockerNo} · Locator 明細` : 'Locator 明細' }}</strong><span>{{ activeSlots.length }} 格</span></div>
        <div class="table-scroll detail-scroll"><table class="detail-table"><thead><tr><th>StockerNo</th><th>LocatorNo</th><th>VerticalPosition</th><th>HorizontalPosition</th></tr></thead><tbody>
          <tr v-for="slot in activeSlots" :key="`${slot.verticalPosition}-${slot.horizontalPosition}`"><td>{{ activeStocker?.stockerNo }}</td><td class="mono">{{ slot.locatorNo }}</td><td class="number">{{ slot.verticalPosition }}</td><td class="number">{{ slot.horizontalPosition }}</td></tr>
          <tr v-if="!activeSlots.length"><td class="empty" colspan="4">沒有 Locator 明細</td></tr>
        </tbody></table></div>
      </section>
      <aside class="side-actions">
        <button type="button" class="primary" @click="openForm('add')">ADD (A)</button><button type="button" :disabled="!canEdit" @click="openForm('edit')">EDIT (E)</button>
        <button type="button" class="danger" :disabled="!actionRows.length" @click="deleteSelected">DEL (D)</button><button type="button" :disabled="!activeStocker" @click="openForm('copy')">Copy (C)</button>
        <button type="button" :disabled="!actionRows.some((row) => row.issueState === 0)" @click="approveSelected">Approve (Y)</button><button type="button" class="close-button" @click="emit('close')">Close (X)</button>
      </aside>
    </div>

    <div v-if="formMode" class="modal-backdrop" @click.self="formMode = null">
      <form class="editor" @submit.prevent="saveForm(true)">
        <header><div><p>INV · IV103</p><h2>Stocker Basis · {{ formMode.toUpperCase() }}</h2></div><button type="button" aria-label="關閉" @click="formMode = null">×</button></header>
        <fieldset class="field-grid">
          <legend>貨架資料</legend>
          <label>StockerNo <input v-model="draft.stockerNo" :readonly="formMode === 'edit'" @blur="stockerNoBlur" /></label>
          <label>AreaNo <select v-model="draft.areaNo"><option value="">選擇 Area</option><option v-for="area in areas" :key="area.areaNo" :value="area.areaNo">{{ area.areaNo }} · {{ area.areaName }} · {{ area.factoryNo }}</option></select><small>舊 Form 使用 frmCOMSearch；DEV 用明確標註的 Area fixtures。</small></label>
          <label>V Number <input v-model="draft.verticalNumber" type="number" min="1" max="500" step="1" @change="dimensionsChanged" @blur="dimensionsChanged" /></label>
          <label>H Number <input v-model="draft.horizontalNumber" type="number" min="1" max="500" step="1" @change="dimensionsChanged" @blur="dimensionsChanged" /></label>
          <label class="check-field"><input v-model="draft.capacityCtrl" type="checkbox" /> QtyLimit</label>
          <label>QtyLimit <input v-model="draft.qtyLimit" type="number" min="1" step="1" :disabled="!draft.capacityCtrl" /></label>
          <label class="wide-field">Description <textarea v-model="draft.description" rows="2"></textarea></label>
        </fieldset>
        <section class="layout-section" aria-label="Locator 位置配置">
          <header><div><h3>Locator 配置</h3><p>水平位置排列 V Number 欄；垂直位置排列 H Number 列。選取方格可手動修改 LocatorNo。</p></div><span>{{ draftSlots.length }} / {{ Number(draft.verticalNumber || 0) * Number(draft.horizontalNumber || 0) }} 格</span></header>
          <div class="layout-scroll"><div class="locator-layout" :style="{ '--vertical-count': Math.max(1, Math.min(Number(draft.verticalNumber) || 1, maxPreviewSlots)) }">
            <button v-for="slot in draftSlots" :key="`${slot.verticalPosition}-${slot.horizontalPosition}`" type="button" class="locator-tile" @click="openSlotRename(slot)"><small>{{ slot.verticalPosition }} · {{ slot.horizontalPosition }}</small><strong>{{ slot.locatorNo }}</strong></button>
          </div><p v-if="Number(draft.verticalNumber || 0) * Number(draft.horizontalNumber || 0) > maxPreviewSlots" class="layout-warning">格數超過 DEV 預覽上限，請調整尺寸後再儲存。</p><p v-else-if="!draftSlots.length" class="layout-empty">輸入 StockerNo 與 V/H Number 後離開欄位，即可產生 Locator 方格。</p></div>
        </section>
        <p v-if="formError" class="form-error" role="alert">{{ formError }}</p>
        <footer><span class="audit">{{ formMode === 'edit' ? `Creator ${activeStocker?.creator ?? ''} · ${activeStocker?.createDate ?? ''} · DataStamp ${activeStocker?.dataStamp ?? ''}` : '新增資料會以 IssueState=0 建立。' }}</span><button type="button" @click="formMode = null">Close</button><button type="button" @click="saveForm(false)">Apply</button><button class="primary" type="submit">Confirm</button></footer>
      </form>
    </div>

    <div v-if="renameSlotOpen" class="modal-backdrop rename-backdrop" @click.self="renameSlotOpen = false">
      <form class="rename-dialog" @submit.prevent="saveSlotRename">
        <header><h3>LocatorNo · V{{ selectedDraftSlot?.verticalPosition }} / H{{ selectedDraftSlot?.horizontalPosition }}</h3><button type="button" aria-label="關閉" @click="renameSlotOpen = false">×</button></header>
        <label>LocatorNo <input v-model="renameSlotDraft" autofocus /></label>
        <footer><button type="button" @click="renameSlotOpen = false">Cancel</button><button class="primary" type="submit">Confirm</button></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.stocker-basis{display:flex;flex-direction:column;width:100%;height:100%;min-height:0;gap:7px;padding:10px;box-sizing:border-box;background:#f4f2eb;color:#292820;font:13px/1.35 "Segoe UI","Microsoft JhengHei",sans-serif}.page-heading{display:flex;align-items:center;justify-content:space-between;padding:0 2px 6px;border-bottom:1px solid #cbc8bb}.page-heading p,.editor header p{margin:0;color:#777366;font-size:11px;letter-spacing:.06em}.page-heading h1{margin:0;font-size:20px;font-weight:600}.close-icon{width:29px;height:27px;border:1px solid #888477;background:#e7e4d9;color:#35332d;font-size:19px}.query-strip{display:flex;align-items:center;gap:7px;min-height:36px;flex-wrap:wrap}.state-filter{display:flex;align-items:center;gap:8px;margin:0;padding:3px 8px;border:1px solid #c8c5b9;background:#eeece4}.state-filter legend{padding:0 4px;color:#6d695e}.state-filter label{display:flex;align-items:center;gap:3px;white-space:nowrap}.state-filter input{margin:0;accent-color:#56704f}.query-label{display:flex;align-items:center;gap:6px}.query-label input{width:180px;min-height:27px;border:1px solid #999587;padding:4px 7px;background:#fffef8;font:inherit}.preview-note{margin-left:auto;color:#777366;font-size:11px}.stocker-basis button{min-height:28px;padding:4px 9px;border:1px solid #8c887b;border-radius:2px;background:#fbfaf5;color:#292820;font:inherit;cursor:pointer;white-space:nowrap}.stocker-basis button:hover:not(:disabled){background:#e7e4d9}.stocker-basis button:disabled{opacity:.5;cursor:not-allowed}.stocker-basis button.primary{border-color:#48634d;background:#506c54;color:white}.stocker-basis button.danger{color:#8d302a}.notice{min-height:18px;color:#625f55;font-size:11px}.work-area{display:flex;flex:1;min-height:0;gap:8px}.main-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c6c2b6;background:#fffef9}.table-scroll{flex:1;min-height:100px;overflow:auto}.basis-table,.detail-table{width:max-content;min-width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.stocker-basis th,.stocker-basis td{height:27px;padding:4px 6px;border-right:1px solid #e0ded6;border-bottom:1px solid #e3e1da;text-align:left}.stocker-basis th{position:sticky;top:0;z-index:1;background:#e8e6dd;color:#514f46;font-weight:600}.stocker-basis tbody tr{cursor:pointer}.stocker-basis tbody tr:hover{background:#f5f2e5}.stocker-basis tbody tr.active{background:#e4ecde}.stocker-basis tbody tr.checked:not(.active){background:#f5efd9}.selector{width:28px;text-align:center}.selector input{margin:0}.mono{font-family:Consolas,"Courier New",monospace}.number{text-align:right!important}.description{max-width:190px;overflow:hidden;text-overflow:ellipsis}.grid-footer,.detail-heading{display:flex;justify-content:space-between;gap:10px;align-items:center;min-height:23px;padding:3px 8px;border-top:1px solid #d6d3c8;color:#6e6a60;font-size:11px}.detail-heading{color:#514f46;background:#f0eee6}.detail-scroll{flex:0.8}.side-actions{width:104px;display:flex;flex:none;flex-direction:column;gap:6px}.side-actions button{width:100%}.side-actions .close-button{margin-top:auto}.empty{text-align:center!important;color:#817d72;padding:18px!important}.modal-backdrop{position:fixed;inset:0;z-index:25;display:flex;align-items:center;justify-content:center;padding:14px;background:rgb(25 24 20 / 42%)}.editor{width:min(920px,97vw);height:min(760px,95vh);display:flex;flex-direction:column;gap:9px;overflow:auto;padding:12px;border:1px solid #898578;background:#f8f6ef;box-shadow:0 8px 24px #0004}.editor>header,.rename-dialog>header{display:flex;align-items:center;justify-content:space-between;padding-bottom:7px;border-bottom:1px solid #cfccbf}.editor h2,.rename-dialog h3{margin:0;font-size:16px;font-weight:600}.editor>header button,.rename-dialog>header button{width:28px;padding:0;font-size:19px}.field-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px 12px;margin:0;padding:10px;border:1px solid #c8c4b7}.field-grid legend{padding:0 5px}.field-grid>label,.rename-dialog>label{display:flex;flex-direction:column;gap:3px}.field-grid input,.field-grid select,.field-grid textarea,.rename-dialog input{box-sizing:border-box;min-height:28px;border:1px solid #999587;padding:4px 7px;color:#292820;background:#fffef8;font:inherit}.field-grid small{color:#777366;font-size:10px}.field-grid .check-field{flex-direction:row;align-items:center}.field-grid .check-field input{min-height:auto}.wide-field{grid-column:1/-1}.field-grid textarea{resize:vertical}.layout-section{display:flex;flex:1;min-height:150px;flex-direction:column;border:1px solid #c8c4b7;background:#fffef9}.layout-section>header{display:flex;align-items:flex-start;justify-content:space-between;gap:12px;padding:7px 9px;border-bottom:1px solid #d8d5cb}.layout-section h3{margin:0;font-size:13px}.layout-section p{margin:2px 0 0;color:#777366;font-size:10px}.layout-section>header>span{white-space:nowrap;color:#625f55;font-size:11px}.layout-scroll{flex:1;overflow:auto;padding:9px}.locator-layout{display:grid;grid-template-columns:repeat(var(--vertical-count),minmax(88px,1fr));gap:5px;width:max-content;min-width:100%}.locator-tile{display:flex!important;min-width:88px;min-height:53px!important;flex-direction:column;align-items:center;justify-content:center;gap:2px;padding:4px!important;border-color:#aaa696!important;background:#eeeae0!important}.locator-tile small{color:#777366;font-size:9px}.locator-tile strong{font-family:Consolas,"Courier New",monospace;font-size:11px}.layout-warning,.layout-empty{padding:8px;color:#8a302a!important}.form-error{margin:0;color:#9a2924}.editor footer,.rename-dialog footer{display:flex;align-items:center;gap:7px;padding-top:8px;border-top:1px solid #cfccbf}.editor footer>.audit{flex:1;color:#777366;font-size:10px}.rename-backdrop{z-index:30}.rename-dialog{width:min(400px,94vw);display:flex;flex-direction:column;gap:12px;padding:12px;border:1px solid #898578;background:#f8f6ef;box-shadow:0 8px 24px #0004}.rename-dialog footer{justify-content:flex-end}@media(max-width:860px){.work-area{gap:5px}.side-actions{width:88px}.preview-note{margin-left:0}.field-grid{grid-template-columns:1fr}.wide-field{grid-column:auto}.editor{height:94vh}.detail-scroll{flex:0.65}}@media(max-width:560px){.query-strip{align-items:flex-start}.state-filter{width:100%;flex-wrap:wrap}.work-area{flex-direction:column}.main-panel{min-height:300px}.side-actions{width:100%;flex-direction:row;flex-wrap:wrap}.side-actions button{width:auto}.side-actions .close-button{margin:0}.editor footer{flex-wrap:wrap}.editor footer>.audit{flex-basis:100%}}
</style>
