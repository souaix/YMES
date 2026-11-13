<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

interface ParameterOption { parameterNo: string; parameterName: string }
interface SpecOption { value: number; label: string }
interface ParameterRow { seqNo: number; parameterNo: string; specType: number | null; parameterValue?: number | null; uiId: string }
interface StoredParameter { seqNo: number; parameterNo: string; specType: number; parameterValue?: number | null }

const props = defineProps<{ equipmentNo: string; apiBaseUrl: string; currentUserNo?: string }>()
const emit = defineEmits<{ close: []; updated: [] }>()
const mock = import.meta.env.DEV
const storageKey = 'ymes-dev-mock:equipment-parameters:v1'
const seed: Record<string, StoredParameter[]> = {
  'DEV-1AP-01': [
    { seqNo: 1, parameterNo: 'DEV-PARAM-TEMP', specType: 0, parameterValue: 25 },
    { seqNo: 2, parameterNo: 'DEV-PARAM-PRESSURE', specType: 1, parameterValue: 1.2 },
  ],
  'DEV-1AP-02': [{ seqNo: 1, parameterNo: 'DEV-PARAM-CYCLE', specType: 0, parameterValue: 30 }],
}
const mockParameterOptions: ParameterOption[] = [
  { parameterNo: 'DEV-PARAM-TEMP', parameterName: 'DEV 假資料：設備溫度' },
  { parameterNo: 'DEV-PARAM-PRESSURE', parameterName: 'DEV 假資料：設備壓力' },
  { parameterNo: 'DEV-PARAM-CYCLE', parameterName: 'DEV 假資料：加工週期' },
  { parameterNo: 'DEV-PARAM-SPEED', parameterName: 'DEV 假資料：主軸轉速' },
]
const mockSpecOptions: SpecOption[] = [
  { value: 0, label: 'DEV 規格型態 0' }, { value: 1, label: 'DEV 規格型態 1' }, { value: 2, label: 'DEV 規格型態 2' },
]
const rows = ref<ParameterRow[]>([])
const parameterOptions = ref<ParameterOption[]>([])
const specOptions = ref<SpecOption[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const activeId = ref('')
let nextId = 0

function newUiId(): string { nextId += 1; return `parameter-row-${nextId}` }
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
function readMock(): Record<string, StoredParameter[]> {
  try { const raw = localStorage.getItem(storageKey); return raw ? JSON.parse(raw) as Record<string, StoredParameter[]> : structuredClone(seed) }
  catch { return structuredClone(seed) }
}
function writeMock(value: Record<string, StoredParameter[]>): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function normalizeOrder(): void { rows.value.forEach((row, index) => { row.seqNo = index + 1 }) }
const selectedIndex = computed(() => rows.value.findIndex((row) => row.uiId === activeId.value))
function addRow(): void {
  const row: ParameterRow = { seqNo: rows.value.length + 1, parameterNo: '', specType: null, uiId: newUiId() }
  rows.value.push(row); activeId.value = row.uiId
}
function removeRow(): void {
  if (selectedIndex.value < 0) return
  rows.value.splice(selectedIndex.value, 1); normalizeOrder()
  activeId.value = rows.value[Math.min(selectedIndex.value, rows.value.length - 1)]?.uiId ?? ''
}
function moveRow(direction: -1 | 1): void {
  const index = selectedIndex.value; const target = index + direction
  if (index < 0 || target < 0 || target >= rows.value.length) return
  const [row] = rows.value.splice(index, 1)
  rows.value.splice(target, 0, row); normalizeOrder(); activeId.value = row.uiId
}
function availableParameterOptions(row: ParameterRow): ParameterOption[] {
  const used = new Set(rows.value.filter((item) => item.uiId !== row.uiId).map((item) => item.parameterNo))
  return parameterOptions.value.filter((item) => !used.has(item.parameterNo) || item.parameterNo === row.parameterNo)
}
async function load(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    if (mock) {
      const stored = readMock()[props.equipmentNo] ?? seed[props.equipmentNo] ?? []
      rows.value = structuredClone(stored).sort((a, b) => a.seqNo - b.seqNo).map((item) => ({ ...item, uiId: newUiId() }))
      parameterOptions.value = structuredClone(mockParameterOptions)
      specOptions.value = structuredClone(mockSpecOptions)
    } else {
      const [items, options] = await Promise.all([
        request<StoredParameter[]>(`api/equipment/${encodeURIComponent(props.equipmentNo)}/parameters`),
        request<{ parameters: ParameterOption[]; specTypes: SpecOption[] }>('api/equipment/parameter-options'),
      ])
      rows.value = items.sort((a, b) => a.seqNo - b.seqNo).map((item) => ({ ...item, uiId: newUiId() }))
      parameterOptions.value = options.parameters
      specOptions.value = options.specTypes
    }
    normalizeOrder(); activeId.value = rows.value[0]?.uiId ?? ''
  } catch (e) { error.value = e instanceof Error ? e.message : '設備參數載入失敗。' }
  finally { loading.value = false }
}
async function confirm(): Promise<void> {
  if (busy.value) return
  if (rows.value.some((row) => !row.parameterNo)) { error.value = '每一列都必須選擇 ParameterName。'; return }
  if (rows.value.some((row) => row.specType === null || row.specType === undefined)) { error.value = '每一列都必須選擇 SpecType。'; return }
  const counts = new Map<string, number>()
  for (const row of rows.value) counts.set(row.parameterNo, (counts.get(row.parameterNo) ?? 0) + 1)
  const duplicate = [...counts].find(([, count]) => count > 1)?.[0]
  if (duplicate) { error.value = `ParameterName 不可重複：${duplicate}`; return }

  busy.value = true; error.value = ''
  const payload = rows.value.map((row, index) => ({ seqNo: index + 1, parameterNo: row.parameterNo, specType: row.specType as number, reviser: props.currentUserNo || 'DEV001' }))
  try {
    if (mock) {
      const all = readMock()
      all[props.equipmentNo] = payload.map((item) => ({ ...item, parameterValue: rows.value.find((row) => row.parameterNo === item.parameterNo)?.parameterValue ?? null }))
      writeMock(all)
    } else await request<void>(`api/equipment/${encodeURIComponent(props.equipmentNo)}/parameters`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ parameters: payload }) })
    notice.value = `設備 ${props.equipmentNo} 的參數設定已儲存。`
    emit('updated'); emit('close')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備參數儲存失敗。' }
  finally { busy.value = false }
}

