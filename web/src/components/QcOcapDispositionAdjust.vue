<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type ActionOption = 'changeGroup' | 'finish'
interface TargetNode { nodeId: string; nodeNo: string; groupNo: string; groupName: string; linkName: string }
interface DispositionRow {
  erfNo: string; lotNo: string; productNo: string; holdItemNo: string; ocapNo: string; ocapVersion: string
  erfSource: number; erfStatus: number; ocapStatus: number; currentGroupNo: string; currentNodeNo: string
  currentNodeId: string; lotStamp: number | null; createDate: string; targets: TargetNode[]
}
interface Envelope<T> { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-ocap-disposition-adjust:v1'
const demoTargets: TargetNode[] = [
  { nodeId: 'DEV-OCAP-01-0002', nodeNo: 'QC-REVIEW', groupNo: 'DEV-QC-GROUP', groupName: 'DEV 品保處理群組', linkName: 'pass' },
  { nodeId: 'DEV-OCAP-01-0003', nodeNo: 'ENG-REVIEW', groupNo: 'DEV-ENG-GROUP', groupName: 'DEV 工程處理群組', linkName: 'pass' },
  { nodeId: 'DEV-OCAP-01-0004', nodeNo: 'END', groupNo: '', groupName: '最終處置', linkName: 'pass' },
]
const seedRows: DispositionRow[] = [
  { erfNo: 'DEV-ERF-0001', lotNo: 'DEV-LOT-1001', productNo: 'DEV-PROD-A', holdItemNo: 'DEV-HOLD-01', ocapNo: 'DEV-OCAP-01', ocapVersion: '01', erfSource: 0, erfStatus: 3, ocapStatus: 2, currentGroupNo: 'DEV-QC-GROUP', currentNodeNo: 'QC-REVIEW', currentNodeId: 'DEV-OCAP-01-0002', lotStamp: 18, createDate: '2026-11-17T08:15:00+08:00', targets: clone(demoTargets) },
  { erfNo: 'DEV-ERF-0001', lotNo: 'DEV-LOT-1001', productNo: 'DEV-PROD-A', holdItemNo: 'DEV-HOLD-02', ocapNo: 'DEV-OCAP-01', ocapVersion: '01', erfSource: 0, erfStatus: 3, ocapStatus: 2, currentGroupNo: 'DEV-QC-GROUP', currentNodeNo: 'QC-REVIEW', currentNodeId: 'DEV-OCAP-01-0002', lotStamp: 18, createDate: '2026-11-17T08:18:00+08:00', targets: clone(demoTargets) },
  { erfNo: 'DEV-ERF-0002', lotNo: 'DEV-LOT-1002', productNo: 'DEV-PROD-B', holdItemNo: 'DEV-HOLD-03', ocapNo: 'DEV-OCAP-02', ocapVersion: '02', erfSource: 1, erfStatus: 3, ocapStatus: 2, currentGroupNo: 'DEV-ENG-GROUP', currentNodeNo: 'ENG-REVIEW', currentNodeId: 'DEV-OCAP-02-0003', lotStamp: null, createDate: '2026-11-17T10:40:00+08:00', targets: clone(demoTargets) },
]

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function emptyFilters() { return { useErfNo: false, erfNo: '', useLotNo: false, lotNo: '', useDateFrom: false, dateFrom: '', useDateTo: false, dateTo: '' } }
const filters = ref(emptyFilters())
const appliedFilters = ref(emptyFilters())
const rows = ref<DispositionRow[]>([])
const activeKey = ref('')
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const queried = ref(false)
const action = ref<ActionOption | ''>('')
const description = ref('')
const selectedTargetId = ref('')
const targets = ref<TargetNode[]>([])
const routePickerOpen = ref(false)
const routeLoading = ref(false)
const routeError = ref('')

const rowKey = (row: DispositionRow) => `${row.erfNo}::${row.holdItemNo}::${row.ocapNo}::${row.ocapVersion}`
const activeRow = computed(() => rows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const selectedTarget = computed(() => targets.value.find(target => target.nodeId === selectedTargetId.value) ?? null)
const changeGroupMode = computed(() => action.value === 'changeGroup')
const formattedDescription = computed(() => action.value === 'finish' ? 'Hold Item Disposition Done' : action.value === 'changeGroup' ? 'Change Disposition Group' : '')

function endpoint(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(endpoint(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入已逾期，請重新登入。') }
  const text = await response.text()
  if (!response.ok) throw new Error(text || `API 請求失敗 (${response.status})`)
  return (text ? JSON.parse(text) : undefined) as T
}
function list<T>(value: T[] | Envelope<T>): T[] { return Array.isArray(value) ? value : value.items ?? value.rows ?? value.data ?? [] }
function normalizeTarget(value: Record<string, unknown>): TargetNode {
  const field = (name: string) => value[name] ?? value[name[0].toUpperCase() + name.slice(1)]
  return { nodeId: String(field('nodeId') ?? ''), nodeNo: String(field('nodeNo') ?? ''), groupNo: String(field('groupNo') ?? ''), groupName: String(field('groupName') ?? ''), linkName: String(field('linkName') ?? '') }
}
function normalizeRow(value: Record<string, unknown>): DispositionRow {
  const field = (name: string) => value[name] ?? value[name[0].toUpperCase() + name.slice(1)]
  const rawTargets = field('targets')
  return {
    erfNo: String(field('erfNo') ?? ''), lotNo: String(field('lotNo') ?? ''), productNo: String(field('productNo') ?? ''),
    holdItemNo: String(field('holdItemNo') ?? ''), ocapNo: String(field('ocapNo') ?? ''), ocapVersion: String(field('ocapVersion') ?? ''),
    erfSource: Number(field('erfSource') ?? 0), erfStatus: Number(field('erfStatus') ?? 0), ocapStatus: Number(field('ocapStatus') ?? 0),
    currentGroupNo: String(field('currentGroupNo') ?? ''), currentNodeNo: String(field('currentNodeNo') ?? ''), currentNodeId: String(field('currentNodeId') ?? ''),
    lotStamp: field('lotStamp') == null ? null : Number(field('lotStamp')), createDate: String(field('createDate') ?? ''),
    targets: Array.isArray(rawTargets) ? (rawTargets as Record<string, unknown>[]).map(normalizeTarget) : [],
  }
}
function demoRows(): DispositionRow[] {
  try { const saved = localStorage.getItem(storageKey); if (saved) return (JSON.parse(saved) as Record<string, unknown>[]).map(normalizeRow) }
  catch { /* restore development sample records */ }
  return clone(seedRows)
}
function persist(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function filterQuery(source: typeof filters.value): URLSearchParams {
  const query = new URLSearchParams()
  if (source.useErfNo && source.erfNo.trim()) query.set('erfNo', source.erfNo.trim())
  if (source.useLotNo && source.lotNo.trim()) query.set('lotNo', source.lotNo.trim())
  if (source.useDateFrom && source.dateFrom) query.set('createDateFrom', source.dateFrom)
  if (source.useDateTo && source.dateTo) query.set('createDateTo', source.dateTo)
  return query
}
function filteredDemoRows(source: typeof filters.value): DispositionRow[] {
  return demoRows().filter(row => row.ocapStatus !== 11)
    .filter(row => !source.useErfNo || !source.erfNo.trim() || row.erfNo.toLowerCase().includes(source.erfNo.trim().toLowerCase()))
    .filter(row => !source.useLotNo || !source.lotNo.trim() || row.lotNo.toLowerCase().includes(source.lotNo.trim().toLowerCase()))
    .filter(row => !source.useDateFrom || !source.dateFrom || new Date(row.createDate) >= new Date(source.dateFrom))
    .filter(row => !source.useDateTo || !source.dateTo || new Date(row.createDate) <= new Date(source.dateTo))
    .sort((a, b) => a.lotNo.localeCompare(b.lotNo))
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''; queried.value = true
  const query = clone(filters.value); appliedFilters.value = query
  try {
    if (mock.value) rows.value = filteredDemoRows(query)
    else rows.value = list(await api<Record<string, unknown>[] | Envelope<Record<string, unknown>>>(`/api/qc/ocap-dispositions?${filterQuery(query)}`)).map(normalizeRow)
    activeKey.value = ''; action.value = ''; description.value = ''; selectedTargetId.value = ''; targets.value = []
    if (!rows.value.length) notice.value = '沒有符合條件且仍待處置的 OCAP 異常單。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 OCAP 異常單失敗。' }
  finally { loading.value = false }
}
function selectRow(row: DispositionRow): void { activeKey.value = rowKey(row); action.value = ''; description.value = ''; selectedTargetId.value = ''; targets.value = []; notice.value = '' }
function chooseAction(value: ActionOption): void {
  action.value = value; description.value = ''; selectedTargetId.value = ''; targets.value = []; notice.value = ''
  if (value === 'finish') routePickerOpen.value = false
}
async function openRoutePicker(): Promise<void> {
  const row = activeRow.value
  if (!row) { notice.value = '請先選取一筆 OCAP 異常單。'; return }
  routePickerOpen.value = true; routeLoading.value = true; routeError.value = ''; targets.value = []
  try {
    if (mock.value) targets.value = clone(row.targets.filter(target => target.nodeNo !== 'END'))
    else {
      const query = new URLSearchParams({ ocapNo: row.ocapNo, ocapVersion: row.ocapVersion, currentNodeId: row.currentNodeId })
      const result = await api<Record<string, unknown>[] | Envelope<Record<string, unknown>>>(`/api/qc/ocap-dispositions/${encodeURIComponent(row.erfNo)}/${encodeURIComponent(row.holdItemNo)}/route-options?${query}`)
      targets.value = list(result).map(normalizeTarget)
    }
    if (!targets.value.length) routeError.value = '目前流程沒有可選的 OCAP 節點。'
  } catch (cause) { routeError.value = cause instanceof Error ? cause.message : '載入 OCAP 流程節點失敗。' }
  finally { routeLoading.value = false }
}
function useTarget(target: TargetNode): void { selectedTargetId.value = target.nodeId; routePickerOpen.value = false; notice.value = '' }
function selectedTargetText(row: DispositionRow): string {
  if (!selectedTarget.value) return row.currentGroupNo || row.currentNodeNo
  return selectedTarget.value.groupNo || selectedTarget.value.nodeNo
}
async function confirmDisposition(): Promise<void> {
  const row = activeRow.value
  if (!row) { notice.value = '請先選取一筆異常單。'; return }
  if (!action.value) { notice.value = '請選擇「變更處理群組」或「Hold Item 處置完成」。'; return }
  if (!description.value.trim()) { notice.value = 'Disp Description 為必填。'; return }
  if (description.value.trim().length > 255) { notice.value = 'Disp Description 不可超過 255 字元。'; return }
  if (action.value === 'changeGroup' && !selectedTarget.value) { notice.value = '請先從 OCAP 流程圖選擇處置節點。'; return }
  if (row.erfSource === 0 && row.lotStamp == null) { notice.value = '找不到 Hold 中的 Lot State/LotStamp，無法調整。'; return }

  busy.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) {
      const nextGroup = action.value === 'finish' ? 'END' : selectedTargetText(row)
      const now = new Date().toISOString()
      if (action.value === 'changeGroup') {
        row.currentGroupNo = nextGroup; row.currentNodeNo = selectedTarget.value?.nodeNo ?? nextGroup; row.currentNodeId = selectedTarget.value?.nodeId ?? row.currentNodeId
      } else row.ocapStatus = 11
      row.createDate = now
      rows.value = rows.value.filter(item => item.ocapStatus !== 11)
      persist()
    } else {
      await api(`/api/qc/ocap-dispositions/${encodeURIComponent(row.erfNo)}/${encodeURIComponent(row.holdItemNo)}/adjust`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ocapNo: row.ocapNo, ocapVersion: row.ocapVersion, action: action.value, targetNodeId: action.value === 'changeGroup' ? selectedTarget.value?.nodeId : null, description: description.value.trim(), lotStamp: row.lotStamp }),
      })
    }
    notice.value = action.value === 'finish' ? 'Hold Item 處置已完成。' : `已轉派至 ${selectedTargetText(row)}。`
    const priorKey = rowKey(row); await load()
    if (rows.value.some(item => rowKey(item) === priorKey)) activeKey.value = priorKey
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '更新 OCAP 處置失敗。' }
  finally { busy.value = false }
}
function formatDate(value: string): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }
function closeRoutePicker(): void { routePickerOpen.value = false }
onMounted(() => { if (mock.value) void load() })
</script>

