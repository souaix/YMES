<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type FormMode = 'add' | 'edit' | 'copy'
interface EquipmentTypeRow { equipmentType: string; capacity: string; pictureName: string; description: string; creator: string; createDate: string }
interface MapRecord { equipmentType: string; breakDownNo: string; description: string; creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
interface DevState { version: 1; equipmentTypes: EquipmentTypeRow[]; records: MapRecord[] }
interface BreakdownChoice { breakDownNo: string; breakDownCause: string; issueState?: number }
interface Draft { equipmentType: string; breakDownNo: string; description: string }

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pm.eqp-type-breakdown-map.v1'
const breakdownStorageKey = 'ymes.dev.pm.breakdown-basis.v1'
const currentUser = 'DEV01'

function seedState(): DevState {
  return {
    version: 1,
    equipmentTypes: [
      { equipmentType: 'ETCHER-01', capacity: '4 Chamber', pictureName: 'ETCHER-01.jpg', description: '乾式蝕刻機台', creator: 'PMUSER', createDate: '2026-10-10 09:15' },
      { equipmentType: 'CVD-02', capacity: '2 Chamber', pictureName: 'CVD-02.jpg', description: '薄膜沉積設備', creator: 'PMUSER', createDate: '2026-10-12 10:20' },
      { equipmentType: 'SCRUBBER-03', capacity: '1 Unit', pictureName: '', description: '廢氣洗滌設備', creator: 'EQPUSER', createDate: '2026-10-16 14:00' },
      { equipmentType: 'WET-BENCH-04', capacity: '8 Station', pictureName: 'WB-04.png', description: '濕製程清洗設備', creator: 'EQPUSER', createDate: '2026-10-20 08:40' },
    ],
    records: [
      { equipmentType: 'ETCHER-01', breakDownNo: 'BD-ELEC-01', description: '優先確認 RF 電源與接地。', creator: 'PMUSER', createDate: '2026-10-21 11:30', reviser: 'PMUSER', reviseDate: '2026-11-02 15:10', dataStamp: 1 },
      { equipmentType: 'ETCHER-01', breakDownNo: 'BD-MECH-02', description: '檢查載台與傳動部件。', creator: 'PMUSER', createDate: '2026-10-21 11:32', reviser: '', reviseDate: '', dataStamp: 0 },
      { equipmentType: 'ETCHER-01', breakDownNo: 'BD-SENSOR-03', description: '', creator: 'DEV02', createDate: '2026-10-21 11:34', reviser: '', reviseDate: '', dataStamp: 0 },
      { equipmentType: 'CVD-02', breakDownNo: 'BD-ELEC-01', description: '', creator: 'PMUSER', createDate: '2026-10-22 09:05', reviser: '', reviseDate: '', dataStamp: 0 },
      { equipmentType: 'CVD-02', breakDownNo: 'BD-HYD-08', description: '確認冷卻水壓及管路。', creator: 'PMUSER', createDate: '2026-10-22 09:08', reviser: '', reviseDate: '', dataStamp: 0 },
      { equipmentType: 'SCRUBBER-03', breakDownNo: 'BD-HYD-08', description: '巡檢泵浦壓力與洩漏。', creator: 'EQPUSER', createDate: '2026-10-24 16:40', reviser: '', reviseDate: '', dataStamp: 0 },
    ],
  }
}

function loadState(): DevState {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(storageKey) ?? 'null') as DevState | null
    if (parsed?.version === 1 && Array.isArray(parsed.equipmentTypes) && Array.isArray(parsed.records)) return parsed
  } catch { /* Keep the DEV screen available when browser storage is unavailable. */ }
  return seedState()
}