onMounted(() => { void load() })
</script>

<template>
  <div class="parameter-overlay" @click.self="emit('close')">
    <section class="parameter-window" role="dialog" aria-modal="true" aria-label="設備參數設定">
      <header><strong>設備參數設定</strong><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
      <p v-if="mock" class="parameter-dev-note">DEV 假資料｜操作只保存在此瀏覽器，不會呼叫 API 或寫入資料庫；SpecType 值為示範資料。</p>
      <div class="parameter-equipment"><label>設備編號 <input :value="equipmentNo" readonly /></label><span>參數名稱及規格型態依設備設定選項載入。</span></div>
      <p v-if="error" class="parameter-error" role="alert">{{ error }}</p>
      <div v-if="loading" class="parameter-loading">設備參數載入中…</div>
      <div v-else class="parameter-grid-scroll"><table><thead><tr><th>SeqNo</th><th>ParameterName</th><th>SpecType</th><th>ParameterValue</th></tr></thead><tbody>
        <tr v-for="row in rows" :key="row.uiId" :class="{ active: activeId === row.uiId }" @click="activeId = row.uiId">
          <td>{{ row.seqNo }}</td><td><select v-model="row.parameterNo"><option value="">請選擇參數</option><option v-for="option in availableParameterOptions(row)" :key="option.parameterNo" :value="option.parameterNo">{{ option.parameterName }}</option></select></td>
          <td><select v-model.number="row.specType"><option :value="null">請選擇</option><option v-for="option in specOptions" :key="option.value" :value="option.value">{{ option.label }}</option></select></td>
          <td class="parameter-hidden-value">{{ row.parameterValue ?? '—' }}</td>
        </tr>
        <tr v-if="rows.length === 0" class="parameter-empty"><td colspan="4">尚無參數，按「新增列」建立設定。</td></tr>
      </tbody></table></div>
      <footer class="parameter-actions"><div class="parameter-reorder"><button type="button" :disabled="selectedIndex <= 0 || busy" @click="moveRow(-1)" aria-label="上移">↑</button><button type="button" :disabled="selectedIndex < 0 || selectedIndex >= rows.length - 1 || busy" @click="moveRow(1)" aria-label="下移">↓</button><button type="button" :disabled="selectedIndex < 0 || busy" @click="removeRow">刪除</button><button type="button" :disabled="busy" @click="addRow">新增列</button></div><span class="parameter-spacer"/><button class="parameter-confirm" type="button" :disabled="busy || loading" @click="confirm">確定</button><button type="button" :disabled="busy" @click="emit('close')">關閉</button></footer>
    </section>
  </div>
