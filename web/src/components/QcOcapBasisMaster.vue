<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type IssueState = -1 | 0 | 1 | 2
type EditMode = 'add' | 'edit' | 'copy' | null
type NodeKind = 'start' | 'end' | 'ocap' | 'and' | 'or'
interface FlowNode { nodeId: string; nodeNo: string; nodeType: number; kind: NodeKind; groupNo: string; x: number; y: number }
interface FlowLink { fromNodeId: string; toNodeId: string; linkName: string }
interface FlowGraph { nodes: FlowNode[]; links: FlowLink[] }
interface OcapRow {
  ocapNo: string; ocapVersion: string; description: string; issueState: IssueState; curVersion: number
  creator: string; createDate: string; reviser: string | null; reviseDate: string | null
  dataStamp: number | null; flow: FlowGraph | null
}
interface OcapForm { ocapNo: string; ocapVersion: string; description: string }
interface Envelope<T> { items?: T[]; rows?: T[]; data?: T[] }

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:qc-ocap-basis:v1'
const statuses: { value: IssueState; label: string }[] = [
  { value: 0, label: '未簽核' }, { value: 1, label: '簽核中' }, { value: 2, label: '已簽核' }, { value: -1, label: '不使用' },
]

const seedRows: OcapRow[] = [
  { ocapNo: 'DEV-OCAP-01', ocapVersion: '01', description: 'DEV 外觀缺陷處置流程', issueState: 0, curVersion: 0, creator: 'DEV001', createDate: '2026-11-10T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-10T09:15:00+08:00', dataStamp: 2, flow: { nodes: [{ nodeId: 'DEV-OCAP-01-01-0001', nodeNo: 'START', nodeType: 8, kind: 'start', groupNo: '', x: 70, y: 160 }, { nodeId: 'DEV-OCAP-01-01-0002', nodeNo: 'OCAP-01', nodeType: 0, kind: 'ocap', groupNo: 'DEV-HOLD-01', x: 330, y: 160 }, { nodeId: 'DEV-OCAP-01-01-0003', nodeNo: 'END', nodeType: 9, kind: 'end', groupNo: '', x: 590, y: 160 }], links: [{ fromNodeId: 'DEV-OCAP-01-01-0001', toNodeId: 'DEV-OCAP-01-01-0002', linkName: 'pass' }, { fromNodeId: 'DEV-OCAP-01-01-0002', toNodeId: 'DEV-OCAP-01-01-0003', linkName: 'pass' }] } },
  { ocapNo: 'DEV-OCAP-01', ocapVersion: '02', description: 'DEV 外觀缺陷處置流程新版', issueState: 2, curVersion: 1, creator: 'DEV002', createDate: '2026-11-11T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-11T10:30:00+08:00', dataStamp: 3, flow: { nodes: [{ nodeId: 'DEV-OCAP-01-02-0001', nodeNo: 'START', nodeType: 8, kind: 'start', groupNo: '', x: 70, y: 160 }, { nodeId: 'DEV-OCAP-01-02-0002', nodeNo: 'OCAP-02', nodeType: 0, kind: 'ocap', groupNo: 'DEV-HOLD-02', x: 330, y: 160 }, { nodeId: 'DEV-OCAP-01-02-0003', nodeNo: 'END', nodeType: 9, kind: 'end', groupNo: '', x: 590, y: 160 }], links: [{ fromNodeId: 'DEV-OCAP-01-02-0001', toNodeId: 'DEV-OCAP-01-02-0002', linkName: 'pass' }, { fromNodeId: 'DEV-OCAP-01-02-0002', toNodeId: 'DEV-OCAP-01-02-0003', linkName: 'pass' }] } },
  { ocapNo: 'DEV-OCAP-02', ocapVersion: '01', description: 'DEV 製程異常隔離與判定', issueState: 1, curVersion: 0, creator: 'DEV001', createDate: '2026-11-12T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-11-12T09:00:00+08:00', dataStamp: 1, flow: null },
  { ocapNo: 'DEV-OCAP-OLD', ocapVersion: '01', description: 'DEV 舊版流程範例', issueState: -1, curVersion: 0, creator: 'DEV001', createDate: '2026-11-09T08:00:00+08:00', reviser: 'DEV001', reviseDate: '2026-11-09T09:00:00+08:00', dataStamp: 1, flow: null },
]

const rows = ref<OcapRow[]>([])
const issueState = ref<IssueState>(0)
const searchText = ref('')
const appliedSearch = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<EditMode>(null)
const form = ref<OcapForm>(emptyForm())
const formSource = ref<OcapRow | null>(null)
const formError = ref('')
const flowOpen = ref(false)
const flowDraft = ref<FlowGraph>({ nodes: [], links: [] })
const flowNodeId = ref('')
const linkFrom = ref('')
const linkTo = ref('')
const linkName = ref('pass')
const flowError = ref('')

const visibleRows = computed(() => rows.value
  .filter(row => row.issueState === issueState.value)
  .filter(row => row.ocapNo.toLocaleLowerCase().includes(appliedSearch.value.toLocaleLowerCase()))
  .sort((a, b) => a.ocapNo.localeCompare(b.ocapNo) || a.ocapVersion.localeCompare(b.ocapVersion)))