const state = ref<DevState>(loadState())
const equipmentTypeSearch = ref('')
const activeEquipmentType = ref(state.value.equipmentTypes[0]?.equipmentType ?? '')
const activeMapKey = ref('')
const selectedMapKeys = ref<string[]>([])
const formMode = ref<FormMode | null>(null)
const draft = ref<Draft | null>(null)
const originalDataStamp = ref(0)
const message = ref('')
const messageKind = ref<'success' | 'error'>('success')
const nowLabel = computed(() => new Date().toLocaleString('zh-TW', { hour12: false }))
const filteredEquipmentTypes = computed(() => {
  const query = equipmentTypeSearch.value.trim().toLocaleLowerCase()
  return state.value.equipmentTypes.filter((row) => !query || row.equipmentType.toLocaleLowerCase().includes(query))
    .sort((a, b) => a.equipmentType.localeCompare(b.equipmentType))
})
const activeType = computed(() => state.value.equipmentTypes.find((row) => row.equipmentType === activeEquipmentType.value) ?? null)
const visibleMaps = computed(() => state.value.records.filter((row) => row.equipmentType === activeEquipmentType.value)
  .sort((a, b) => a.breakDownNo.localeCompare(b.breakDownNo)))
const activeMap = computed(() => state.value.records.find((row) => mapKey(row) === activeMapKey.value) ?? null)
const actionTargets = computed(() => selectedMapKeys.value.length ? [...selectedMapKeys.value] : activeMap.value ? [mapKey(activeMap.value)] : [])
const allVisibleSelected = computed(() => visibleMaps.value.length > 0 && visibleMaps.value.every((row) => selectedMapKeys.value.includes(mapKey(row))))
const breakdownChoices = computed<BreakdownChoice[]>(() => {
  try {
    const parsed = JSON.parse(window.localStorage.getItem(breakdownStorageKey) ?? 'null') as { records?: Array<{ breakDownNo?: string; breakDownCause?: string; issueState?: number }> } | null
    if (Array.isArray(parsed?.records)) return parsed.records.filter((row) => row.breakDownNo && row.issueState === 2).map((row) => ({ breakDownNo: row.breakDownNo!, breakDownCause: row.breakDownCause ?? '', issueState: row.issueState }))
  } catch { /* Use local lookup fixtures below. */ }
  return [
    { breakDownNo: 'BD-ELEC-01', breakDownCause: '電源異常', issueState: 2 },
    { breakDownNo: 'BD-SENSOR-03', breakDownCause: '感測器訊號異常', issueState: 2 },
    { breakDownNo: 'BD-HYD-08', breakDownCause: '油壓不足', issueState: 2 },
  ]
})
const breakdownNameByNo = computed(() => new Map(breakdownChoices.value.map((row) => [row.breakDownNo.toLocaleUpperCase(), row.breakDownCause])))
watch(state, (value) => { try { window.localStorage.setItem(storageKey, JSON.stringify(value)) } catch { /* The page remains usable without persistence. */ } }, { deep: true })

