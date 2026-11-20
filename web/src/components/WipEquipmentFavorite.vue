<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface EquipmentOption { equipmentNo: string; equipmentType: string; equipmentName: string; chamberFlag: number; issueState: number }
interface EquipmentFavoriteOptions { equipmentTypes: string[]; equipment: EquipmentOption[] }
interface FavoritePage { pageNo: string; equipmentNos: string[] }
interface PageSummary { pageNo: string }
type EditorMode = 'add' | 'edit' | 'copy'

const props = withDefaults(defineProps<{
  apiBaseUrl: string
  developmentPreview?: boolean
}>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const mockMode = computed(() => import.meta.env.DEV && props.developmentPreview)
const pagesKey = 'ymes-dev-mock:wip-eqp-favorite:pages:v1'
const equipmentKey = 'ymes-dev-mock:wip-eqp-favorite:equipment:v1'
const seedEquipment: EquipmentOption[] = [
  { equipmentNo: 'EQ-DEV-01', equipmentType: 'ASSY', equipmentName: '組裝機台 01', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'EQ-DEV-02', equipmentType: 'ASSY', equipmentName: '組裝機台 02', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'EQ-DEV-03', equipmentType: 'ASSY', equipmentName: '組裝機台 03', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'EQ-DEV-04', equipmentType: 'TEST', equipmentName: '測試機台 01', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'EQ-DEV-05', equipmentType: 'TEST', equipmentName: '測試機台 02', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'EQ-DEV-06', equipmentType: 'ASSY', equipmentName: '組裝機台 01 子設備', chamberFlag: 1, issueState: 2 },
  { equipmentNo: 'EQ-DEV-07', equipmentType: 'PACK', equipmentName: '包裝機台 01', chamberFlag: 0, issueState: 2 },
]
const seedPages: FavoritePage[] = [
  { pageNo: 'LOT_EXECUTION_A', equipmentNos: ['EQ-DEV-01', 'EQ-DEV-02'] },
  { pageNo: 'NIGHT_SHIFT', equipmentNos: ['EQ-DEV-04'] },
]

const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const pages = ref<PageSummary[]>([])
const equipmentOptions = ref<EquipmentOption[]>([])
const equipmentTypeOptions = ref<string[]>([])
const selectedPageNo = ref('')
const selectedPageEquipmentNos = ref<string[]>([])
const checkedPageNos = ref<string[]>([])
const editorMode = ref<EditorMode | null>(null)
const editorPageNo = ref('')
const editorOriginalPageNo = ref('')
const editorCheckedEquipmentNos = ref<string[]>([])
const editorSelectedEquipmentNos = ref<string[]>([])
const editorRemoveNos = ref<string[]>([])
const equipmentType = ref('')
const replaceConfirmOpen = ref(false)
const deleteConfirmOpen = ref(false)
const pendingSaveClose = ref(false)

const equipmentTypes = computed(() => [...equipmentTypeOptions.value].sort())
const editorEquipmentRows = computed(() => equipmentOptions.value
  .filter(row => row.issueState === 2 && row.chamberFlag === 0 && row.equipmentType === equipmentType.value)
  .sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo)))
const selectedEquipmentRows = computed(() => editorSelectedEquipmentNos.value
  .map(no => equipmentOptions.value.find(row => row.equipmentNo === no))
  .filter((row): row is EquipmentOption => Boolean(row)))
const activePage = computed(() => pages.value.find(row => row.pageNo === selectedPageNo.value) ?? null)
const editorTitle = computed(() => editorMode.value === 'add' ? 'Equipment Favorite (ADD)' : editorMode.value === 'copy' ? 'Equipment Favorite (COPY)' : 'Equipment Favorite (EDIT)')