const activeRow = computed(() => visibleRows.value.find(row => rowKey(row) === activeKey.value) ?? null)
const pickedRows = computed(() => visibleRows.value.filter(row => selectedKeys.value.includes(rowKey(row))))
const userNo = computed(() => props.currentUserNo?.trim() || (mock.value ? 'DEV001' : ''))
const editTitle = computed(() => mode.value === 'add' ? '新增 OCAP 基本資料' : mode.value === 'copy' ? '複製 OCAP 基本資料' : '修改 OCAP 基本資料')
const flowSelectedNode = computed(() => flowDraft.value.nodes.find(node => node.nodeId === flowNodeId.value) ?? null)
const flowActiveRow = ref<OcapRow | null>(null)
const flowNodeLabel = (node: FlowNode) => node.kind === 'start' ? '開始' : node.kind === 'end' ? '結束' : node.nodeNo || 'OCAP 節點'

function emptyForm(): OcapForm { return { ocapNo: '', ocapVersion: '', description: '' } }
function rowKey(row: Pick<OcapRow, 'ocapNo' | 'ocapVersion'>): string { return `${row.ocapNo}::${row.ocapVersion}` }
function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function list<T>(value: T[] | Envelope<T>): T[] { return Array.isArray(value) ? value : value.items ?? value.rows ?? value.data ?? [] }
function normalizeRow(value: Record<string, unknown>): OcapRow {
  const field = (name: string) => value[name] ?? value[name[0].toUpperCase() + name.slice(1)]
  const rawFlow = field('flow')
  return {
    ocapNo: String(field('ocapNo') ?? ''), ocapVersion: String(field('ocapVersion') ?? ''),
    description: String(field('description') ?? ''), issueState: Number(field('issueState') ?? 0) as IssueState,
    curVersion: Number(field('curVersion') ?? 0), creator: String(field('creator') ?? ''),
    createDate: String(field('createDate') ?? ''), reviser: (field('reviser') as string | null) ?? null,
    reviseDate: (field('reviseDate') as string | null) ?? null,
    dataStamp: field('dataStamp') == null ? null : Number(field('dataStamp')),
    flow: rawFlow && typeof rawFlow === 'object' ? clone(rawFlow as FlowGraph) : null,
  }
}
function endpoint(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(endpoint(path), { credentials: 'include', ...init })
  if (response.status === 401) { window.location.assign(import.meta.env.BASE_URL + 'login'); throw new Error('登入已逾期，請重新登入。') }
  const body = await response.text()
  if (!response.ok) throw new Error(body || `API 請求失敗 (${response.status})`)
  return (body ? JSON.parse(body) : undefined) as T
}
function persist(): void { localStorage.setItem(storageKey, JSON.stringify(rows.value)) }
function demoRows(): OcapRow[] {
  try {
    const saved = localStorage.getItem(storageKey)
    if (saved) return (JSON.parse(saved) as Record<string, unknown>[]).map(normalizeRow)
  } catch { /* use the bundled development samples */ }
  return clone(seedRows)
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''; notice.value = ''
  try {
    if (mock.value) rows.value = demoRows()
    else {
      const query = new URLSearchParams({ issueState: String(issueState.value) })
      if (searchText.value.trim()) query.set('ocapNo', searchText.value.trim())
      const result = await api<Record<string, unknown>[] | Envelope<Record<string, unknown>>>(`/api/qc/ocap-basis?${query}`)
      rows.value = list(result).map(normalizeRow)
    }
    appliedSearch.value = searchText.value.trim(); activeKey.value = ''; selectedKeys.value = []
    if (!visibleRows.value.length) notice.value = '沒有符合查詢條件的 OCAP 資料。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '載入 OCAP 資料失敗。' }
  finally { loading.value = false }
}
function chooseState(value: IssueState): void { issueState.value = value; void load() }
function selectRow(row: OcapRow): void { activeKey.value = rowKey(row); notice.value = '' }
function toggleSelected(row: OcapRow, checked: boolean): void {
  const key = rowKey(row)
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, key])] : selectedKeys.value.filter(item => item !== key)
  selectRow(row)
}
function openEditor(next: Exclude<EditMode, null>): void {
  if (next !== 'add' && !activeRow.value) { notice.value = '請先選取一筆 OCAP 資料。'; return }
  if (next === 'edit' && activeRow.value?.issueState !== 0) { notice.value = '只有未簽核資料可以修改。'; return }
  formSource.value = next === 'add' ? null : activeRow.value
  const source = formSource.value
  form.value = source
    ? { ocapNo: source.ocapNo, ocapVersion: source.ocapVersion, description: source.description }
    : emptyForm()
  formError.value = ''; mode.value = next
}
function closeEditor(): void { mode.value = null; formError.value = '' }
function copyGraph(source: FlowGraph | null, ocapNo: string, version: string): FlowGraph | null {
  if (!source) return null
  const idMap = new Map<string, string>()
  source.nodes.forEach((node, index) => idMap.set(node.nodeId, `${ocapNo}-${version}-${String(index + 1).padStart(4, '0')}`))
  return { nodes: source.nodes.map((node, index) => ({ ...node, nodeId: idMap.get(node.nodeId)!, x: 70 + (index % 3) * 250, y: 80 + Math.floor(index / 3) * 100 })), links: source.links.map(link => ({ ...link, fromNodeId: idMap.get(link.fromNodeId) ?? link.fromNodeId, toNodeId: idMap.get(link.toNodeId) ?? link.toNodeId })) }
}
async function saveEditor(closeAfter: boolean): Promise<void> {
  formError.value = ''
  const value = { ocapNo: form.value.ocapNo.trim().toUpperCase(), ocapVersion: form.value.ocapVersion.trim().toUpperCase(), description: form.value.description.trim() }
  if (!value.ocapNo || !value.ocapVersion) { formError.value = 'OCAP 編號與版本為必填。'; return }
  if (value.ocapNo.length > 50 || value.ocapVersion.length > 5 || value.description.length > 255) { formError.value = '欄位長度超過資料表限制。'; return }
  const source = formSource.value
  const found = rows.value.some(row => row.ocapNo === value.ocapNo && row.ocapVersion === value.ocapVersion && !(mode.value === 'edit' && row === source))
  if (found) { formError.value = '相同的 OCAP 編號與版本已存在。'; return }
  busy.value = true
  try {
    if (mock.value) {
      const now = new Date().toISOString()
      if (mode.value === 'edit' && source) {
        const target = rows.value.find(row => rowKey(row) === rowKey(source))
        if (!target) throw new Error('資料已不存在，請重新查詢。')
        target.description = value.description; target.reviser = userNo.value || 'DEV001'; target.reviseDate = now; target.dataStamp = (target.dataStamp ?? 0) + 1
      } else {
        const copiedFlow = mode.value === 'copy' ? copyGraph(source?.flow ?? null, value.ocapNo, value.ocapVersion) : null
        rows.value.push({ ...value, issueState: 0, curVersion: 0, creator: userNo.value || 'DEV001', createDate: now, reviser: null, reviseDate: null, dataStamp: 0, flow: copiedFlow })
      }
      persist()
    } else if (mode.value === 'add') {
      await api('/api/qc/ocap-basis', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(value) })
    } else if (mode.value === 'copy' && source) {
      await api(`/api/qc/ocap-basis/${encodeURIComponent(source.ocapNo)}/${encodeURIComponent(source.ocapVersion)}/copy`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ...value, dataStamp: source.dataStamp }) })
    } else if (source) {
      await api(`/api/qc/ocap-basis/${encodeURIComponent(source.ocapNo)}/${encodeURIComponent(source.ocapVersion)}?dataStamp=${source.dataStamp ?? ''}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ description: value.description }) })
    }
    const savedMode = mode.value
    notice.value = savedMode === 'edit' ? 'OCAP 基本資料已修改。' : savedMode === 'copy' ? 'OCAP 版本已複製。' : 'OCAP 基本資料已新增。'
    if (closeAfter) closeEditor()
    else if (savedMode === 'add' || savedMode === 'copy') form.value = emptyForm()
    await load()
    if (!closeAfter && savedMode === 'edit' && source) formSource.value = rows.value.find(row => rowKey(row) === rowKey(source)) ?? source
  } catch (cause) { formError.value = cause instanceof Error ? cause.message : '儲存 OCAP 資料失敗。' }
  finally { busy.value = false }
}
async function deleteSelected(): Promise<void> {
  const targets = pickedRows.value.length ? pickedRows.value : activeRow.value ? [activeRow.value] : []
  if (!targets.length) { notice.value = '請先選取要刪除的 OCAP 資料。'; return }
  if (targets.some(row => row.issueState === 1)) { notice.value = '簽核中的資料不可刪除。'; return }
  if (!window.confirm(`確定刪除選取的 ${targets.length} 筆 OCAP 資料及其流程節點/連線？`)) return
  busy.value = true; error.value = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        if (row.issueState === 2) {
          row.issueState = -1; row.curVersion = 0; row.reviser = userNo.value || 'DEV001'; row.reviseDate = new Date().toISOString(); row.dataStamp = (row.dataStamp ?? 0) + 1
        } else rows.value = rows.value.filter(item => rowKey(item) !== rowKey(row))
      }
      else await api(`/api/qc/ocap-basis/${encodeURIComponent(row.ocapNo)}/${encodeURIComponent(row.ocapVersion)}?issueState=${row.issueState}&dataStamp=${row.dataStamp ?? ''}`, { method: 'DELETE' })
    }
    if (mock.value) persist()
    selectedKeys.value = []; notice.value = `${targets.length} 筆 OCAP 資料已刪除。`; await load()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '刪除 OCAP 資料失敗。' }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = pickedRows.value.length ? pickedRows.value : activeRow.value ? [activeRow.value] : []
  if (!targets.length) { notice.value = '請先選取要核准的 OCAP 資料。'; return }
  const invalid = targets.find(row => row.issueState !== 0 || !row.flow?.nodes.length)
  if (invalid) { notice.value = `版本 ${invalid.ocapNo}/${invalid.ocapVersion} 必須是未簽核且已建立流程，才能核准。`; return }
  if (!window.confirm(`確定核准選取的 ${targets.length} 筆 OCAP 版本？核准後會設為目前版本。`)) return
  busy.value = true; error.value = ''
  try {
    for (const row of targets) {
      if (mock.value) {
        rows.value.forEach(item => { if (item.ocapNo === row.ocapNo) item.curVersion = 0 })
        row.issueState = 2; row.curVersion = 1; row.reviser = userNo.value || 'DEV001'; row.reviseDate = new Date().toISOString(); row.dataStamp = (row.dataStamp ?? 0) + 1
      } else await api(`/api/qc/ocap-basis/${encodeURIComponent(row.ocapNo)}/${encodeURIComponent(row.ocapVersion)}/approve`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, dataStamp: row.dataStamp }) })
    }
    if (mock.value) persist()
    selectedKeys.value = []; notice.value = `${targets.length} 筆 OCAP 資料已核准。`; await load()
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '核准 OCAP 資料失敗。' }
  finally { busy.value = false }
}
function openFlowEditor(): void {
  const row = activeRow.value
  if (!row) { notice.value = '請先選取一筆 OCAP 資料。'; return }
  if (row.issueState !== 0) { notice.value = '只有未簽核資料可以繪製流程。'; return }
  flowActiveRow.value = row; flowDraft.value = clone(row.flow ?? { nodes: [], links: [] }); flowNodeId.value = flowDraft.value.nodes[0]?.nodeId ?? ''
  linkFrom.value = flowDraft.value.nodes[0]?.nodeId ?? ''; linkTo.value = flowDraft.value.nodes[1]?.nodeId ?? ''; flowError.value = ''; flowOpen.value = true
}
function addFlowNode(kind: NodeKind): void {
  const ordinal = flowDraft.value.nodes.length + 1
  const nodeType: Record<NodeKind, number> = { ocap: 0, and: 6, or: 7, start: 8, end: 9 }
  const nodeNo = kind === 'start' ? 'START' : kind === 'end' ? 'END' : `${kind === 'ocap' ? 'OCAP' : kind.toUpperCase()}-${String(ordinal).padStart(2, '0')}`
  const node: FlowNode = { nodeId: `new-${Date.now()}-${ordinal}`, nodeNo, nodeType: nodeType[kind], kind, groupNo: '', x: 70 + ((ordinal - 1) % 3) * 250, y: 70 + Math.floor((ordinal - 1) / 3) * 100 }
  flowDraft.value.nodes.push(node); flowNodeId.value = node.nodeId
  if (!linkFrom.value) linkFrom.value = node.nodeId
}
function removeFlowNode(): void {
  if (!flowSelectedNode.value) return
  const id = flowSelectedNode.value.nodeId
  flowDraft.value.nodes = flowDraft.value.nodes.filter(node => node.nodeId !== id)
  flowDraft.value.links = flowDraft.value.links.filter(link => link.fromNodeId !== id && link.toNodeId !== id)
  flowNodeId.value = flowDraft.value.nodes[0]?.nodeId ?? ''; linkFrom.value = flowDraft.value.nodes[0]?.nodeId ?? ''; linkTo.value = flowDraft.value.nodes[1]?.nodeId ?? ''
}
function addFlowLink(): void {
  if (!linkFrom.value || !linkTo.value || linkFrom.value === linkTo.value) { flowError.value = '請指定不同的起點與終點。'; return }
  if (flowDraft.value.links.some(link => link.fromNodeId === linkFrom.value && link.toNodeId === linkTo.value && link.linkName === linkName.value)) { flowError.value = '相同的流程連線已存在。'; return }
  flowDraft.value.links.push({ fromNodeId: linkFrom.value, toNodeId: linkTo.value, linkName: linkName.value }); flowError.value = ''
}
function removeFlowLink(index: number): void { flowDraft.value.links.splice(index, 1) }
function nodePosition(nodeId: string): FlowNode | undefined { return flowDraft.value.nodes.find(node => node.nodeId === nodeId) }
function nodeFill(kind: NodeKind): string { return kind === 'start' || kind === 'end' ? '#fff0b8' : kind === 'and' || kind === 'or' ? '#e7eef5' : '#fff8d8' }
async function saveFlow(): Promise<void> {
  const row = flowActiveRow.value
  if (!row) return
  flowError.value = ''
  if (flowDraft.value.nodes.some(node => !node.nodeNo.trim() || node.nodeNo.length > 50 || node.groupNo.length > 30)) { flowError.value = '節點編號必填且不可超過 50 字元；群組編號不可超過 30 字元。'; return }
  busy.value = true
  try {
    if (mock.value) {
      const target = rows.value.find(item => rowKey(item) === rowKey(row))
      if (!target) throw new Error('OCAP 資料已不存在，請重新查詢。')
      target.flow = clone(flowDraft.value); target.dataStamp = (target.dataStamp ?? 0) + 1; target.reviser = userNo.value || 'DEV001'; target.reviseDate = new Date().toISOString(); persist()
    } else await api(`/api/qc/ocap-basis/${encodeURIComponent(row.ocapNo)}/${encodeURIComponent(row.ocapVersion)}/flow`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ dataStamp: row.dataStamp, flow: flowDraft.value }) })
    flowOpen.value = false; notice.value = '流程節點與連線已儲存。'; await load()
  } catch (cause) { flowError.value = cause instanceof Error ? cause.message : '儲存流程圖失敗。' }
  finally { busy.value = false }
}
function formatDate(value: string | null): string { return value ? new Date(value).toLocaleString('zh-TW', { hour12: false }) : '' }
function stateLabel(value: IssueState): string { return statuses.find(item => item.value === value)?.label ?? String(value) }
onMounted(() => { void load() })
</script>

<template>
  <main class="ocap-page">
    <header class="page-title"><h1>OCAP基本資料</h1><span>異常設定</span></header>

    <section class="search-panel" aria-label="查詢條件">
      <div class="status-options">
        <label v-for="item in statuses" :key="item.value" class="status-option">
          <input type="radio" :checked="issueState === item.value" @change="chooseState(item.value)"><span>{{ item.label }}</span>
        </label>
      </div>
      <form class="search-form" @submit.prevent="load">
        <label for="ocap-search">OCAP 編號</label>
        <input id="ocap-search" v-model="searchText" class="kd-field search-input" maxlength="50" @keyup.enter="load">
        <button class="button" type="submit" :disabled="loading">查詢</button>
      </form>
    </section>

    <section class="list-panel">
      <div class="toolbar">
        <button class="button primary" type="button" :disabled="busy" @click="openEditor('add')">新增(A)</button>
        <button class="button" type="button" :disabled="busy || !activeRow || activeRow.issueState !== 0" @click="openEditor('edit')">修改(E)</button>
        <button class="button" type="button" :disabled="busy || !activeRow" @click="openEditor('copy')">複製(C)</button>
        <button class="button" type="button" :disabled="busy || (!activeRow && !pickedRows.length)" @click="approveSelected">核准</button>
        <button class="button" type="button" :disabled="busy || (!activeRow && !pickedRows.length)" @click="deleteSelected">刪除(D)</button>
        <button class="button" type="button" :disabled="busy || !activeRow || activeRow.issueState !== 0" @click="openFlowEditor">Draw</button>
        <span class="toolbar-spacer"></span><button class="button" type="button" @click="emit('close')">關閉</button>
      </div>

      <div v-if="error" class="message error-message" role="alert">{{ error }}</div>
      <div v-else-if="notice" class="message" role="status">{{ notice }}</div>
      <div class="table-wrap">
        <table class="data-grid">
          <thead><tr>
            <th class="select-col">選取</th><th>OCAP編號</th><th>版本</th><th>目前版本</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th><th>狀態</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in visibleRows" :key="rowKey(row)" :class="{ selected: activeKey === rowKey(row) }" @click="selectRow(row)">
              <td class="select-col"><input type="checkbox" :checked="selectedKeys.includes(rowKey(row))" :aria-label="`選取 ${row.ocapNo} ${row.ocapVersion}`" @click.stop @change="toggleSelected(row, ($event.target as HTMLInputElement).checked)"></td>
              <td>{{ row.ocapNo }}</td><td>{{ row.ocapVersion }}</td><td>{{ row.curVersion === 1 ? '是' : '否' }}</td><td>{{ row.description }}</td>
              <td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ formatDate(row.reviseDate) }}</td><td>{{ stateLabel(row.issueState) }}</td>
            </tr>
            <tr v-if="!loading && visibleRows.length === 0"><td colspan="10" class="empty-cell">沒有資料</td></tr>
            <tr v-if="loading"><td colspan="10" class="empty-cell">載入中…</td></tr>
          </tbody>
        </table>
      </div>
      <footer class="list-footer"><span>資料總筆數：{{ visibleRows.length }}</span><span v-if="activeRow">目前選取：{{ activeRow.ocapNo }} / {{ activeRow.ocapVersion }}</span></footer>
    </section>

    <section class="preview-panel">
      <div class="preview-heading"><h2>流程預覽</h2><span v-if="activeRow">{{ activeRow.ocapNo }} / {{ activeRow.ocapVersion }}</span></div>
      <template v-if="activeRow?.flow?.nodes.length">
        <div class="flow-preview">
          <template v-for="(node, index) in activeRow.flow.nodes" :key="node.nodeId">
            <div class="preview-node" :class="`node-${node.kind}`"><strong>{{ flowNodeLabel(node) }}</strong><small v-if="node.groupNo">群組：{{ node.groupNo }}</small></div>
            <span v-if="index < activeRow.flow.nodes.length - 1" class="preview-arrow">→</span>
          </template>
        </div>
        <small class="link-summary">{{ activeRow.flow.links.length }} 條流程連線</small>
      </template>
      <p v-else class="empty-preview">{{ activeRow ? '尚未繪製流程。' : '選取資料列以檢視流程。' }}</p>
    </section>

    <div v-if="mode" class="dialog-backdrop" @click.self="closeEditor">
      <section class="dialog-card edit-dialog" role="dialog" aria-modal="true" :aria-label="editTitle">
        <header class="dialog-header"><h2>{{ editTitle }}</h2><button class="icon-close" type="button" aria-label="關閉" @click="closeEditor">×</button></header>
        <div class="edit-fields">
          <label><span>OCAP 編號</span><input v-model="form.ocapNo" class="kd-field" maxlength="50" :readonly="mode === 'edit'" :disabled="busy"></label>
          <label><span>OCAP 版本</span><input v-model="form.ocapVersion" class="kd-field" maxlength="5" :readonly="mode === 'edit'" :disabled="busy"></label>
          <label class="wide"><span>說明</span><textarea v-model="form.description" class="kd-field" maxlength="255" rows="5" :disabled="busy"></textarea></label>
        </div>
        <div v-if="formSource" class="audit-strip"><span>建立人：{{ formSource.creator }}</span><span>建立日期：{{ formatDate(formSource.createDate) }}</span><span>修改人：{{ formSource.reviser || '—' }}</span><span>修改日期：{{ formatDate(formSource.reviseDate) || '—' }}</span></div>
        <p v-if="formError" class="message error-message" role="alert">{{ formError }}</p>
        <footer class="dialog-actions"><button class="button primary" type="button" :disabled="busy" @click="saveEditor(true)">確定</button><button class="button" type="button" :disabled="busy" @click="closeEditor">關閉</button><button class="button" type="button" :disabled="busy" @click="saveEditor(false)">套用</button></footer>
      </section>
    </div>

    <div v-if="flowOpen" class="dialog-backdrop flow-backdrop" @click.self="flowOpen = false">
      <section class="dialog-card flow-dialog" role="dialog" aria-modal="true" aria-label="OCAP 流程繪製">
        <header class="dialog-header"><div><h2>流程繪製</h2><small>{{ flowActiveRow?.ocapNo }} / {{ flowActiveRow?.ocapVersion }}</small></div><button class="icon-close" type="button" aria-label="關閉" @click="flowOpen = false">×</button></header>
        <div class="flow-tools">
          <span>新增節點</span>
          <button class="button" type="button" @click="addFlowNode('start')">開始</button><button class="button" type="button" @click="addFlowNode('ocap')">OCAP 節點</button><button class="button" type="button" @click="addFlowNode('end')">結束</button>
          <button class="button" type="button" @click="addFlowNode('and')">AND 群組</button><button class="button" type="button" @click="addFlowNode('or')">OR 群組</button>
          <button class="button danger-button" type="button" :disabled="!flowSelectedNode" @click="removeFlowNode">刪除選取節點</button>
        </div>
        <div class="flow-workspace">
          <div class="flow-canvas">
            <svg viewBox="0 0 720 330" role="img" aria-label="流程節點與連線圖">
              <defs><marker id="ocap-arrow" markerWidth="9" markerHeight="7" refX="8" refY="3.5" orient="auto"><polygon points="0 0, 9 3.5, 0 7" fill="#727b84" /></marker></defs>
              <g v-for="(link, index) in flowDraft.links" :key="`${link.fromNodeId}-${link.toNodeId}-${index}`">
                <line v-if="nodePosition(link.fromNodeId) && nodePosition(link.toNodeId)" :x1="(nodePosition(link.fromNodeId)?.x ?? 0) + 58" :y1="(nodePosition(link.fromNodeId)?.y ?? 0) + 24" :x2="nodePosition(link.toNodeId)?.x ?? 0" :y2="(nodePosition(link.toNodeId)?.y ?? 0) + 24" stroke="#727b84" stroke-width="2" marker-end="url(#ocap-arrow)" />
                <text v-if="nodePosition(link.fromNodeId) && nodePosition(link.toNodeId)" :x="((nodePosition(link.fromNodeId)?.x ?? 0) + (nodePosition(link.toNodeId)?.x ?? 0)) / 2 + 28" :y="((nodePosition(link.fromNodeId)?.y ?? 0) + (nodePosition(link.toNodeId)?.y ?? 0)) / 2 + 18" class="link-label">{{ link.linkName }}</text>
              </g>
              <g v-for="node in flowDraft.nodes" :key="node.nodeId" class="flow-node" :class="{ 'node-active': flowNodeId === node.nodeId }" @click="flowNodeId = node.nodeId">
                <rect :x="node.x" :y="node.y" width="116" height="48" rx="6" :fill="nodeFill(node.kind)" :stroke="flowNodeId === node.nodeId ? '#d58414' : '#888'" stroke-width="2" />
                <text :x="node.x + 58" :y="node.y + 21" text-anchor="middle">{{ flowNodeLabel(node) }}</text>
                <text v-if="node.groupNo" :x="node.x + 58" :y="node.y + 38" text-anchor="middle" class="node-subtitle">{{ node.groupNo }}</text>
              </g>
              <text v-if="!flowDraft.nodes.length" x="360" y="165" text-anchor="middle" class="canvas-placeholder">從上方工具列新增流程節點</text>
            </svg>
          </div>
          <aside class="flow-inspector">
            <h3>節點屬性</h3>
            <template v-if="flowSelectedNode">
              <label><span>節點編號</span><input v-model="flowSelectedNode.nodeNo" class="kd-field" maxlength="50" :readonly="flowSelectedNode.kind === 'start' || flowSelectedNode.kind === 'end'"></label>
              <label v-if="flowSelectedNode.kind === 'ocap'"><span>異常處理群組</span><input v-model="flowSelectedNode.groupNo" class="kd-field" maxlength="30" placeholder="輸入已簽核 Group No"></label>
              <label v-if="flowSelectedNode.kind === 'and' || flowSelectedNode.kind === 'or'"><span>群組編號</span><input v-model="flowSelectedNode.groupNo" class="kd-field" maxlength="30"></label>
              <button class="button" type="button" :disabled="!flowSelectedNode.groupNo" @click="flowSelectedNode.groupNo = ''">清除群組</button>
            </template>
            <p v-else class="empty-preview">選取流程節點以編輯屬性。</p>
            <hr>
            <h3>新增連線</h3>
            <label><span>起點</span><select v-model="linkFrom" class="kd-field"><option value="">請選擇</option><option v-for="node in flowDraft.nodes" :key="node.nodeId" :value="node.nodeId">{{ flowNodeLabel(node) }}</option></select></label>
            <label><span>終點</span><select v-model="linkTo" class="kd-field"><option value="">請選擇</option><option v-for="node in flowDraft.nodes" :key="node.nodeId" :value="node.nodeId">{{ flowNodeLabel(node) }}</option></select></label>
            <label><span>連線名稱</span><select v-model="linkName" class="kd-field"><option value="pass">pass</option><option value="fail">fail</option><option value="是">是</option><option value="否">否</option></select></label>
            <button class="button" type="button" @click="addFlowLink">新增連線</button>
            <ul class="link-list"><li v-for="(link, index) in flowDraft.links" :key="`${link.fromNodeId}-${link.toNodeId}-${index}`"><span>{{ flowNodeLabel(nodePosition(link.fromNodeId) ?? { nodeId: '', nodeNo: link.fromNodeId, nodeType: 0, kind: 'ocap', groupNo: '', x: 0, y: 0 }) }} → {{ flowNodeLabel(nodePosition(link.toNodeId) ?? { nodeId: '', nodeNo: link.toNodeId, nodeType: 0, kind: 'ocap', groupNo: '', x: 0, y: 0 }) }} ({{ link.linkName }})</span><button type="button" class="text-button" @click="removeFlowLink(index)">移除</button></li></ul>
          </aside>
        </div>
        <p v-if="flowError" class="message error-message" role="alert">{{ flowError }}</p>
        <footer class="dialog-actions"><button class="button primary" type="button" :disabled="busy" @click="saveFlow">確定</button><button class="button" type="button" :disabled="busy" @click="flowOpen = false">關閉</button></footer>
      </section>
    </div>
  </main>
</template>

<style scoped>
.ocap-page { min-height: 100%; padding: 0 5px 16px; color: #353535; font: 14px Arial, "Microsoft JhengHei", sans-serif; }
.page-title { display: flex; align-items: center; gap: 14px; min-height: 58px; padding: 0 24px; border-bottom: 1px solid #dedbd3; background: #f7f5ef; }
.page-title h1 { margin: 0; color: #342d22; font-size: 21px; font-weight: 600; }
.page-title > span { color: #827b70; }
.search-panel, .list-panel, .preview-panel { margin-top: 8px; border: 1px solid #c9c9c9; background: #fff; }
.search-panel { display: flex; min-height: 42px; align-items: center; gap: 22px; padding: 6px 12px; flex-wrap: wrap; }
.status-options { display: flex; align-items: center; gap: 16px; }
.status-option { display: inline-flex; align-items: center; gap: 4px; white-space: nowrap; }
.status-option input { accent-color: #ee981d; }
.search-form { display: flex; align-items: center; gap: 7px; }
.search-form label { white-space: nowrap; }
.kd-field { min-height: 30px; border: 1px solid #949494; border-radius: 2px; padding: 4px 7px; background-color: #fff0b0; color: #303030; font: inherit; }
.kd-field:focus { outline: 2px solid #e0a031; outline-offset: 1px; }
.kd-field:read-only, .kd-field:disabled { background: #eee; color: #777; }
.search-input { width: 220px; background: #fff; }
.button { min-height: 30px; padding: 4px 12px; border: 1px solid #bbb; border-radius: 4px; background: linear-gradient(#fff, #f3f3f3); color: #333; cursor: pointer; font: inherit; white-space: nowrap; }
.button:hover:not(:disabled) { border-color: #dd9b2d; background: #fff8e7; }
.button:disabled { color: #aaa; cursor: not-allowed; }
.button.primary { border-color: #f1a32c; background: #ff9f1c; color: #fff; }
.button.primary:hover:not(:disabled) { background: #ec8e0a; }
.danger-button { color: #8d3026; }
.toolbar { display: flex; align-items: center; gap: 5px; min-height: 40px; padding: 5px 7px; border-bottom: 1px solid #d3d3d3; flex-wrap: wrap; }
.toolbar-spacer { flex: 1; }
.table-wrap { overflow: auto; max-height: min(52vh, 520px); }
.data-grid { width: 100%; border-collapse: collapse; white-space: nowrap; }
.data-grid th { position: sticky; top: 0; z-index: 1; padding: 6px 9px; border: 1px solid #d1d1d1; background: #e6e8ea; color: #333; text-align: left; font-weight: 600; }
.data-grid td { padding: 5px 9px; border: 1px solid #d1d1d1; }
.data-grid tbody tr { cursor: pointer; }
.data-grid tbody tr:hover { background: #fff8e7; }
.data-grid tbody tr.selected { background: #ffebad; }
.data-grid input[type="checkbox"] { accent-color: #e99b21; }
.select-col { width: 52px; text-align: center !important; }
.empty-cell { height: 52px; color: #777; text-align: center; }
.list-footer { display: flex; justify-content: space-between; gap: 12px; padding: 7px 9px; color: #666; font-size: 12px; }
.preview-panel { padding: 9px 12px 12px; }
.preview-heading { display: flex; align-items: center; gap: 12px; }
.preview-heading h2 { margin: 0; color: #4c4c4c; font-size: 15px; }
.preview-heading span, .link-summary { color: #777; font-size: 12px; }
.flow-preview { display: flex; align-items: center; gap: 12px; min-height: 78px; overflow-x: auto; padding: 12px 2px 5px; }
.preview-node { display: flex; min-width: 126px; flex-direction: column; align-items: center; gap: 4px; padding: 10px 12px; border: 1px solid #9c9c9c; border-radius: 5px; background: #fff8d8; text-align: center; }
.preview-node.node-start, .preview-node.node-end { border-radius: 20px; background: #fff0b8; }
.preview-node small { color: #777; }
.preview-arrow { color: #737373; font-size: 21px; }
.empty-preview { margin: 12px 0 4px; color: #888; }
.message { margin: 0; padding: 7px 9px; color: #62522e; background: #fff9e8; }
.error-message { color: #8c251e; background: #fff0ed; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 30; display: grid; place-items: center; overflow: auto; padding: 20px; background: rgb(0 0 0 / 35%); }
.dialog-card { width: min(680px, 96vw); max-height: 95vh; overflow: auto; border: 1px solid #aaa; border-radius: 5px; background: #fff; box-shadow: 0 8px 28px rgb(0 0 0 / 24%); }
.dialog-header { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid #d4d4d4; background: #f7f5ef; }
.dialog-header h2 { margin: 0; font-size: 18px; font-weight: 600; }
.dialog-header small { display: inline-block; margin-top: 4px; color: #777; }
.icon-close { border: 0; background: transparent; color: #555; cursor: pointer; font-size: 25px; line-height: 1; }
.edit-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 14px 18px; padding: 18px; }
.edit-fields label, .flow-inspector label { display: flex; flex-direction: column; gap: 5px; }
.edit-fields label > span, .flow-inspector label > span { color: #555; }
.edit-fields .wide { grid-column: 1 / -1; }
.edit-fields textarea { resize: vertical; }
.audit-strip { display: grid; grid-template-columns: 1fr 1fr; gap: 7px 14px; margin: 0 18px 14px; padding: 10px; border: 1px solid #d5d5d5; background: #f7f7f7; color: #666; font-size: 12px; }
.dialog-actions { display: flex; justify-content: flex-end; gap: 8px; padding: 12px 16px; border-top: 1px solid #ddd; }
.edit-dialog > .message, .flow-dialog > .message { margin: 0 16px 8px; }
.flow-backdrop { z-index: 35; }
.flow-dialog { width: min(1180px, 98vw); }
.flow-tools { display: flex; align-items: center; gap: 6px; padding: 9px 12px; border-bottom: 1px solid #ddd; flex-wrap: wrap; }
.flow-tools > span { margin-right: 4px; color: #555; }
.flow-workspace { display: grid; grid-template-columns: minmax(0, 1fr) 280px; min-height: 380px; }
.flow-canvas { min-width: 0; margin: 12px; border: 1px solid #c7c7c7; background-color: #fff; background-image: radial-gradient(#ddd 0.8px, transparent 0.8px); background-size: 18px 18px; }
.flow-canvas svg { display: block; width: 100%; min-height: 330px; }
.flow-canvas text { fill: #393939; font: 13px Arial, "Microsoft JhengHei", sans-serif; pointer-events: none; }
.flow-canvas .node-subtitle { fill: #777; font-size: 10px; }
.flow-canvas .link-label { fill: #656565; font-size: 11px; }
.flow-canvas .canvas-placeholder { fill: #888; font-size: 14px; }
.flow-node { cursor: pointer; }
.flow-inspector { display: flex; flex-direction: column; gap: 9px; padding: 12px; border-left: 1px solid #ddd; }
.flow-inspector h3 { margin: 2px 0; font-size: 14px; }
.flow-inspector hr { width: 100%; border: 0; border-top: 1px solid #ddd; }
.flow-inspector .button { align-self: flex-start; }
.link-list { display: flex; max-height: 120px; flex-direction: column; gap: 5px; overflow: auto; margin: 0; padding: 0; list-style: none; font-size: 12px; }
.link-list li { display: flex; justify-content: space-between; gap: 6px; }
.text-button { padding: 0; border: 0; background: transparent; color: #8b5a10; cursor: pointer; }
@media (max-width: 800px) {
  .flow-workspace { grid-template-columns: 1fr; }
  .flow-inspector { border-top: 1px solid #ddd; border-left: 0; }
  .flow-canvas svg { min-width: 640px; }
  .flow-canvas { overflow: auto; }
  .edit-fields { grid-template-columns: 1fr; }
  .edit-fields .wide { grid-column: auto; }
}
</style>