function mapKey(row: Pick<MapRecord, 'equipmentType' | 'breakDownNo'>) { return JSON.stringify([row.equipmentType, row.breakDownNo]) }
function toast(text: string, kind: 'success' | 'error' = 'success') { message.value = text; messageKind.value = kind }
function selectEquipmentType(value: string) { activeEquipmentType.value = value; activeMapKey.value = ''; selectedMapKeys.value = [] }
function selectMap(row: MapRecord) { activeMapKey.value = mapKey(row); selectedMapKeys.value = [] }
function toggleMap(row: MapRecord) {
  const key = mapKey(row)
  selectedMapKeys.value = selectedMapKeys.value.includes(key) ? selectedMapKeys.value.filter((value) => value !== key) : [...selectedMapKeys.value, key]
  activeMapKey.value = key
}
function toggleAllMaps() {
  selectedMapKeys.value = allVisibleSelected.value ? [] : visibleMaps.value.map(mapKey)
  if (selectedMapKeys.value.length) activeMapKey.value = selectedMapKeys.value[0]
}
function openForm(mode: FormMode) {
  const record = activeMap.value
  if (!activeType.value && mode === 'add') return toast('請先選擇設備類型。', 'error')
  if ((mode === 'edit' || mode === 'copy') && !record) return toast('請先選取故障原因對應資料。', 'error')
  formMode.value = mode
  draft.value = mode === 'add'
    ? { equipmentType: activeEquipmentType.value, breakDownNo: '', description: '' }
    : { equipmentType: record!.equipmentType, breakDownNo: record!.breakDownNo, description: record!.description }
  originalDataStamp.value = record?.dataStamp ?? 0
  message.value = ''
}
function closeForm() { formMode.value = null; draft.value = null }
function saveForm(closeAfter: boolean) {
  const mode = formMode.value
  const form = draft.value
  if (!mode || !form) return
  const equipmentType = form.equipmentType.trim()
  const breakDownNo = form.breakDownNo.trim().toLocaleUpperCase()
  if (!equipmentType || !breakDownNo) return toast('EquipmentType 與 BreakDownNo 為必填。', 'error')
  if (!state.value.equipmentTypes.some((row) => row.equipmentType === equipmentType)) return toast('EquipmentType 不存在於設備類型清單。', 'error')

  if (mode === 'add' || mode === 'copy') {
    if (state.value.records.some((row) => row.equipmentType === equipmentType && row.breakDownNo.toLocaleUpperCase() === breakDownNo)) return toast('此設備類型已對應該故障原因。', 'error')
    state.value.records.push({ equipmentType, breakDownNo, description: form.description, creator: currentUser, createDate: nowLabel.value, reviser: '', reviseDate: '', dataStamp: 0 })
    activeEquipmentType.value = equipmentType
    activeMapKey.value = JSON.stringify([equipmentType, breakDownNo])
    selectedMapKeys.value = []
    toast(mode === 'copy' ? '故障原因對應已複製。' : '故障原因對應已新增。')
  } else {
    const row = state.value.records.find((item) => item.equipmentType === equipmentType && item.breakDownNo === form.breakDownNo)
    if (!row) return toast('對應資料已不存在，請重新查詢。', 'error')
    if (row.dataStamp !== originalDataStamp.value) return toast('資料版本已變更，請重新載入後再修改。', 'error')
    // The legacy service treats an empty Description as “not supplied”.
    if (form.description) row.description = form.description
    row.reviser = currentUser
    row.reviseDate = nowLabel.value
    row.dataStamp += 1
    originalDataStamp.value = row.dataStamp
    toast('故障原因對應已儲存。')
  }
  if (closeAfter) closeForm()
}
function deleteMaps() {
  const targets = actionTargets.value
  if (!targets.length || !window.confirm(`確定刪除 ${targets.length} 筆設備與故障原因對應？`)) return
  const targetSet = new Set(targets)
  const before = state.value.records.length
  state.value.records = state.value.records.filter((row) => !targetSet.has(mapKey(row)))
  const deleted = before - state.value.records.length
  activeMapKey.value = ''
  selectedMapKeys.value = []
  toast(`已刪除 ${deleted} 筆對應。故障登錄引用限制仍待資料庫確認。`)
}
</script>