</template>

<style scoped>
.parameter-overlay{position:fixed;inset:0;z-index:35;display:grid;place-items:center;padding:14px;background:#0005;font:13px 'Segoe UI','Microsoft JhengHei',sans-serif;color:#333}
.parameter-window{display:flex;width:min(880px,96vw);max-height:92vh;flex-direction:column;gap:10px;overflow:auto;padding:14px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 28px #0003}
.parameter-window>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}.parameter-window>header button{width:30px;height:28px;border:1px solid #ccc;background:#fafafa;font-size:20px}
.parameter-dev-note{margin:0;padding:7px 9px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}.parameter-equipment{display:flex;justify-content:space-between;gap:10px;padding:8px;border:1px solid #ddd}.parameter-equipment label{display:flex;align-items:center;gap:8px}.parameter-equipment input{height:28px;border:1px solid #aaa;background:#eee;padding:4px 7px}.parameter-equipment span{align-self:center;color:#777}
.parameter-error{margin:0;padding:7px;border:1px solid #d4a3a3;background:#fff0f0;color:#7a2424}.parameter-loading,.parameter-empty{padding:24px;text-align:center;color:#777}.parameter-grid-scroll{max-height:55vh;overflow:auto;border:1px solid #c7c7c7}.parameter-grid-scroll table{width:100%;border-collapse:collapse}.parameter-grid-scroll th{position:sticky;top:0;background:#e7c600;color:#26251f;text-align:left}.parameter-grid-scroll th,.parameter-grid-scroll td{height:34px;padding:5px 8px;border:1px solid #ccc}.parameter-grid-scroll tbody tr.active{background:#ffe69a}.parameter-grid-scroll select{width:100%;min-width:160px;height:28px;border:1px solid #b3a765;background:#fff1aa;padding:3px 6px;font:inherit}.parameter-hidden-value{color:#888}
.parameter-actions{display:flex;align-items:center;gap:8px}.parameter-reorder{display:flex;gap:5px}.parameter-reorder button,.parameter-actions>button{min-width:54px;height:34px;padding:4px 10px;border:1px solid #c7c7c7;background:#fafafa;font:inherit}.parameter-reorder button:disabled,.parameter-actions>button:disabled{color:#aaa}.parameter-spacer{flex:1}.parameter-actions .parameter-confirm{min-width:100px;border-color:#f0a126;background:#ffa62b;color:#fff}
@media(max-width:620px){.parameter-equipment{flex-direction:column}.parameter-actions{flex-wrap:wrap}.parameter-spacer{flex-basis:100%}.parameter-grid-scroll table{min-width:620px}}
</style>
