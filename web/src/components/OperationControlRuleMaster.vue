<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type RuleType = 0 | 1
type FormMode = 'new' | 'edit' | 'copy' | ''
interface RuleRow {
  ruleNo: string
  ruleName: string
  ruleScript: string
  description: string
  creator: string
  createDate: string
  holdDispGroup: string
  sendMail: 0 | 1
  ruleClass: number
  ruleType: RuleType
  ruleMode: number | null
  ruleModeQty: number | null
  dataStamp: number | null
  reviser: string
  reviseDate: string
}
interface RuleDraft { ruleNo: string; ruleName: string; ruleScript: string; description: string; holdDispGroup: string; sendMail: 0 | 1; ruleType: RuleType }
interface ApiEnvelope<T> { items?: T[]; rows?: T[]; data?: T[]; message?: string; title?: string }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes.dev.op-control-rule-master.v1'
const seedRows: RuleRow[] = [
  { ruleNo: 'DEV-LOT-HOLD', ruleName: 'DEV 批次異常暫停', ruleScript: 'IF lot.status = "HOLD" THEN\n  RETURN false\nEND', description: 'DEV 假資料：批次狀態檢核', creator: 'DEV001', createDate: '2026-11-03T08:30:00', holdDispGroup: 'DEV-HOLD-ENG', sendMail: 1, ruleClass: 0, ruleType: 0, ruleMode: 0, ruleModeQty: null, dataStamp: 2, reviser: 'DEV002', reviseDate: '2026-11-07T10:15:00' },
  { ruleNo: 'DEV-COMP-CHECK', ruleName: 'DEV 元件投入檢查', ruleScript: 'RETURN component.qty > 0', description: 'DEV 假資料：元件投入數量須大於零', creator: 'DEV001', createDate: '2026-11-04T09:10:00', holdDispGroup: '', sendMail: 0, ruleClass: 0, ruleType: 1, ruleMode: 0, ruleModeQty: null, dataStamp: 0, reviser: '', reviseDate: '' },
  { ruleNo: 'DEV-CP-EXAMPLE', ruleName: 'DEV 行業包範例', ruleScript: 'RETURN true', description: 'DEV 假資料：此筆不屬標準版清單', creator: 'DEV001', createDate: '2026-11-05T10:00:00', holdDispGroup: '', sendMail: 0, ruleClass: 1, ruleType: 1, ruleMode: 0, ruleModeQty: null, dataStamp: 0, reviser: '', reviseDate: '' },
]
const rows = ref<RuleRow[]>([])
const activeRuleNo = ref('')
const selectedRuleNos = ref<string[]>([])
const keyword = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const dialogMode = ref<FormMode>('')
const editing = ref<RuleRow | null>(null)
const draft = ref<RuleDraft>(emptyDraft())
const scriptEditorOpen = ref(false)
const scriptDraft = ref('')
const holdGroupPickerOpen = ref(false)
const holdGroups = ref<string[]>([])
const selectedRow = computed(() => rows.value.find((row) => row.ruleNo === activeRuleNo.value) ?? null)
const selectedRows = computed(() => rows.value.filter((row) => selectedRuleNos.value.includes(row.ruleNo)))
const visibleRows = computed(() => rows.value.filter((row) => row.ruleClass === 0 && (!keyword.value.trim() || row.ruleNo.toLocaleUpperCase().includes(keyword.value.trim().toLocaleUpperCase()))))
const dialogOpen = computed(() => dialogMode.value !== '')