<template>
  <section class="map-page" aria-label="設備與故障原因對應 DEV 預覽">
    <header class="page-heading">
      <div>
        <div class="eyebrow">PM403 · 設備維修</div>
        <h1>設備與故障原因對應</h1>
        <p>選擇設備類型後維護可選用的故障原因。</p>
      </div>
      <div class="heading-actions">
        <span class="dev-badge">DEV 假資料</span>
        <button type="button" class="secondary-button" @click="emit('close')">關閉</button>
      </div>
    </header>

    <p class="notice">資料儲存在本機瀏覽器；故障原因名稱會讀取 PM401 DEV 資料。</p>

    <section class="panel equipment-panel">
      <div class="panel-heading">
        <div><h2>Equipment Type</h2><span>{{ filteredEquipmentTypes.length }} 筆設備類型</span></div>
        <label class="search-field">搜尋設備類型<input v-model="equipmentTypeSearch" type="search" placeholder="EquipmentType"></label>
      </div>
      <div class="table-scroll equipment-scroll">
        <table>
          <thead><tr><th>EquipmentType</th><th>Capacity</th><th>PictureName</th><th>Description</th><th>Creator</th><th>CreateDate</th></tr></thead>
          <tbody>
            <tr v-for="row in filteredEquipmentTypes" :key="row.equipmentType" :class="{ active: row.equipmentType === activeEquipmentType }" @click="selectEquipmentType(row.equipmentType)">
              <td class="key-cell">{{ row.equipmentType }}</td><td>{{ row.capacity }}</td><td>{{ row.pictureName || '—' }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td>
            </tr>
            <tr v-if="filteredEquipmentTypes.length === 0"><td colspan="6" class="empty-cell">沒有符合的設備類型。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="panel map-panel">
      <div class="panel-heading map-heading">
        <div><h2>Break Down Mapping</h2><span>{{ activeType?.equipmentType ?? '尚未選取設備類型' }} · {{ visibleMaps.length }} 筆對應</span></div>
        <div class="toolbar">
          <button type="button" class="primary-button" :disabled="!activeType" @click="openForm('add')">新增</button>
          <button type="button" :disabled="!activeMap" @click="openForm('edit')">修改</button>
          <button type="button" :disabled="actionTargets.length === 0" @click="deleteMaps">刪除</button>
          <button type="button" :disabled="!activeMap" @click="openForm('copy')">複製</button>
        </div>
      </div>
      <div class="table-scroll map-scroll">
        <table>
          <thead><tr><th class="select-column"><input type="checkbox" aria-label="全選對應" :checked="allVisibleSelected" :disabled="visibleMaps.length === 0" @change="toggleAllMaps"></th><th>EquipmentType</th><th>BreakDownNo</th><th>BreakDownCause</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleMaps" :key="mapKey(row)" :class="{ active: mapKey(row) === activeMapKey, selected: selectedMapKeys.includes(mapKey(row)) }" @click="selectMap(row)">
              <td class="select-column" @click.stop><input type="checkbox" :checked="selectedMapKeys.includes(mapKey(row))" :aria-label="`選取 ${row.breakDownNo}`" @change="toggleMap(row)"></td>
              <td>{{ row.equipmentType }}</td><td class="key-cell">{{ row.breakDownNo }}</td><td>{{ breakdownNameByNo.get(row.breakDownNo.toLocaleUpperCase()) || '（未找到故障原因主檔）' }}</td><td class="description-cell">{{ row.description || '—' }}</td><td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.reviser || '—' }}</td><td>{{ row.reviseDate || '—' }}</td>
            </tr>
            <tr v-if="visibleMaps.length === 0"><td colspan="9" class="empty-cell">此設備類型尚未設定故障原因對應。</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="message" class="status-message" :class="messageKind" role="status">{{ message }}</p>
    </section>

    <div v-if="formMode && draft" class="dialog-backdrop" @click.self="closeForm">
      <section class="edit-dialog" role="dialog" aria-modal="true" :aria-label="formMode === 'edit' ? '修改故障原因對應' : formMode === 'copy' ? '複製故障原因對應' : '新增故障原因對應'">
        <header><div><div class="eyebrow">{{ formMode === 'edit' ? 'EDIT' : formMode === 'copy' ? 'COPY' : 'ADD' }}</div><h2>EQP Type &amp; Break Down Mapping</h2></div><button type="button" class="icon-button" aria-label="關閉" @click="closeForm">×</button></header>
        <p v-if="message" class="dialog-message" :class="messageKind" role="status">{{ message }}</p>
        <label>EquipmentType<input v-model="draft.equipmentType" readonly></label>
        <label>BreakDownNo
          <input v-model="draft.breakDownNo" list="breakdown-options" :readonly="formMode === 'edit'" @blur="draft.breakDownNo = draft.breakDownNo.toLocaleUpperCase()">
          <datalist id="breakdown-options"><option v-for="choice in breakdownChoices" :key="choice.breakDownNo" :value="choice.breakDownNo" :label="`${choice.breakDownCause}${choice.issueState === undefined ? '' : ` · 狀態 ${choice.issueState}`}`"></option></datalist>
          <small>可從 PM401 清單選取，或輸入故障原因代碼。</small>
        </label>
        <label>Description<textarea v-model="draft.description" rows="4"></textarea></label>
        <footer><button type="button" @click="closeForm">取消</button><button type="button" @click="saveForm(false)">套用</button><button type="button" class="primary-button" @click="saveForm(true)">確定</button></footer>
      </section>
    </div>
  </section>
</template>

