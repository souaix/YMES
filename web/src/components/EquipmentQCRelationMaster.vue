<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type RelationMode = 'new' | 'edit' | 'copy' | null
interface QcListOption { qcListNo: string; qcListName: string; qcType: number }
interface Relation {
  equipmentNo: string; qcListNo: string; qcListName: string; qcType: number
  inspType: number; inspStartTime: string; creator: string; createDate: string
  reviser: string | null; reviseDate: string | null; dataStamp: number
}
interface RelationForm { qcListNo: string; inspType: number; inspStartTime: string }
interface RelationKey { qcListNo: string; inspType: number; inspStartTime: string }

const props = defineProps<{ equipmentNo: string; equipmentType: string; equipmentName: string; apiBaseUrl: string; currentUserNo?: string }>()
const emit = defineEmits<{ close: []; updated: [] }>()
const mock = import.meta.env.DEV
const relationStorageKey = 'ymes-dev-mock:equipment-qc-relations:v1'
const qcListStorageKey = 'ymes-dev-mock:equipment-qc-lists:v1'
const seedQcLists: QcListOption[] = [
  { qcListNo: 'DEV-EQP-QC-01', qcListName: 'DEV 假資料：開機點檢表', qcType: 0 },
  { qcListNo: 'DEV-EQP-QC-02', qcListName: 'DEV 假資料：設備巡檢表', qcType: 2 },
]
const seedRelations: Record<string, Relation[]> = {
  'DEV-1AP-01': [{ equipmentNo: 'DEV-1AP-01', qcListNo: 'DEV-EQP-QC-01', qcListName: 'DEV 假資料：開機點檢表', qcType: 0, inspType: 2, inspStartTime: '08:00', creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null, dataStamp: 0 }],
}
const timeOptions = Array.from({ length: 48 }, (_, i) => `${String(Math.floor(i / 2)).padStart(2, '0')}:${i % 2 ? '30' : '00'}`)
const rows = ref<Relation[]>([])
const qcLists = ref<QcListOption[]>([])
const keyword = ref('')
const appliedKeyword = ref('')
const activeKey = ref('')
const selectedKeys = ref<string[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<RelationMode>(null)
const oldKey = ref<RelationKey | null>(null)
const form = ref<RelationForm>(emptyForm())

function emptyForm(): RelationForm { return { qcListNo: '', inspType: 0, inspStartTime: '00:00' } }
function relationKey(row: RelationKey): string { return `${row.qcListNo}\u001f${row.inspType}\u001f${row.inspStartTime || 'N/A'}` }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), `${base.replace(/\/+$/, '')}/`).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function read<T>(key: string, seed: T): T {
  try { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) as T : structuredClone(seed) }
  catch { return structuredClone(seed) }
}
function write<T>(key: string, value: T): void { localStorage.setItem(key, JSON.stringify(value)) }
const visibleRows = computed(() => rows.value.filter((row) => !appliedKeyword.value || row.qcListNo.toLocaleLowerCase().includes(appliedKeyword.value)))
const selected = computed(() => rows.value.find((row) => relationKey(row) === activeKey.value) ?? null)
const selectedQcList = computed(() => qcLists.value.find((item) => item.qcListNo === form.value.qcListNo) ?? null)
const scheduledInspection = computed(() => selectedQcList.value?.qcType === 0)
const supportedQcType = computed(() => selectedQcList.value?.qcType === 0 || selectedQcList.value?.qcType === 2)
const issueTypeLabel = (value: number) => ({ [-1]: 'None', 0: 'Lot', 1: 'Shift', 2: 'Daily' }[value] ?? String(value))
const qcTypeLabel = (value: number) => value === 0 ? 'CheckIn' : value === 2 ? 'Manual' : `未對照型態 (${value})`
function formatDate(value: string | null): string { return value ? new Date(value).toLocaleString() : '' }
function search(): void { appliedKeyword.value = keyword.value.trim().toLocaleLowerCase() }
function select(row: Relation): void { activeKey.value = relationKey(row) }
function toggleSelected(row: Relation, checked: boolean): void {
  const key = relationKey(row)
  selectedKeys.value = checked ? [...new Set([...selectedKeys.value, key])] : selectedKeys.value.filter((item) => item !== key)
}
watch(() => form.value.qcListNo, () => {
  if (selectedQcList.value?.qcType === 2) { form.value.inspType = -1; form.value.inspStartTime = 'N/A' }
  else if (selectedQcList.value?.qcType === 0 && form.value.inspType < 0) { form.value.inspType = 0; form.value.inspStartTime = '00:00' }
})
function openForm(nextMode: Exclude<RelationMode, null>, row?: Relation): void {
  mode.value = nextMode; error.value = ''; notice.value = ''
  if (nextMode === 'new' || !row) {
    oldKey.value = null; form.value = emptyForm()
  } else {
    oldKey.value = { qcListNo: row.qcListNo, inspType: row.inspType, inspStartTime: row.inspStartTime }
    form.value = { qcListNo: row.qcListNo, inspType: row.inspType, inspStartTime: row.inspStartTime }
  }
}
function normalizeKey(value: RelationForm): RelationKey {
  const type = selectedQcList.value?.qcType === 2 ? -1 : value.inspType
  return { qcListNo: value.qcListNo, inspType: type, inspStartTime: type === 2 ? value.inspStartTime : 'N/A' }
}
function currentRelations(): Record<string, Relation[]> { return read(relationStorageKey, seedRelations) }
function validate(): string {
  if (!form.value.qcListNo) return '請選擇點檢表編號。'
  if (!selectedQcList.value) return '所選點檢表不存在，請重新選擇。'
  if (!supportedQcType.value) return `QC Type ${selectedQcList.value.qcType} 尚未從 WinForms ValueList 核實，不能設定排程。`
  if (scheduledInspection.value && ![0, 1, 2].includes(form.value.inspType)) return '請選擇點檢週期。'
  if (scheduledInspection.value && form.value.inspType === 2 && !timeOptions.includes(form.value.inspStartTime)) return 'Daily 起始時間需為每 30 分鐘一個時段。'
  return ''
}
async function save(): Promise<void> {
  if (busy.value) return
  error.value = validate()
  if (error.value) return
  const nextKey = normalizeKey(form.value)
  const existing = rows.value.find((row) => relationKey(row) === relationKey(nextKey) && (mode.value !== 'edit' || relationKey(row) !== relationKey(oldKey.value!)))
  if (existing) { error.value = '相同點檢表、點檢週期與起始時間的關聯已存在。'; return }
  busy.value = true
  try {
    if (mock) {
      const all = currentRelations()
      const list = [...(all[props.equipmentNo] ?? seedRelations[props.equipmentNo] ?? [])]
      const now = new Date().toISOString()
      const oldIndex = mode.value === 'edit' && oldKey.value ? list.findIndex((item) => relationKey(item) === relationKey(oldKey.value!)) : -1
      const previous = oldIndex >= 0 ? list[oldIndex] : null
      const item: Relation = {
        equipmentNo: props.equipmentNo, ...nextKey, qcListName: selectedQcList.value!.qcListName,
        qcType: selectedQcList.value!.qcType, creator: previous?.creator ?? props.currentUserNo ?? 'DEV001',
        createDate: previous?.createDate ?? now, reviser: previous ? (props.currentUserNo ?? 'DEV001') : null,
        reviseDate: previous ? now : null, dataStamp: (previous?.dataStamp ?? -1) + 1,
      }
      if (oldIndex >= 0) list.splice(oldIndex, 1, item); else list.push(item)
      all[props.equipmentNo] = list; write(relationStorageKey, all)
    } else {
      const base = `api/equipment/${encodeURIComponent(props.equipmentNo)}/qc-lists`
      const body = JSON.stringify({ relation: { ...nextKey, reviser: props.currentUserNo || '' }, oldKey: mode.value === 'edit' ? oldKey.value : null })
      await request<void>(base, { method: mode.value === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body })
    }
    mode.value = null; await load(); emit('updated')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備點檢表關聯儲存失敗。' }
  finally { busy.value = false }
}
async function removeSelected(): Promise<void> {
  const targets = rows.value.filter((row) => selectedKeys.value.includes(relationKey(row)))
  if (!targets.length && selected.value) targets.push(selected.value)
  if (!targets.length || !window.confirm(`確定刪除選取的 ${targets.length} 筆設備點檢表關聯？`)) return
  busy.value = true; error.value = ''
  try {
    if (mock) {
      const all = currentRelations()
      const remove = new Set(targets.map(relationKey))
      all[props.equipmentNo] = (all[props.equipmentNo] ?? seedRelations[props.equipmentNo] ?? []).filter((row) => !remove.has(relationKey(row)))
      write(relationStorageKey, all)
    } else {
      const base = `api/equipment/${encodeURIComponent(props.equipmentNo)}/qc-lists`
      for (const row of targets) {
        const query = new URLSearchParams({ qcListNo: row.qcListNo, inspType: String(row.inspType), inspStartTime: row.inspStartTime })
        await request<void>(`${base}?${query}`, { method: 'DELETE' })
      }
    }
    selectedKeys.value = []; await load(); emit('updated')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備點檢表關聯刪除失敗。' }
  finally { busy.value = false }
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mock) {
      const list = read<QcListOption[]>('ymes-dev-mock:equipment-qc-lists:v1', [])
      qcLists.value = list.length ? list : structuredClone(seedQcLists)
      rows.value = read<Record<string, Relation[]>>(relationStorageKey, seedRelations)[props.equipmentNo] ?? seedRelations[props.equipmentNo] ?? []
    } else {
      const [relations, options] = await Promise.all([
        request<Relation[]>(`api/equipment/${encodeURIComponent(props.equipmentNo)}/qc-lists`),
        request<QcListOption[]>(`api/equipment/${encodeURIComponent(props.equipmentNo)}/qc-list-options`),
      ])
      rows.value = relations; qcLists.value = options
    }
    rows.value = rows.value.slice().sort((a, b) => a.qcListNo.localeCompare(b.qcListNo) || a.inspType - b.inspType || a.inspStartTime.localeCompare(b.inspStartTime))
    selectedKeys.value = selectedKeys.value.filter((key) => rows.value.some((row) => relationKey(row) === key))
    if (!rows.value.some((row) => relationKey(row) === activeKey.value)) activeKey.value = rows.value[0] ? relationKey(rows.value[0]) : ''
  } catch (e) { rows.value = []; error.value = e instanceof Error ? e.message : '設備點檢表關聯載入失敗。' }
  finally { loading.value = false }
}
onMounted(() => { void load() })
</script>