function emptyDraft(): RuleDraft { return { ruleNo: '', ruleName: '', ruleScript: '', description: '', holdDispGroup: '', sendMail: 0, ruleType: 1 } }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function displayDate(value: string): string {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date).replaceAll('-', '/')
}
function persistMock(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function loadMock(): void {
  try {
    const saved = localStorage.getItem(storageKey)
    rows.value = saved ? JSON.parse(saved) as RuleRow[] : clone(seedRows)
    if (!saved) persistMock()
  } catch { rows.value = clone(seedRows) }
}
function apiUrl(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers: { 'Content-Type': 'application/json', ...init.headers } })
  const raw = await response.text()
  let body: unknown = null
  try { body = raw ? JSON.parse(raw) : null } catch { body = raw }
  if (!response.ok) {
    const envelope = body as ApiEnvelope<unknown> | null
    throw new Error(envelope && typeof envelope === 'object' ? envelope.message ?? envelope.title ?? 'API 回應 ' + response.status : String(body || 'API 回應 ' + response.status))
  }
  return body as T
}
function normalizeRow(row: Record<string, unknown>): RuleRow {
  const value = (camel: string, upper: string): unknown => row[camel] ?? row[upper] ?? row[upper.toLocaleLowerCase()]
  const dataStamp = value('dataStamp', 'DATASTAMP')
  const ruleModeQty = value('ruleModeQty', 'RULEMODEQTY')
  return {
    ruleNo: String(value('ruleNo', 'RULENO') ?? ''), ruleName: String(value('ruleName', 'RULENAME') ?? ''),
    ruleScript: String(value('ruleScript', 'RULESCRIPT') ?? ''), description: String(value('description', 'DESCRIPTION') ?? ''),
    creator: String(value('creator', 'CREATOR') ?? ''), createDate: String(value('createDate', 'CREATEDATE') ?? ''),
    holdDispGroup: String(value('holdDispGroup', 'HOLDDISPGROUP') ?? ''), sendMail: Number(value('sendMail', 'SENDMAIL') ?? 0) === 1 ? 1 : 0,
    ruleClass: Number(value('ruleClass', 'RULECLASS') ?? 0), ruleType: Number(value('ruleType', 'RULETYPE') ?? 1) === 0 ? 0 : 1,
    ruleMode: value('ruleMode', 'RULEMODE') == null ? null : Number(value('ruleMode', 'RULEMODE')),
    ruleModeQty: ruleModeQty == null ? null : Number(ruleModeQty), dataStamp: dataStamp == null ? null : Number(dataStamp),
    reviser: String(value('reviser', 'REVISER') ?? ''), reviseDate: String(value('reviseDate', 'REVISEDATE') ?? ''),
  }
}
async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; activeRuleNo.value = ''; selectedRuleNos.value = []
  try {
    if (mock.value) loadMock()
    else {
      const query = new URLSearchParams({ ruleClass: '0' })
      if (keyword.value.trim()) query.set('ruleNo', keyword.value.trim())
      const result = await api<RuleRow[] | ApiEnvelope<RuleRow>>('/api/operation-control-rules?' + query.toString())
      const list = Array.isArray(result) ? result : result.items ?? result.rows ?? result.data ?? []
      rows.value = list.map((row) => normalizeRow(row as unknown as Record<string, unknown>))
    }
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入作業站法則失敗。' }
  finally { loading.value = false }
}
function selectRow(row: RuleRow, event?: MouseEvent): void {
  const multi = Boolean(event?.ctrlKey || event?.metaKey)
  if (multi) selectedRuleNos.value = selectedRuleNos.value.includes(row.ruleNo) ? selectedRuleNos.value.filter((key) => key !== row.ruleNo) : [...selectedRuleNos.value, row.ruleNo]
  else selectedRuleNos.value = [row.ruleNo]
  activeRuleNo.value = row.ruleNo
}
function openDialog(mode: Exclude<FormMode, ''>, row: RuleRow | null = null): void {
  error.value = ''; notice.value = ''; dialogMode.value = mode; editing.value = row ? clone(row) : null
  draft.value = row ? { ruleNo: row.ruleNo, ruleName: row.ruleName, ruleScript: row.ruleScript, description: row.description, holdDispGroup: row.holdDispGroup, sendMail: row.sendMail, ruleType: row.ruleType } : emptyDraft()
}
function closeDialog(): void { dialogMode.value = ''; editing.value = null; error.value = '' }
function validateDraft(): boolean {
  draft.value.ruleNo = draft.value.ruleNo.trim().toUpperCase()
  if (!draft.value.ruleNo) { error.value = 'Rule No 為必填欄位。'; return false }
  if (draft.value.ruleNo.length > 20) { error.value = 'Rule No 最多 20 個字元。'; return false }
  if (!draft.value.ruleName.trim()) { error.value = 'Rule Name 為必填欄位。'; return false }
  if (draft.value.ruleName.length > 50) { error.value = 'Rule Name 最多 50 個字元。'; return false }
  if (!draft.value.ruleScript.trim()) { error.value = 'Rule Script 為必填欄位。'; return false }
  if (draft.value.ruleScript.length > 3000) { error.value = 'Rule Script 最多 3000 個字元。'; return false }
  if (draft.value.description.length > 255) { error.value = 'Description 最多 255 個字元。'; return false }
  if (draft.value.sendMail === 1 && !draft.value.holdDispGroup) { error.value = 'Send Mail 選 Yes 時，Hold Disp Group 為必填。'; return false }
  return true
}
async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!validateDraft()) return
  busy.value = true; error.value = ''; notice.value = ''
  const mode = dialogMode.value
  const applyCompatibility = !closeAfterSave
  const now = new Date().toISOString()
  try {
    if (mock.value) {
      if (mode === 'edit' && editing.value) {
        const current = rows.value.find((row) => row.ruleNo === editing.value?.ruleNo)
        if (!current) throw new Error('資料已不存在，請重新查詢。')
        const updated: RuleRow = { ...current, ruleName: draft.value.ruleName.trim(), ruleScript: draft.value.ruleScript, description: draft.value.description, ruleType: draft.value.ruleType, dataStamp: (current.dataStamp ?? 0) + 1, reviser: props.currentUserNo || 'DEVUSER', reviseDate: now }
        if (!applyCompatibility) { updated.sendMail = draft.value.sendMail; updated.holdDispGroup = draft.value.holdDispGroup }
        rows.value = rows.value.map((row) => row.ruleNo === current.ruleNo ? updated : row)
        editing.value = clone(updated)
      } else {
        if (rows.value.some((row) => row.ruleNo.toLocaleUpperCase() === draft.value.ruleNo)) throw new Error('Rule No 已存在，請輸入其他編號。')
        rows.value.unshift({ ruleNo: draft.value.ruleNo, ruleName: draft.value.ruleName.trim(), ruleScript: draft.value.ruleScript, description: draft.value.description, creator: props.currentUserNo || 'DEVUSER', createDate: now, holdDispGroup: applyCompatibility ? '' : draft.value.holdDispGroup, sendMail: applyCompatibility ? 0 : draft.value.sendMail, ruleClass: 0, ruleType: draft.value.ruleType, ruleMode: 0, ruleModeQty: null, dataStamp: 0, reviser: '', reviseDate: '' })
      }
      persistMock()
    } else {
      const body: Record<string, unknown> = { ruleName: draft.value.ruleName.trim(), ruleScript: draft.value.ruleScript, description: draft.value.description, ruleType: draft.value.ruleType }
      if (!applyCompatibility) { body.sendMail = draft.value.sendMail; body.holdDispGroup = draft.value.holdDispGroup }
      if (mode === 'edit' && editing.value) {
        body.dataStamp = editing.value.dataStamp
        await api('/api/operation-control-rules/' + encodeURIComponent(editing.value.ruleNo), { method: 'PUT', body: JSON.stringify(body) })
      } else {
        body.ruleNo = draft.value.ruleNo
        await api('/api/operation-control-rules', { method: 'POST', body: JSON.stringify(body) })
      }
    }
    await loadRows()
    if (rows.value.some((row) => row.ruleNo === draft.value.ruleNo)) { activeRuleNo.value = draft.value.ruleNo; selectedRuleNos.value = [draft.value.ruleNo] }
    notice.value = applyCompatibility ? '已套用；欄位更新依 WinForms Apply 規則儲存。' : '作業站法則已儲存。'
    if (closeAfterSave) closeDialog()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '儲存作業站法則失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = selectedRows.value.length ? [...selectedRows.value] : selectedRow.value ? [selectedRow.value] : []
  if (!targets.length) { notice.value = '請先選取作業站法則。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆作業站法則？刪除後無法復原。')) return
  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) { const keys = new Set(targets.map((row) => row.ruleNo)); rows.value = rows.value.filter((row) => !keys.has(row.ruleNo)); persistMock() }
    else for (const row of targets) await api('/api/operation-control-rules/' + encodeURIComponent(row.ruleNo), { method: 'DELETE' })
    await loadRows(); notice.value = '已刪除 ' + targets.length + ' 筆作業站法則。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除作業站法則失敗。' }
  finally { busy.value = false }
}
function openScriptEditor(): void { scriptDraft.value = draft.value.ruleScript; scriptEditorOpen.value = true }
function saveScriptEditor(): void {
  if (scriptDraft.value.length > 3000) { error.value = 'Rule Script 最多 3000 個字元。'; return }
  draft.value.ruleScript = scriptDraft.value; scriptEditorOpen.value = false; error.value = ''
}
async function openHoldGroupPicker(): Promise<void> {
  error.value = ''
  try {
    if (mock.value) holdGroups.value = ['DEV-HOLD-ENG', 'DEV-HOLD-QA', 'DEV-HOLD-LEADER']
    else {
      const result = await api<unknown>('/api/qc/hold-disposition-groups?searchFieldName=GroupNo&searchFieldValue=')
      const items = Array.isArray(result) ? result : ((result as ApiEnvelope<unknown> | null)?.items ?? (result as ApiEnvelope<unknown> | null)?.rows ?? (result as ApiEnvelope<unknown> | null)?.data ?? [])
      holdGroups.value = items.map((item) => typeof item === 'string' ? item : String((item as Record<string, unknown>)?.groupNo ?? (item as Record<string, unknown>)?.GroupNo ?? (item as Record<string, unknown>)?.groupno ?? (item as Record<string, unknown>)?.GROUPNO ?? '')).filter(Boolean)
    }
    holdGroupPickerOpen.value = true
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 Hold Disp Group 失敗。' }
}
function chooseHoldGroup(group: string): void { draft.value.holdDispGroup = group; holdGroupPickerOpen.value = false }
function typeName(type: RuleType): string { return type === 0 ? 'Lot' : 'Component' }

onMounted(() => { void loadRows() })
</script>

<template>
  <div class="op-control-rule-page">
    <header class="page-title"><h1>作業站法則設定</h1><span>OP101 · TBLOPCONTROLRULE</span></header>
    <form class="query-panel" @submit.prevent="loadRows">
      <label class="search-field"><span>Rule No</span><input v-model="keyword" class="kd-field" maxlength="20" /></label><span class="wildcard">%</span>
      <button class="button" type="submit" :disabled="loading || busy">查詢</button>
    </form>
    <p v-if="error" class="message error-message" role="alert">{{ error }}</p>
    <p v-if="notice" class="message" role="status">{{ notice }}</p>
    <p v-if="mock" class="dev-note">開發區本機假資料：僅儲存在目前瀏覽器 localStorage，不會寫入 MES 或 YMES 資料庫。</p>
    <section class="master-screen">
      <div class="grid-panel">
        <div class="table-scroll"><table class="data-grid">
          <thead><tr><th>Rule No</th><th>Rule Name</th><th>Rule Script</th><th>Description</th><th>Creator</th><th>Create Date</th><th>Send Mail</th><th>Hold Disp Group</th><th>Rule Type</th><th>Reviser</th><th>Revise Date</th></tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="row.ruleNo" :class="{ selected: selectedRuleNos.includes(row.ruleNo), active: activeRuleNo === row.ruleNo }" :aria-selected="selectedRuleNos.includes(row.ruleNo)" tabindex="0" @click="selectRow(row, $event)" @keydown.enter.prevent="selectRow(row)">
              <td>{{ row.ruleNo }}</td><td>{{ row.ruleName }}</td><td class="script-cell" :title="row.ruleScript">{{ row.ruleScript }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ displayDate(row.createDate) }}</td><td>{{ row.sendMail ? 'Yes' : 'No' }}</td><td>{{ row.holdDispGroup }}</td><td>{{ typeName(row.ruleType) }}</td><td>{{ row.reviser }}</td><td>{{ displayDate(row.reviseDate) }}</td>
            </tr>
            <tr v-if="!loading && !visibleRows.length"><td colspan="11" class="empty-cell">沒有符合條件的作業站法則</td></tr>
            <tr v-if="loading && !visibleRows.length"><td colspan="11" class="empty-cell">資料載入中…</td></tr>
          </tbody>
        </table></div>
        <footer class="grid-footer">資料總筆數 {{ visibleRows.length }}</footer>
      </div>
      <aside class="action-panel">
        <button class="button primary" :disabled="busy" @click="openDialog('new')">ADD (A)</button>
        <button class="button" :disabled="busy || !selectedRow" @click="openDialog('edit', selectedRow)">EDIT (E)</button>
        <button class="button" :disabled="busy || (!selectedRuleNos.length && !selectedRow)" @click="deleteSelected">DEL (D)</button>
        <button class="button" :disabled="busy || !selectedRow" @click="openDialog('copy', selectedRow)">Copy (C)</button>
        <button class="button close-grid" @click="emit('close')">Close (X)</button>
      </aside>
    </section>

    <div v-if="dialogOpen" class="dialog-backdrop" @click.self="closeDialog">
      <section class="definition-dialog" role="dialog" aria-modal="true" aria-labelledby="op-rule-dialog-title">
        <h2 id="op-rule-dialog-title">OP Control Rule Define ({{ dialogMode === 'new' ? 'ADD' : dialogMode === 'edit' ? 'EDIT' : 'Copy' }})</h2>
        <fieldset class="data-group"><legend>OP Control Rule Data</legend>
          <div class="form-row rule-type-row"><span>RuleType</span><fieldset class="radio-group"><label><input v-model.number="draft.ruleType" type="radio" :value="0" />Lot</label><label><input v-model.number="draft.ruleType" type="radio" :value="1" />Component</label></fieldset></div>
          <label class="form-row"><span>Rule No</span><input v-model="draft.ruleNo" class="kd-field rule-no" maxlength="20" :readonly="dialogMode === 'edit'" @blur="draft.ruleNo = draft.ruleNo.trim().toUpperCase()" /></label>
          <label class="form-row"><span>Rule Name</span><input v-model="draft.ruleName" class="kd-field rule-name" maxlength="50" /></label>
          <div class="form-row script-row"><span>Rule Script</span><div class="script-fields"><textarea :value="draft.ruleScript" readonly aria-label="Rule Script" /><button class="button" type="button" @click="openScriptEditor">Define Script</button></div></div>
          <label class="form-row description-row"><span>Description</span><textarea v-model="draft.description" maxlength="255" /></label>
          <div class="form-row"><span>Send Mail</span><fieldset class="radio-group"><label><input v-model.number="draft.sendMail" type="radio" :value="1" />Yes</label><label><input v-model.number="draft.sendMail" type="radio" :value="0" />No</label></fieldset></div>
          <div class="form-row"><span>Hold Disp Group</span><div class="lookup-fields"><input :value="draft.holdDispGroup" readonly maxlength="50" /><button class="button" type="button" aria-label="選擇 Hold Disp Group" @click="openHoldGroupPicker">...</button></div></div>
        </fieldset>
        <fieldset class="audit-group"><legend>建立/修改資訊</legend>
          <label><span>建立人</span><input :value="dialogMode === 'edit' ? editing?.creator : currentUserNo" disabled /></label>
          <label><span>建立日期</span><input :value="dialogMode === 'edit' ? displayDate(editing?.createDate ?? '') : '儲存時由伺服器建立'" disabled /></label>
          <label v-if="dialogMode === 'edit'"><span>修改人</span><input :value="currentUserNo" disabled /></label>
          <label v-if="dialogMode === 'edit'"><span>修改日期</span><input :value="displayDate(new Date().toISOString())" disabled /></label>
        </fieldset>
        <p v-if="error" class="message error-message dialog-error" role="alert">{{ error }}</p>
        <footer class="dialog-actions"><button class="button primary" :disabled="busy" @click="saveForm(true)">確定</button><button class="button" :disabled="busy" @click="closeDialog">關閉</button><button class="button" :disabled="busy" @click="saveForm(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="scriptEditorOpen" class="dialog-backdrop nested-backdrop" @click.self="scriptEditorOpen = false">
      <section class="script-dialog" role="dialog" aria-modal="true" aria-labelledby="script-dialog-title">
        <h2 id="script-dialog-title">OP Script Define</h2><p>DEV 以純文字方式編輯 Script；此頁不執行 Script。</p>
        <textarea v-model="scriptDraft" maxlength="3000" spellcheck="false" />
        <footer class="dialog-actions"><button class="button primary" @click="saveScriptEditor">確定</button><button class="button" @click="scriptEditorOpen = false">關閉</button></footer>
      </section>
    </div>

    <div v-if="holdGroupPickerOpen" class="dialog-backdrop nested-backdrop" @click.self="holdGroupPickerOpen = false">
      <section class="lookup-dialog" role="dialog" aria-modal="true" aria-labelledby="hold-group-title">
        <h2 id="hold-group-title">Hold Disp Group</h2><div class="lookup-list"><button v-for="group in holdGroups" :key="group" class="lookup-item" @click="chooseHoldGroup(group)">{{ group }}</button><p v-if="!holdGroups.length">沒有可選的 Hold Disp Group。</p></div>
        <footer class="dialog-actions"><button class="button" @click="holdGroupPickerOpen = false">關閉</button></footer>
      </section>
    </div>
  </div>