<style scoped>
.map-page { display:flex; flex-direction:column; gap:12px; min-height:0; height:100%; box-sizing:border-box; padding:16px; overflow:auto; background:#f6f4ed; color:#26251f; font:inherit; }
.page-heading,.panel-heading,.heading-actions,.toolbar,.edit-dialog header,.edit-dialog footer { display:flex; align-items:center; justify-content:space-between; gap:10px; }
.page-heading h1 { margin:2px 0 3px; font-size:20px; font-weight:700; }
.page-heading p { margin:0; color:#716e63; font-size:12px; }
.eyebrow { color:#777366; font-size:10px; letter-spacing:.1em; font-weight:700; }
.dev-badge { padding:4px 8px; border:1px solid #c3a56c; border-radius:12px; background:#fff8e8; color:#76571d; font-size:11px; font-weight:700; }
.notice { margin:0; padding:7px 10px; border-left:3px solid #b99b61; background:#fff9ed; color:#6f5a31; font-size:11px; }
.panel { min-width:0; border:1px solid #cfccc1; background:#fff; box-shadow:0 1px 2px #28251c0c; }
.panel-heading { min-height:48px; padding:8px 12px; border-bottom:1px solid #e2dfd5; }
.panel-heading h2 { display:inline; margin:0 8px 0 0; font-size:14px; }
.panel-heading span { color:#777366; font-size:11px; }
.search-field { display:flex; align-items:center; gap:8px; color:#666255; font-size:11px; }
input,textarea,button { font:inherit; }
.search-field input { width:190px; padding:6px 8px; border:1px solid #c9c6bb; border-radius:3px; }
.table-scroll { overflow:auto; }
.equipment-scroll { max-height:222px; }
.map-scroll { max-height:min(42vh,430px); }
table { width:100%; border-collapse:collapse; font-size:11px; white-space:nowrap; }
th { position:sticky; top:0; z-index:1; padding:7px 8px; background:#eeece4; color:#575449; text-align:left; font-weight:700; }
td { padding:7px 8px; border-top:1px solid #eceae3; color:#39382f; }
tbody tr { cursor:pointer; }
tbody tr:hover { background:#f7f6f1; }
tbody tr.active { background:#e8f1ef; }
tbody tr.selected { box-shadow:inset 3px 0 #6b9188; }
.key-cell { color:#355c57; font-weight:700; }
.description-cell { max-width:250px; overflow:hidden; text-overflow:ellipsis; }
.empty-cell { padding:18px; text-align:center; color:#888477; }
.select-column { width:28px; text-align:center; }
.toolbar button,.secondary-button,.edit-dialog footer button { min-height:28px; padding:4px 10px; border:1px solid #aaa69a; border-radius:3px; background:#fff; color:#36352d; cursor:pointer; }
button:disabled { opacity:.45; cursor:not-allowed; }
.toolbar .primary-button,.primary-button { border-color:#365b55; background:#365b55; color:white; }
.status-message,.dialog-message { margin:0; padding:7px 10px; border-radius:3px; font-size:11px; }
.status-message.success,.dialog-message.success { background:#edf6ef; color:#2d673e; }
.status-message.error,.dialog-message.error { background:#fff0ed; color:#9a3d2c; }
.dialog-backdrop { position:fixed; inset:0; z-index:30; display:grid; place-items:center; padding:16px; background:#15140f77; }
.edit-dialog { display:flex; flex-direction:column; gap:14px; width:min(520px,100%); box-sizing:border-box; padding:18px; border:1px solid #c9c5b8; border-radius:5px; background:#fcfbf7; box-shadow:0 12px 40px #0003; }
.edit-dialog header { align-items:flex-start; }
.edit-dialog h2 { margin:3px 0 0; font-size:16px; }
.icon-button { border:0; background:transparent; color:#555247; font-size:24px; cursor:pointer; }
.edit-dialog label { display:flex; flex-direction:column; gap:5px; color:#575449; font-size:11px; font-weight:700; }
.edit-dialog input,.edit-dialog textarea { width:100%; box-sizing:border-box; padding:8px; border:1px solid #c9c6bb; border-radius:3px; background:#fff; color:#292820; font-weight:400; }
.edit-dialog input[readonly] { background:#eeece4; }
.edit-dialog small { color:#777366; font-weight:400; }
.edit-dialog footer { justify-content:flex-end; padding-top:4px; border-top:1px solid #e2dfd5; }
@media (max-width:760px) { .map-page { padding:10px; } .page-heading,.panel-heading { align-items:flex-start; flex-direction:column; } .heading-actions,.toolbar { width:100%; justify-content:flex-start; flex-wrap:wrap; } .search-field { width:100%; } .search-field input { flex:1; } }
</style>