function readStorage<T>(key: string, fallback: T): T {
  try { const raw = window.localStorage.getItem(key); return raw ? JSON.parse(raw) as T : structuredClone(fallback) }
  catch { return structuredClone(fallback) }
}
function writeStorage<T>(key: string, value: T): void { window.localStorage.setItem(key, JSON.stringify(value)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...(init.headers ?? {}) } })
  if (response.status === 401) { window.location.assign(appBase + '/'); throw new Error('登入狀態已逾期，請重新登入。') }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})。`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function getMockPages(): FavoritePage[] { return readStorage(pagesKey, seedPages) }
function getMockEquipment(): EquipmentOption[] { return readStorage(equipmentKey, seedEquipment) }
async function loadMain(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    if (mockMode.value) {
      equipmentOptions.value = getMockEquipment()
      equipmentTypeOptions.value = ['ASSY', 'PACK', 'TEST']
      const storedPages = getMockPages()
      writeStorage(pagesKey, storedPages)
      pages.value = storedPages.map(row => ({ pageNo: row.pageNo })).sort((a, b) => a.pageNo.localeCompare(b.pageNo))
    } else {
      const [pageRows, optionRows] = await Promise.all([
        requestJson<PageSummary[]>('api/wip/equipment-favorites'),
        requestJson<EquipmentFavoriteOptions>('api/wip/equipment-favorites/options?issueState=2'),
      ])
      pages.value = [...pageRows].sort((a, b) => a.pageNo.localeCompare(b.pageNo))
      equipmentTypeOptions.value = optionRows.equipmentTypes
      equipmentOptions.value = optionRows.equipment
    }
    if (selectedPageNo.value && pages.value.some(row => row.pageNo === selectedPageNo.value)) await selectPage(selectedPageNo.value)
    else if (pages.value.length) await selectPage(pages.value[0].pageNo)
    else { selectedPageNo.value = ''; selectedPageEquipmentNos.value = [] }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '設備收藏資料載入失敗。' }
  finally { loading.value = false }
}
async function selectPage(pageNo: string): Promise<void> {
  selectedPageNo.value = pageNo
  errorMessage.value = ''
  try {
    if (mockMode.value) selectedPageEquipmentNos.value = getMockPages().find(row => row.pageNo === pageNo)?.equipmentNos ?? []
    else {
      const row = await requestJson<FavoritePage>(`api/wip/equipment-favorites/${encodeURIComponent(pageNo)}`)
      selectedPageEquipmentNos.value = row.equipmentNos
    }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '設備收藏明細載入失敗。'; selectedPageEquipmentNos.value = [] }
}
function togglePage(pageNo: string, checked: boolean): void {
  checkedPageNos.value = checked ? [...new Set([...checkedPageNos.value, pageNo])] : checkedPageNos.value.filter(value => value !== pageNo)
}
function openEditor(mode: EditorMode): void {
  errorMessage.value = ''
  editorMode.value = mode
  const current = mode === 'add' ? null : { pageNo: selectedPageNo.value, equipmentNos: [...selectedPageEquipmentNos.value] }
  editorOriginalPageNo.value = mode === 'edit' ? selectedPageNo.value : ''
  editorPageNo.value = mode === 'add' ? '' : selectedPageNo.value
  const preselected = current?.equipmentNos ?? []
  editorSelectedEquipmentNos.value = [...preselected]
  editorCheckedEquipmentNos.value = [...preselected]
  editorRemoveNos.value = []
  equipmentType.value = equipmentTypes.value[0] ?? ''
}
function closeEditor(): void { editorMode.value = null; replaceConfirmOpen.value = false }
function toggleEditorEquipment(equipmentNo: string, checked: boolean): void {
  editorCheckedEquipmentNos.value = checked
    ? [...new Set([...editorCheckedEquipmentNos.value, equipmentNo])]
    : editorCheckedEquipmentNos.value.filter(value => value !== equipmentNo)
}
function moveEquipmentToSelected(): void {
  const checked = equipmentOptions.value.filter(row => editorCheckedEquipmentNos.value.includes(row.equipmentNo) && row.issueState === 2 && row.chamberFlag === 0).map(row => row.equipmentNo)
  if (!checked.length) { errorMessage.value = '請先勾選設備。'; return }
  if (checked.length > 15) { errorMessage.value = '設備數量不可超過 15 台。'; return }
  editorSelectedEquipmentNos.value = [...checked].sort((a, b) => a.localeCompare(b))
  errorMessage.value = ''
}
function removeSelectedEquipment(): void {
  let removeNos = editorRemoveNos.value
  if (!removeNos.length && editorSelectedEquipmentNos.value.length) removeNos = [editorSelectedEquipmentNos.value[0]]
  editorSelectedEquipmentNos.value = editorSelectedEquipmentNos.value.filter(no => !removeNos.includes(no))
  editorCheckedEquipmentNos.value = editorCheckedEquipmentNos.value.filter(no => !removeNos.includes(no))
  editorRemoveNos.value = []
}
async function saveEditor(closeAfterSave: boolean, confirmedReplace = false): Promise<void> {
  if (!editorMode.value) return
  const pageNo = editorPageNo.value.trim()
  if (!pageNo) { errorMessage.value = 'PageNo 不可空白。'; return }
  if (!editorSelectedEquipmentNos.value.length) { errorMessage.value = '請至少選擇一台設備。'; return }
  if (editorSelectedEquipmentNos.value.length > 15) { errorMessage.value = '設備數量不可超過 15 台。'; return }
  const duplicate = pages.value.some(row => row.pageNo === pageNo && row.pageNo !== editorOriginalPageNo.value)
  if (duplicate && editorMode.value !== 'edit' && !confirmedReplace) { pendingSaveClose.value = closeAfterSave; replaceConfirmOpen.value = true; return }
  saving.value = true
  errorMessage.value = ''
  const equipmentNos = [...editorSelectedEquipmentNos.value]
  try {
    if (mockMode.value) {
      const stored = getMockPages()
      const next = stored.filter(row => row.pageNo !== pageNo)
      next.push({ pageNo, equipmentNos })
      writeStorage(pagesKey, next)
    } else if (editorMode.value === 'edit') {
      await requestJson(`api/wip/equipment-favorites/${encodeURIComponent(pageNo)}`, { method: 'PUT', body: JSON.stringify({ equipmentNos }) })
    } else {
      await requestJson('api/wip/equipment-favorites', { method: 'POST', body: JSON.stringify({ pageNo, equipmentNos, replaceExisting: confirmedReplace }) })
    }
    selectedPageNo.value = pageNo
    await loadMain()
    if (closeAfterSave) closeEditor()
    else { editorOriginalPageNo.value = editorMode.value === 'edit' ? pageNo : ''; editorPageNo.value = pageNo }
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '收藏頁儲存失敗。' }
  finally { saving.value = false; replaceConfirmOpen.value = false }
}
async function confirmReplace(): Promise<void> { await saveEditor(pendingSaveClose.value, true) }
async function deleteSelectedPages(): Promise<void> {
  const targets = checkedPageNos.value.length ? [...checkedPageNos.value] : activePage.value ? [activePage.value.pageNo] : []
  if (!targets.length) { errorMessage.value = '請先選取要刪除的 PageNo。'; deleteConfirmOpen.value = false; return }
  loading.value = true
  errorMessage.value = ''
  const deleted: string[] = []
  let failureMessage = ''
  try {
    for (const pageNo of targets) {
      try {
        if (mockMode.value) writeStorage(pagesKey, getMockPages().filter(row => row.pageNo !== pageNo))
        else await requestJson(`api/wip/equipment-favorites/${encodeURIComponent(pageNo)}`, { method: 'DELETE' })
        deleted.push(pageNo)
      } catch (error) { failureMessage = error instanceof Error ? error.message : '刪除失敗。'; break }
    }
    if (deleted.includes(selectedPageNo.value)) selectedPageNo.value = ''
    checkedPageNos.value = checkedPageNos.value.filter(pageNo => !deleted.includes(pageNo))
    deleteConfirmOpen.value = false
    await loadMain()
    if (failureMessage) errorMessage.value = failureMessage + '先前已成功刪除的頁面不會自動復原。'
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '刪除失敗。' }
  finally { loading.value = false }
}

onMounted(() => { void loadMain() })
</script>

<template>
  <section class="favorite-page">
    <header class="page-heading"><h1>過帳設備設定</h1><span v-if="mockMode" class="dev-badge">DEV 假資料</span></header>
    <div class="favorite-shell">
      <div class="favorite-grids">
        <fieldset class="favorite-pane page-pane"><legend class="sr-only">Equipment Favorite Pages</legend>
          <div class="favorite-table"><table><thead><tr><th class="check-col"></th><th>PageNo</th></tr></thead><tbody>
            <tr v-for="row in pages" :key="row.pageNo" :class="{ active: selectedPageNo === row.pageNo }" @click="selectPage(row.pageNo)">
              <td class="check-col"><input type="checkbox" :checked="checkedPageNos.includes(row.pageNo)" @click.stop @change="togglePage(row.pageNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.pageNo }}</td>
            </tr><tr v-if="!pages.length"><td colspan="2" class="empty">沒有收藏頁面</td></tr>
          </tbody></table></div>
        </fieldset>
        <fieldset class="favorite-pane equipment-pane"><legend class="sr-only">Favorite Equipment</legend>
          <div class="favorite-table"><table><thead><tr><th>EquipmentNo</th></tr></thead><tbody>
            <tr v-for="no in selectedPageEquipmentNos" :key="no"><td>{{ no }}</td></tr>
            <tr v-if="!selectedPageEquipmentNos.length"><td class="empty">請選取左側 PageNo</td></tr>
          </tbody></table></div>
        </fieldset>
        <aside class="favorite-actions"><button type="button" class="win-btn primary" @click="openEditor('add')">ADD (&amp;A)</button><button type="button" class="win-btn" :disabled="!activePage" @click="openEditor('edit')">EDIT (&amp;E)</button><button type="button" class="win-btn danger" :disabled="!checkedPageNos.length && !activePage" @click="deleteConfirmOpen = true">DEL (&amp;D)</button><button type="button" class="win-btn" :disabled="!activePage" @click="openEditor('copy')">Copy (&amp;C)</button><button type="button" class="win-btn close-button" @click="emit('close')">Close (&amp;X)</button></aside>
      </div>
      <p v-if="errorMessage && !editorMode" class="message error">{{ errorMessage }}</p>
      <div v-if="loading" class="loading-mask">載入中…</div>
    </div>

    <div v-if="editorMode" class="backdrop" @click.self="closeEditor">
      <section class="dialog favorite-editor">
        <header class="dialog-title"><h2>{{ editorTitle }}</h2><button type="button" class="x-button" @click="closeEditor">×</button></header>
        <div class="editor-body">
          <fieldset class="type-pane"><legend class="sr-only">Equipment Type</legend><label class="page-no-field"><span>Page No</span><input v-model="editorPageNo" class="kd-input" :readonly="editorMode === 'edit'" maxlength="50"></label><div class="favorite-table"><table><thead><tr><th>EquipmentType</th></tr></thead><tbody><tr v-for="type in equipmentTypes" :key="type" :class="{ active: equipmentType === type }" @click="equipmentType = type"><td>{{ type }}</td></tr><tr v-if="!equipmentTypes.length"><td class="empty">沒有核准設備類別</td></tr></tbody></table></div></fieldset>
          <fieldset class="selection-pane"><legend class="sr-only">設備選擇（最多 15 台）</legend>
            <div class="favorite-table available-table"><table><thead><tr><th class="check-col">Check</th><th>EquipmentNo</th></tr></thead><tbody><tr v-for="row in editorEquipmentRows" :key="row.equipmentNo"><td class="check-col"><input type="checkbox" :checked="editorCheckedEquipmentNos.includes(row.equipmentNo)" @change="toggleEditorEquipment(row.equipmentNo, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.equipmentNo }}</td></tr><tr v-if="!editorEquipmentRows.length"><td colspan="2" class="empty">此類別沒有可用的主設備</td></tr></tbody></table></div>
            <div class="move-buttons"><button type="button" class="win-btn" title="加入已選設備" @click="moveEquipmentToSelected">▽</button><button type="button" class="win-btn" title="移除已選設備" @click="removeSelectedEquipment">△</button></div>
            <div class="favorite-table selected-table"><table><thead><tr><th class="check-col">Check</th><th>EquipmentNo</th></tr></thead><tbody><tr v-for="row in selectedEquipmentRows" :key="row.equipmentNo"><td class="check-col"><input v-model="editorRemoveNos" type="checkbox" :value="row.equipmentNo"></td><td>{{ row.equipmentNo }}</td></tr><tr v-if="!selectedEquipmentRows.length"><td colspan="2" class="empty">尚未選擇設備</td></tr></tbody></table></div>
          </fieldset>
        </div>
        <p v-if="errorMessage" class="message error">{{ errorMessage }}</p>
        <footer class="dialog-actions"><button type="button" class="win-btn primary" :disabled="saving" @click="saveEditor(true)">Confirm</button><button type="button" class="win-btn" :disabled="saving" @click="closeEditor">Close</button><button type="button" class="win-btn" :disabled="saving" @click="saveEditor(false)">Apply</button></footer>
      </section>
    </div>
    <div v-if="replaceConfirmOpen" class="backdrop nested" @click.self="replaceConfirmOpen = false"><section class="dialog confirm-dialog"><header class="dialog-title"><h2>Replace Favorite Page?</h2><button type="button" class="x-button" @click="replaceConfirmOpen = false">×</button></header><p>PageNo「{{ editorPageNo }}」已存在。確定要以目前設備清單取代？</p><footer class="dialog-actions"><button type="button" class="win-btn primary" :disabled="saving" @click="confirmReplace">Yes</button><button type="button" class="win-btn" @click="replaceConfirmOpen = false">No</button></footer></section></div>
    <div v-if="deleteConfirmOpen" class="backdrop nested" @click.self="deleteConfirmOpen = false"><section class="dialog confirm-dialog"><header class="dialog-title"><h2>Delete Favorite Page?</h2><button type="button" class="x-button" @click="deleteConfirmOpen = false">×</button></header><p>確定刪除 {{ checkedPageNos.length || 1 }} 個收藏頁面？</p><footer class="dialog-actions"><button type="button" class="win-btn danger" :disabled="loading" @click="deleteSelectedPages">Yes</button><button type="button" class="win-btn" @click="deleteConfirmOpen = false">No</button></footer></section></div>
  </section>
</template>

<style scoped>
.favorite-page{height:100%;min-height:0;display:flex;flex-direction:column;background:#f7f5ef;color:#333}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}.page-heading{display:flex;align-items:center;gap:12px;padding:10px 18px;border-bottom:1px solid #ddd}.page-heading h1{font-size:20px;margin:0;font-weight:600}.dev-badge{font-size:12px;color:#7c5d21;background:#fff0c2;padding:3px 8px;border:1px solid #e3c66f;border-radius:3px}.favorite-shell{position:relative;display:flex;flex-direction:column;flex:1;min-height:0;margin:8px;padding:8px;border:1px solid #bbb;background:#fff}.favorite-grids{display:grid;grid-template-columns:minmax(260px,0.9fr) minmax(320px,1.1fr) 100px;gap:8px;flex:1;min-height:0}.favorite-pane,.type-pane,.selection-pane{min-width:0;min-height:0;border:1px solid #aaa;padding:7px}.favorite-pane legend,.type-pane legend,.selection-pane legend{padding:0 5px;color:#555}.favorite-table{min-height:0;overflow:auto;border:1px solid #aaa;background:#fff}.favorite-table table{width:100%;border-collapse:collapse;font-size:13px}.favorite-table th{position:sticky;top:0;background:#3f9fdf;color:#fff;text-align:left;padding:5px 7px;white-space:nowrap}.favorite-table td{padding:5px 7px;border:1px solid #d6d6d6;white-space:nowrap}.favorite-table tbody tr{cursor:pointer}.favorite-table tbody tr:hover{background:#fff2c5}.favorite-table tbody tr.active{background:#ffe59a}.check-col{width:30px;text-align:center}.check-col input{accent-color:#e7a137}.empty{text-align:center;color:#888;padding:14px!important}.favorite-actions{display:flex;flex-direction:column;gap:7px}.favorite-actions .win-btn{width:100%;min-height:34px}.close-button{margin-top:auto}.win-btn{border:1px solid #c6c6c6;background:linear-gradient(#fff,#f3f3f3);color:#333;border-radius:3px;padding:5px 7px;cursor:pointer}.win-btn:hover:not(:disabled){background:#fff0c2;border-color:#d4aa53}.win-btn:disabled{opacity:.5;cursor:not-allowed}.win-btn.primary{background:#f6a72e;border-color:#dc901f;color:#fff}.win-btn.danger{color:#9b3434}.message{margin:6px 0 0;padding:6px 8px;border:1px solid #e1d3a7;background:#fff5d1}.message.error{color:#8c2a22;border-color:#d79b8b;background:#fff0ec}.loading-mask{position:absolute;inset:0;display:grid;place-items:center;background:#ffffffb8;color:#555}.favorite-editor{width:min(804px,96vw);height:min(566px,94vh);padding:0 9px 7px}.favorite-editor>.dialog-title{margin:0 -9px}.editor-body{display:grid;grid-template-columns:minmax(235px,0.95fr) minmax(340px,1.05fr);gap:7px;flex:1;min-height:0;margin-top:5px}.type-pane,.selection-pane{display:flex;flex-direction:column}.type-pane .favorite-table{flex:1}.selection-pane{gap:5px}.available-table{flex:1}.selected-table{height:34%;flex:none}.move-buttons{display:flex;justify-content:center;gap:7px}.move-buttons .win-btn{min-width:52px;height:26px;padding:1px}.page-no-field{display:grid;grid-template-columns:88px minmax(0,280px);align-items:center;gap:7px;margin:7px 0}.page-no-field>span{text-align:right}.page-no-field input{width:100%;box-sizing:border-box}.favorite-editor>.message{flex:none}.favorite-editor>.dialog-actions{flex:none}.confirm-dialog{width:min(420px,94vw);padding:0 10px 7px}.confirm-dialog>.dialog-title{margin:0 -10px}.confirm-dialog p{padding:8px 2px}.confirm-dialog>.dialog-actions{flex:none}.favorite-page :deep(.backdrop.nested){z-index:20}
@media(max-width:850px){.favorite-grids{grid-template-columns:minmax(0,1fr) 100px}.equipment-pane{grid-column:1}.favorite-actions{grid-column:2;grid-row:1 / span 2}.editor-body{grid-template-columns:1fr 1.2fr}.favorite-page{overflow:auto}}
</style>