</template>

<style scoped>
.op-control-rule-page{min-height:100%;padding:0 5px 12px;color:#353535;font:14px Arial,"Microsoft JhengHei",sans-serif}.page-title{display:flex;align-items:center;gap:14px;min-height:58px;padding:0 24px;border-bottom:1px solid #dedbd3;background:#f7f5ef}.page-title h1{margin:0;color:#342d22;font-size:21px;font-weight:600}.page-title>span{color:#827b70;font-size:12px}.query-panel{display:flex;align-items:center;gap:8px;min-height:48px;margin-top:8px;padding:5px 10px;border:1px solid #d2d2d2;background:#fff}.search-field{display:flex;align-items:center;gap:8px;margin-left:4px;white-space:nowrap}.kd-field{height:29px;border:1px solid #929292;border-radius:2px;padding:4px 7px;background:#fff0b0;color:#303030;font:inherit}.kd-field:focus{outline:2px solid #e0a031;outline-offset:1px}.search-field .kd-field{width:190px}.wildcard{margin-left:-5px}.button{min-height:31px;padding:4px 12px;border:1px solid #bbb;border-radius:4px;background:linear-gradient(#fff,#f3f3f3);color:#333;cursor:pointer;font:inherit;white-space:nowrap}.button:hover:not(:disabled){border-color:#dd9b2d;background:#fff8e7}.button:disabled{color:#aaa;cursor:not-allowed}.button.primary{border-color:#f1a32c;background:#ff9f1c;color:#fff}.message{margin:6px 0 0;padding:7px 9px;color:#62522e;background:#fff9e8}.error-message{color:#8c251e;background:#fff0ed}.dev-note{margin:6px 0 0;padding:6px 9px;color:#695a35;background:#fff9e8;font-size:12px}.master-screen{display:grid;grid-template-columns:minmax(0,1fr) 96px;gap:6px;min-height:calc(100vh - 190px);margin-top:7px;border:1px solid #ccc;padding:6px;background:#fff}.grid-panel{display:flex;min-height:calc(100vh - 204px);min-width:0;flex-direction:column}.action-panel{display:flex;flex-direction:column;gap:8px;padding-left:2px}.action-panel .button{width:88px}.table-scroll{flex:1;min-height:300px;overflow:auto;border:1px solid #d0d0d0}.data-grid{width:100%;min-width:1250px;border-collapse:collapse;white-space:nowrap}.data-grid th,.data-grid td{max-width:220px;overflow:hidden;text-overflow:ellipsis;padding:5px 8px;border:1px solid #d0d0d0;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#e7e9eb;color:#383838;font-weight:600}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#fff8e7}.data-grid tbody tr.selected{background:#ffecb4}.data-grid tbody tr.active{outline:1px solid #d89a2d;outline-offset:-1px}.script-cell{max-width:180px!important}.empty-cell{padding:22px!important;color:#777;text-align:center!important}.grid-footer{padding:6px 2px 0;color:#666;font-size:12px}.close-grid{margin-top:auto}.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:14px;background:#17161166}.definition-dialog{box-sizing:border-box;width:min(519px,96vw);min-height:496px;padding:10px 12px 12px;border:1px solid #aaa;background:#fff;box-shadow:0 12px 32px #0003}.definition-dialog h2,.script-dialog h2,.lookup-dialog h2{margin:0 0 7px;font-size:16px;font-weight:600}.data-group{min-height:356px;margin:0;padding:7px 10px 9px;border:1px solid #bcbcbc}.data-group legend,.audit-group legend{padding:0 5px}.form-row{display:grid;grid-template-columns:96px minmax(0,1fr);align-items:start;gap:8px;margin:6px 0}.form-row>span{text-align:right;padding-top:5px}.form-row>.kd-field{width:min(160px,100%);box-sizing:border-box}.rule-type-row{margin-top:3px}.radio-group{display:flex;align-items:center;gap:22px;min-height:30px;margin:0;padding:0 7px;border:1px solid #c4c4c4}.radio-group label{display:flex;align-items:center;gap:4px;white-space:nowrap}.radio-group input{accent-color:#555}.script-fields{display:flex;min-width:0;flex-direction:column;align-items:flex-start;gap:5px}.script-fields textarea{box-sizing:border-box;width:100%;height:109px;resize:none;border:1px solid #888;padding:5px 7px;background:#fff;font:13px Consolas,monospace}.script-fields .button{min-height:30px}.description-row textarea{box-sizing:border-box;width:100%;height:45px;resize:none;border:1px solid #888;padding:5px 7px;font:inherit}.lookup-fields{display:flex;align-items:center;gap:5px}.lookup-fields input{box-sizing:border-box;width:min(254px,calc(100% - 36px));height:27px;border:1px solid #888;padding:4px 6px;background:#fff;color:#333;font:inherit}.lookup-fields .button{min-width:27px;padding:2px 5px}.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:5px 8px;margin:7px 0;padding:7px;border:1px solid #bcbcbc}.audit-group label{display:grid;grid-template-columns:65px minmax(0,1fr);align-items:center;gap:5px;font-size:12px}.audit-group label>span{text-align:right}.audit-group input{box-sizing:border-box;width:100%;height:25px;border:1px solid #bbb;padding:3px 5px;background:#eee;color:#666;font:inherit}.dialog-error{margin-top:4px;font-size:12px}.dialog-actions{display:flex;justify-content:flex-end;gap:6px;margin-top:8px}.dialog-actions .primary{margin-left:auto}.nested-backdrop{z-index:1100}.script-dialog,.lookup-dialog{box-sizing:border-box;width:min(560px,95vw);padding:14px;border:1px solid #aaa;background:#fff;box-shadow:0 12px 32px #0003}.script-dialog p{margin:4px 0 8px;color:#6d6659;font-size:12px}.script-dialog textarea{box-sizing:border-box;width:100%;height:300px;resize:vertical;border:1px solid #888;padding:8px;background:#fffef7;font:13px Consolas,monospace}.lookup-list{max-height:300px;overflow:auto;border:1px solid #ccc}.lookup-item{display:block;width:100%;padding:8px 10px;border:0;border-bottom:1px solid #ddd;background:#fff;text-align:left;cursor:pointer;font:inherit}.lookup-item:hover{background:#fff0b0}.lookup-list p{padding:12px;color:#777}@media(max-width:760px){.query-panel{flex-wrap:wrap}.master-screen{grid-template-columns:1fr;min-height:65vh}.grid-panel{min-height:55vh}.action-panel{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));padding:0}.action-panel .button{width:100%}.close-grid{margin:0}.audit-group{grid-template-columns:1fr}.definition-dialog{max-height:96vh;min-height:0;overflow:auto}.data-group{min-height:0}}
</style>