<template>
  <div class="eqp-qc-relation-overlay" @click.self="emit('close')">
    <section class="eqp-qc-relation-window" role="dialog" aria-modal="true" aria-label="設備點檢表關聯">
      <header class="eqp-qc-relation-title"><strong>設備點檢表關聯</strong><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
      <p v-if="mock" class="eqp-qc-relation-dev">DEV 假資料｜操作只儲存在此瀏覽器，不會寫入資料庫。</p>
      <fieldset class="eqp-qc-relation-device"><legend>設備</legend><label>設備類別 <input :value="equipmentType" readonly></label><label>設備編號 <input :value="equipmentNo" readonly></label><label>設備名稱 <input :value="equipmentName" readonly></label></fieldset>
      <form class="eqp-qc-relation-search" @submit.prevent="search"><label>點檢表編號 <input v-model="keyword" type="search"></label><button type="submit">查詢</button></form>
      <p v-if="error" class="eqp-qc-relation-error" role="alert">{{ error }}</p><p v-if="notice" class="eqp-qc-relation-notice" role="status">{{ notice }}</p>
      <div class="eqp-qc-relation-actions"><button class="primary" type="button" @click="openForm('new')">新增(A)</button><button type="button" :disabled="!selected || busy" @click="openForm('edit', selected || undefined)">修改(E)</button><button type="button" :disabled="!selected || busy" @click="openForm('copy', selected || undefined)">複製(C)</button><button type="button" class="danger" :disabled="(!selected && !selectedKeys.length) || busy" @click="removeSelected">刪除(D)</button><span class="spacer"></span><button type="button" @click="emit('close')">關閉</button></div>
      <div class="eqp-qc-relation-grid-wrap" :aria-busy="loading"><table class="eqp-qc-relation-grid"><thead><tr><th></th><th>點檢表編號</th><th>點檢表名稱</th><th>QC Type</th><th>點檢週期</th><th>起始時間</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th></tr></thead><tbody><tr v-for="row in visibleRows" :key="relationKey(row)" :class="{ active: activeKey === relationKey(row) }" @click="select(row)"><td><input type="checkbox" :checked="selectedKeys.includes(relationKey(row))" :aria-label="`選取 ${row.qcListNo}`" @click.stop @change="toggleSelected(row, ($event.target as HTMLInputElement).checked)"></td><td>{{ row.qcListNo }}</td><td>{{ row.qcListName }}</td><td>{{ qcTypeLabel(row.qcType) }}</td><td>{{ issueTypeLabel(row.inspType) }}</td><td>{{ row.inspStartTime }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser || '' }}</td><td>{{ formatDate(row.reviseDate) }}</td></tr><tr v-if="!loading && !visibleRows.length"><td colspan="10" class="empty">{{ rows.length ? '沒有符合查詢條件的資料' : '此設備尚未設定點檢表' }}</td></tr><tr v-if="loading"><td colspan="10" class="empty">資料載入中…</td></tr></tbody></table></div>
      <footer>資料總筆數　{{ visibleRows.length }}</footer>
    </section>
    <div v-if="mode" class="eqp-qc-relation-form-shade" @click.self="mode = null"><form class="eqp-qc-relation-form" @submit.prevent="save"><header><strong>設備點檢表關聯 ({{ mode === 'edit' ? 'EDIT' : mode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="mode = null">×</button></header><p>設備編號　{{ equipmentNo }}　{{ equipmentName }}</p><label><span>點檢表編號</span><select v-model="form.qcListNo" required><option value="">請選擇點檢表</option><option v-for="item in qcLists" :key="item.qcListNo" :value="item.qcListNo">{{ item.qcListNo }}　{{ item.qcListName }} ({{ qcTypeLabel(item.qcType) }})</option></select></label><label><span>QC Type</span><input :value="selectedQcList ? qcTypeLabel(selectedQcList.qcType) : ''" readonly></label><fieldset :disabled="!scheduledInspection"><legend>點檢週期</legend><label v-for="option in [{value:0,label:'Lot'}, {value:1,label:'Shift'}, {value:2,label:'Daily'}]" :key="option.value"><input v-model.number="form.inspType" type="radio" :value="option.value">{{ option.label }}</label><label class="time"><span>Daily 起始時間</span><select v-model="form.inspStartTime" :disabled="!scheduledInspection || form.inspType !== 2"><option v-for="time in timeOptions" :key="time" :value="time">{{ time }}</option></select></label></fieldset><p v-if="selectedQcList && !supportedQcType" class="eqp-qc-relation-error">此 QC Type 尚未由 WinForms ValueList 核實，請先確認舊系統對應。</p><footer><button class="primary" type="submit" :disabled="busy">確定</button><button type="button" :disabled="busy" @click="mode = null">關閉</button></footer></form></div>
  </div>
</template>

<style scoped>
.eqp-qc-relation-overlay{position:fixed;inset:0;z-index:34;display:grid;place-items:center;padding:12px;background:#0005;font:13px 'Segoe UI','Microsoft JhengHei',sans-serif;color:#333}.eqp-qc-relation-window{display:flex;width:min(1320px,98vw);max-height:94vh;flex-direction:column;gap:8px;padding:12px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 28px #0003}.eqp-qc-relation-title{display:flex;align-items:center;justify-content:space-between;font-size:16px}.eqp-qc-relation-title button,.eqp-qc-relation-form header button{width:30px;height:28px;border:1px solid #ccc;background:#fafafa;font-size:20px}.eqp-qc-relation-dev{margin:0;padding:6px 9px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}.eqp-qc-relation-device{display:flex;gap:12px;margin:0;padding:8px;border:1px solid #ccc}.eqp-qc-relation-device legend{padding:0 5px}.eqp-qc-relation-device label,.eqp-qc-relation-search label{display:flex;align-items:center;gap:7px}.eqp-qc-relation-device input{min-width:150px;height:28px;border:1px solid #aaa;background:#eee;padding:4px 6px}.eqp-qc-relation-search{display:flex;align-items:center;gap:7px;padding:4px 8px;border:1px solid #ddd}.eqp-qc-relation-search input{height:28px;border:1px solid #aaa;padding:4px 7px}.eqp-qc-relation-actions{display:flex;gap:6px;padding:5px;border:1px solid #ccc}.eqp-qc-relation-actions button,.eqp-qc-relation-search button,.eqp-qc-relation-form footer button{min-width:66px;height:32px;padding:4px 9px;border:1px solid #c7c7c7;background:#fafafa;font:inherit}.eqp-qc-relation-actions .primary,.eqp-qc-relation-form .primary{border-color:#f0a126;background:#ffa62b;color:#fff}.eqp-qc-relation-actions .danger{color:#9a3a2b}.eqp-qc-relation-actions button:disabled{color:#aaa}.eqp-qc-relation-actions .spacer{flex:1}.eqp-qc-relation-grid-wrap{min-height:160px;overflow:auto;border:1px solid #b7b7b7}.eqp-qc-relation-grid{width:100%;border-collapse:collapse;white-space:nowrap}.eqp-qc-relation-grid th{position:sticky;top:0;background:#e7c600;color:#26251f;font-weight:500}.eqp-qc-relation-grid th,.eqp-qc-relation-grid td{height:29px;padding:4px 7px;border:1px solid #ccc;text-align:left}.eqp-qc-relation-grid tbody tr{cursor:pointer}.eqp-qc-relation-grid tbody tr.active{background:#ffe69a}.eqp-qc-relation-grid .empty{text-align:center;color:#777;padding:24px}.eqp-qc-relation-window>footer{color:#777}.eqp-qc-relation-error,.eqp-qc-relation-notice{margin:0;padding:6px 8px;border:1px solid #d4a3a3;background:#fff0f0;color:#7a2424}.eqp-qc-relation-notice{border-color:#a5caa7;background:#eff9ef;color:#275c2a}.eqp-qc-relation-form-shade{position:fixed;inset:0;z-index:1;display:grid;place-items:center;padding:14px;background:#0004}.eqp-qc-relation-form{display:flex;width:min(620px,95vw);flex-direction:column;gap:12px;padding:14px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 28px #0003}.eqp-qc-relation-form>header{display:flex;align-items:center;justify-content:space-between;font-size:15px}.eqp-qc-relation-form>p{margin:0;padding:4px 0}.eqp-qc-relation-form>label{display:grid;grid-template-columns:130px 1fr;align-items:center;gap:10px}.eqp-qc-relation-form select,.eqp-qc-relation-form input[readonly]{height:30px;border:1px solid #b3a765;background:#fff1aa;padding:4px 7px;font:inherit}.eqp-qc-relation-form input[readonly]{background:#eee}.eqp-qc-relation-form fieldset{display:flex;flex-wrap:wrap;align-items:center;gap:12px;margin:0;padding:10px;border:1px solid #ccc}.eqp-qc-relation-form fieldset>label{display:flex;align-items:center;gap:5px}.eqp-qc-relation-form fieldset .time{flex-basis:100%;margin-left:16px}.eqp-qc-relation-form fieldset .time span{width:112px}.eqp-qc-relation-form footer{display:flex;justify-content:flex-end;gap:8px}.eqp-qc-relation-form .eqp-qc-relation-error{color:#7a2424}
@media(max-width:720px){.eqp-qc-relation-device{flex-direction:column}.eqp-qc-relation-grid{min-width:1050px}.eqp-qc-relation-search{flex-wrap:wrap}}
</style>