<template>
  <main class="ocap-adjust-page">
    <header class="page-title"><h1>OCAP異常單處置調整</h1><span>異常設定</span></header>

    <form class="query-panel" @submit.prevent="load">
      <div class="query-row">
        <label class="filter-field"><input v-model="filters.useErfNo" type="checkbox"><span>ERF No</span><input v-model="filters.erfNo" class="kd-field" :disabled="!filters.useErfNo" maxlength="20"></label>
        <label class="filter-field"><input v-model="filters.useLotNo" type="checkbox"><span>Lot No</span><input v-model="filters.lotNo" class="kd-field" :disabled="!filters.useLotNo" maxlength="50"></label>
        <button class="button primary" type="submit" :disabled="loading">查詢</button>
      </div>
      <div class="query-row date-row">
        <label class="filter-field date-filter"><input v-model="filters.useDateFrom" type="checkbox"><span>建立日期</span><span>從</span><input v-model="filters.dateFrom" class="kd-field" type="datetime-local" :disabled="!filters.useDateFrom"></label>
        <span>至</span><input v-model="filters.dateTo" class="kd-field date-input" type="datetime-local" :disabled="!filters.useDateTo">
        <label class="date-enable"><input v-model="filters.useDateTo" type="checkbox"><span>啟用結束日期</span></label>
      </div>
    </form>

    <div v-if="error" class="message error-message" role="alert">{{ error }}</div>
    <div v-else-if="notice" class="message" role="status">{{ notice }}</div>

    <div class="adjust-layout">
      <section class="case-panel">
        <h2>ERF Query</h2>
        <div class="table-wrap">
          <table class="data-grid">
            <thead><tr><th>Lot No</th><th>OCAP No</th><th>ERF No</th><th>Product No</th></tr></thead>
            <tbody>
              <tr v-for="row in rows" :key="rowKey(row)" :class="{ selected: activeKey === rowKey(row) }" @click="selectRow(row)">
                <td>{{ row.lotNo }}</td><td>{{ row.ocapNo }}</td><td>{{ row.erfNo }}</td><td>{{ row.productNo }}</td>
              </tr>
              <tr v-if="loading"><td colspan="4" class="empty-cell">載入中…</td></tr>
              <tr v-else-if="!rows.length"><td colspan="4" class="empty-cell">{{ queried ? '沒有待處置資料' : '請設定條件後按「查詢」' }}</td></tr>
            </tbody>
          </table>
        </div>
        <footer class="list-footer">資料總筆數：{{ rows.length }}</footer>
      </section>

      <section class="action-panel">
        <h2>Disposition Option</h2>
        <div class="current-case" v-if="activeRow">
          <div><span>目前處理群組</span><strong>{{ activeRow.currentGroupNo || activeRow.currentNodeNo }}</strong></div>
          <div><span>Hold Item</span><strong>{{ activeRow.holdItemNo }}</strong></div>
          <div><span>目前 OCAP</span><strong>{{ activeRow.ocapNo }} / {{ activeRow.ocapVersion }}</strong></div>
        </div>
        <div v-else class="case-placeholder">請先選取左側資料列。</div>

        <fieldset class="option-group" :disabled="!activeRow || busy">
          <legend>處置選項</legend>
          <label class="radio-option"><input type="radio" name="ocap-action" :checked="action === 'changeGroup'" @change="chooseAction('changeGroup')"><span>Change Disp Group No</span></label>
          <label class="radio-option"><input type="radio" name="ocap-action" :checked="action === 'finish'" @change="chooseAction('finish')"><span>Hold Item Disposition Done</span></label>
        </fieldset>

        <div v-if="changeGroupMode" class="jump-panel">
          <strong>Jump OP</strong>
          <label><span>Disp Group No</span><input class="kd-field group-readonly" :value="selectedTarget ? selectedTargetText(activeRow!) : ''" readonly><button class="button" type="button" :disabled="!activeRow || busy" @click="openRoutePicker">…</button></label>
          <small v-if="selectedTarget">節點 {{ selectedTarget.nodeNo }}　連線 {{ selectedTarget.linkName }}</small>
        </div>

        <label class="description-field"><span>Disp Description</span><textarea v-model="description" class="kd-field" maxlength="255" rows="7" :disabled="!action || !activeRow || busy" placeholder="輸入本次處置說明"></textarea></label>
        <div v-if="activeRow && activeRow.erfSource === 0" class="lot-stamp-note">來源為系統 ERF；儲存時會核對 LotStamp，請重新查詢後重試並行衝突。</div>
        <div class="action-footer"><button class="button primary" type="button" :disabled="busy || !activeRow" @click="confirmDisposition">Confirm(C)</button><button class="button" type="button" @click="emit('close')">Close(X)</button></div>
      </section>
    </div>

    <div v-if="routePickerOpen" class="dialog-backdrop" @click.self="closeRoutePicker">
      <section class="route-dialog" role="dialog" aria-modal="true" aria-label="選擇 OCAP 處置群組">
        <header><div><h2>選擇 OCAP 節點</h2><small v-if="activeRow">{{ activeRow.ocapNo }} / {{ activeRow.ocapVersion }} · 目前：{{ activeRow.currentNodeNo }}</small></div><button class="icon-close" type="button" aria-label="關閉" @click="closeRoutePicker">×</button></header>
        <p v-if="routeLoading" class="empty-cell">載入流程節點中…</p>
        <p v-else-if="routeError" class="message error-message">{{ routeError }}</p>
        <div v-else class="target-list">
          <button v-for="target in targets" :key="target.nodeId" class="target-row" type="button" @click="useTarget(target)">
            <span class="target-node">{{ target.nodeNo }}</span><span class="target-arrow">→</span><span class="target-group"><strong>{{ target.groupNo || '無群組' }}</strong><small>{{ target.groupName || 'OCAP 節點' }}</small></span><span class="target-link">{{ target.linkName }}</span>
          </button>
        </div>
        <footer><button class="button" type="button" @click="closeRoutePicker">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.ocap-adjust-page { min-height: 100%; padding: 0 5px 16px; color: #353535; font: 14px Arial, "Microsoft JhengHei", sans-serif; }
.page-title { display: flex; align-items: center; gap: 14px; min-height: 58px; padding: 0 24px; border-bottom: 1px solid #dedbd3; background: #f7f5ef; }
.page-title h1 { margin: 0; color: #342d22; font-size: 21px; font-weight: 600; }
.page-title > span { color: #827b70; }
.query-panel, .case-panel, .action-panel { border: 1px solid #c9c9c9; background: #fff; }
.query-panel { display: grid; gap: 7px; margin-top: 8px; padding: 9px 12px; }
.query-row { display: flex; align-items: center; gap: 16px; flex-wrap: wrap; }
.filter-field { display: inline-flex; align-items: center; gap: 7px; }
.filter-field > input[type="checkbox"], .radio-option input { accent-color: #e99b21; }
.filter-field .kd-field { width: 185px; }
.date-row { gap: 9px; }
.date-filter .kd-field { width: 205px; }
.date-input { width: 205px; }
.date-enable { display: inline-flex; align-items: center; gap: 5px; margin-left: auto; }
.kd-field { min-height: 30px; border: 1px solid #949494; border-radius: 2px; padding: 4px 7px; background: #fff0b0; color: #303030; font: inherit; }
.kd-field:focus { outline: 2px solid #e0a031; outline-offset: 1px; }
.kd-field:disabled, .kd-field:read-only { background: #eee; color: #777; }
.button { min-height: 31px; padding: 4px 13px; border: 1px solid #bbb; border-radius: 4px; background: linear-gradient(#fff, #f3f3f3); color: #333; cursor: pointer; font: inherit; white-space: nowrap; }
.button:hover:not(:disabled) { border-color: #dd9b2d; background: #fff8e7; }
.button:disabled { color: #aaa; cursor: not-allowed; }
.button.primary { border-color: #f1a32c; background: #ff9f1c; color: #fff; }
.button.primary:hover:not(:disabled) { background: #ec8e0a; }
.message { margin-top: 7px; padding: 7px 9px; color: #62522e; background: #fff9e8; }
.error-message { color: #8c251e; background: #fff0ed; }
.adjust-layout { display: grid; grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr); gap: 8px; min-height: min(62vh, 520px); margin-top: 8px; }
.case-panel, .action-panel { min-width: 0; padding: 9px; }
.case-panel h2, .action-panel h2 { margin: 1px 0 9px; color: #555; font-size: 15px; font-weight: 600; }
.table-wrap { max-height: calc(62vh - 56px); overflow: auto; border: 1px solid #d1d1d1; }
.data-grid { width: 100%; border-collapse: collapse; white-space: nowrap; }
.data-grid th { position: sticky; top: 0; z-index: 1; padding: 6px 8px; border: 1px solid #d1d1d1; background: #e6e8ea; text-align: left; font-weight: 600; }
.data-grid td { padding: 6px 8px; border: 1px solid #d1d1d1; }
.data-grid tbody tr { cursor: pointer; }
.data-grid tbody tr:hover { background: #fff8e7; }
.data-grid tbody tr.selected { background: #ffebad; }
.empty-cell { padding: 19px 8px; color: #777; text-align: center; }
.list-footer { padding: 6px 2px 0; color: #666; font-size: 12px; }
.action-panel { display: flex; flex-direction: column; gap: 11px; }
.current-case { display: grid; grid-template-columns: 1fr 1fr; gap: 7px 12px; padding: 9px; border: 1px solid #ddd; background: #fafafa; }
.current-case > div { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.current-case span { color: #777; font-size: 12px; }
.current-case strong { overflow-wrap: anywhere; font-weight: 500; }
.case-placeholder { padding: 10px; color: #888; }
.option-group { display: flex; gap: 10px 22px; margin: 0; padding: 9px 10px 11px; border: 1px solid #c9c9c9; flex-wrap: wrap; }
.option-group legend { padding: 0 5px; color: #555; }
.radio-option { display: inline-flex; align-items: center; gap: 5px; }
.jump-panel { display: flex; flex-direction: column; gap: 6px; padding: 9px; border: 1px solid #d5d5d5; background: #fafafa; }
.jump-panel > strong { font-weight: 600; }
.jump-panel label { display: flex; align-items: center; gap: 7px; }
.jump-panel label > span { min-width: 108px; }
.group-readonly { flex: 1; background: #fff0b0; }
.jump-panel small { color: #777; }
.description-field { display: flex; flex: 1; flex-direction: column; gap: 5px; }
.description-field textarea { min-height: 92px; resize: vertical; }
.lot-stamp-note { color: #777; font-size: 12px; }
.action-footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: auto; padding-top: 5px; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 40; display: grid; place-items: center; padding: 18px; background: rgb(0 0 0 / 35%); }
.route-dialog { width: min(720px, 96vw); max-height: 90vh; overflow: auto; border: 1px solid #aaa; border-radius: 5px; background: #fff; box-shadow: 0 8px 28px rgb(0 0 0 / 24%); }
.route-dialog > header { display: flex; justify-content: space-between; align-items: center; padding: 12px 15px; border-bottom: 1px solid #ddd; background: #f7f5ef; }
.route-dialog h2 { margin: 0; font-size: 17px; }
.route-dialog header small { display: inline-block; margin-top: 4px; color: #777; }
.icon-close { border: 0; background: transparent; color: #555; cursor: pointer; font-size: 25px; }
.target-list { display: flex; flex-direction: column; gap: 6px; padding: 12px; }
.target-row { display: grid; grid-template-columns: 1fr auto 1fr auto; align-items: center; gap: 12px; min-height: 52px; padding: 8px 10px; border: 1px solid #ccc; background: #fff; color: inherit; text-align: left; cursor: pointer; }
.target-row:hover { border-color: #dd9b2d; background: #fff8e7; }
.target-node { font-weight: 600; }
.target-arrow { color: #777; font-size: 19px; }
.target-group { display: flex; flex-direction: column; gap: 3px; }
.target-group strong { font-weight: 500; }
.target-group small, .target-link { color: #777; font-size: 12px; }
.route-dialog > footer { display: flex; justify-content: flex-end; padding: 10px 14px; border-top: 1px solid #ddd; }
@media (max-width: 900px) {
  .adjust-layout { grid-template-columns: 1fr; }
  .action-panel { min-height: 360px; }
}
@media (max-width: 620px) {
  .query-row, .filter-field { align-items: flex-start; }
  .date-filter { flex-wrap: wrap; }
  .current-case { grid-template-columns: 1fr; }
  .target-row { grid-template-columns: 1fr auto 1fr; }
  .target-link { grid-column: 1 / -1; }
}
</style>
