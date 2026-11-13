<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

type PropertyMode = 'new' | 'edit' | 'copy'
interface PropertyRow {
  equipment: string; propertyNo: string; defaultValue: string; propertySequence: number; description: string
  issueState: number; dataStamp: number; creator: string | null; createDate: string | null
  reviser: string | null; reviseDate: string | null
}
interface PropertyDefinition { elementNo: string; elementName: string; defaultValue: string; elementSequence: number; values: string[] }
interface PropertyForm { propertyNo: string; defaultValue: string; propertySequence: number; description: string; dataStamp: number }

const props = defineProps<{ equipmentNo: string; apiBaseUrl: string; currentUserNo?: string }>()
const emit = defineEmits<{ close: []; updated: [] }>()
const mock = import.meta.env.DEV
const storageKey = 'ymes-dev-mock:equipment-properties:v1'
const definitions: PropertyDefinition[] = [
  { elementNo: 'DEV-PROP-TEMP', elementName: '設備溫度', defaultValue: '25', elementSequence: 1, values: ['20', '25', '30'] },
  { elementNo: 'DEV-PROP-VOLT', elementName: '設備電壓', defaultValue: '220', elementSequence: 2, values: ['110', '220', '380'] },
  { elementNo: 'DEV-PROP-MODE', elementName: '運轉模式', defaultValue: 'AUTO', elementSequence: 3, values: ['AUTO', 'MANUAL'] },
]
const seed: PropertyRow[] = [
  { equipment: 'DEV-1AP-01', propertyNo: 'DEV-PROP-TEMP', defaultValue: '25', propertySequence: 1, description: 'DEV 假資料：標準設備溫度', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null },
  { equipment: 'DEV-1AP-01', propertyNo: 'DEV-PROP-VOLT', defaultValue: '220', propertySequence: 2, description: 'DEV 假資料：電壓設定', issueState: 2, dataStamp: 1, creator: 'DEV001', createDate: '2026-10-01T08:00:00+08:00', reviser: 'DEV002', reviseDate: '2026-10-03T08:00:00+08:00' },
  { equipment: 'DEV-1AP-02', propertyNo: 'DEV-PROP-MODE', defaultValue: 'AUTO', propertySequence: 1, description: 'DEV 假資料：自動模式', issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-01T08:00:00+08:00', reviser: null, reviseDate: null },
]

const rows = ref<PropertyRow[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const keyword = ref('')
const selected = ref<string[]>([])
const mode = ref<PropertyMode | null>(null)
const form = ref<PropertyForm>(emptyForm())
const audit = ref({ creator: '', createDate: '', reviser: '', reviseDate: '' })
const dirty = ref(false)
const definitionOpen = ref(false)
const valueOpen = ref(false)
const definitionSearch = ref('')
const valueOptions = ref<string[]>([])

function emptyForm(): PropertyForm { return { propertyNo: '', defaultValue: '', propertySequence: 0, description: '', dataStamp: 0 } }
function readMock(): PropertyRow[] {
  try { const value = localStorage.getItem(storageKey); return value ? JSON.parse(value) as PropertyRow[] : structuredClone(seed) }
  catch { return structuredClone(seed) }
}
function writeMock(value: PropertyRow[]): void { localStorage.setItem(storageKey, JSON.stringify(value)) }
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), `${base.replace(/\/+$/, '')}/`).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) throw new Error('登入狀態已逾期，請重新登入。')
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function rootPath(): string { return `api/equipment/${encodeURIComponent(props.equipmentNo)}/properties` }
const visibleRows = computed(() => rows.value
  .filter((row) => row.issueState === 0 && row.propertyNo.toLocaleLowerCase().includes(keyword.value.trim().toLocaleLowerCase()))
  .sort((a, b) => a.propertySequence - b.propertySequence || a.propertyNo.localeCompare(b.propertyNo)))
const filteredDefinitions = computed(() => definitions.filter((item) => `${item.elementNo} ${item.elementName}`.toLocaleLowerCase().includes(definitionSearch.value.trim().toLocaleLowerCase())))

async function loadRows(): Promise<void> {
  loading.value = true; error.value = ''
  try {
    rows.value = mock
      ? readMock().filter((row) => row.equipment === props.equipmentNo)
      : await request<PropertyRow[]>(`${rootPath()}?issueState=0&keyword=${encodeURIComponent(keyword.value.trim())}`)
    selected.value = selected.value.filter((no) => rows.value.some((row) => row.propertyNo === no))
  } catch (e) { error.value = e instanceof Error ? e.message : '設備屬性資料載入失敗。' }
  finally { loading.value = false }
}
function toggleSelected(propertyNo: string): void {
  selected.value = selected.value.includes(propertyNo)
    ? selected.value.filter((no) => no !== propertyNo)
    : [...selected.value, propertyNo]
}
function dateText(value: string | null): string {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.valueOf()) ? value : date.toLocaleString('zh-TW')
}
function stateText(value: number): string { return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(value)] ?? '未知' }

function openForm(nextMode: PropertyMode, source?: PropertyRow): void {
  mode.value = nextMode; error.value = ''; dirty.value = false
  if (!source || nextMode === 'new') {
    form.value = emptyForm()
    audit.value = { creator: props.currentUserNo || 'DEV001', createDate: new Date().toLocaleString('zh-TW'), reviser: '', reviseDate: '' }
  } else {
    form.value = { propertyNo: source.propertyNo, defaultValue: source.defaultValue, propertySequence: source.propertySequence, description: source.description, dataStamp: nextMode === 'edit' ? source.dataStamp : 0 }
    audit.value = nextMode === 'edit'
      ? { creator: source.creator || '', createDate: dateText(source.createDate), reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toLocaleString('zh-TW') }
      : { creator: props.currentUserNo || 'DEV001', createDate: new Date().toLocaleString('zh-TW'), reviser: '', reviseDate: '' }
  }
}
function chooseDefinition(item: PropertyDefinition): void {
  form.value.propertyNo = item.elementNo
  form.value.defaultValue = item.defaultValue
  form.value.propertySequence = item.elementSequence
  definitionOpen.value = false; dirty.value = true
}
function showValueOptions(): void {
  const definition = definitions.find((item) => item.elementNo === form.value.propertyNo)
  valueOptions.value = definition?.values ?? []
  valueOpen.value = true
}
function chooseValue(value: string): void { form.value.defaultValue = value; valueOpen.value = false; dirty.value = true }
function closeForm(): void { mode.value = null; dirty.value = false }

function mockSave(nextMode: PropertyMode): PropertyRow {
  const all = readMock()
  const propertyNo = form.value.propertyNo.trim().toUpperCase()
  const index = all.findIndex((row) => row.equipment === props.equipmentNo && row.propertyNo === propertyNo)
  if (nextMode !== 'edit' && index >= 0) throw new Error(`屬性編號 ${propertyNo} 已存在。`)
  if (nextMode === 'edit' && index < 0) throw new Error(`找不到屬性 ${propertyNo}。`)
  const previous = index >= 0 ? all[index] : null
  const now = new Date().toISOString()
  const saved: PropertyRow = {
    equipment: props.equipmentNo, propertyNo, defaultValue: form.value.defaultValue,
    propertySequence: form.value.propertySequence, description: form.value.description,
    issueState: 0, dataStamp: (previous?.dataStamp ?? -1) + 1,
    creator: previous?.creator || props.currentUserNo || 'DEV001', createDate: previous?.createDate || now,
    reviser: previous ? props.currentUserNo || 'DEV001' : null, reviseDate: previous ? now : null,
  }
  if (index >= 0) all[index] = saved
  else all.push(saved)
  writeMock(all)
  return saved
}
async function save(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value) return
  if (!form.value.propertyNo.trim()) { error.value = 'PropertyNo 為必填。'; return }
  if (!Number.isInteger(form.value.propertySequence) || form.value.propertySequence < 0 || form.value.propertySequence > 99) { error.value = 'PropertySequence 必須為 0 到 99 的整數。'; return }
  busy.value = true; error.value = ''
  const saveMode = mode.value
  try {
    const saved = mock
      ? mockSave(saveMode)
      : await request<PropertyRow>(saveMode === 'edit' ? `${rootPath()}/${encodeURIComponent(form.value.propertyNo)}` : rootPath(), {
        method: saveMode === 'edit' ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...form.value, issueState: saveMode === 'edit' ? 0 : undefined, dataStamp: form.value.dataStamp }),
      })
    if (keepOpen) {
      mode.value = 'edit'
      form.value.dataStamp = saved.dataStamp
      audit.value = { creator: saved.creator || audit.value.creator, createDate: dateText(saved.createDate) || audit.value.createDate, reviser: saved.reviser || props.currentUserNo || 'DEV001', reviseDate: dateText(saved.reviseDate) }
      dirty.value = false
    } else closeForm()
    await loadRows(); notice.value = `屬性 ${saved.propertyNo} 已${saveMode === 'edit' ? '更新' : saveMode === 'copy' ? '複製' : '建立'}。`
    emit('updated')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備屬性儲存失敗。' }
  finally { busy.value = false }
}

async function deleteSelected(): Promise<void> {
  const targets = rows.value.filter((row) => selected.value.includes(row.propertyNo))
  if (!targets.length || busy.value || !window.confirm(`確定刪除選取的 ${targets.length} 筆屬性？`)) return
  busy.value = true; error.value = ''
  try {
    for (const row of targets) {
      if (mock) {
        const all = readMock().filter((item) => !(item.equipment === row.equipment && item.propertyNo === row.propertyNo))
        writeMock(all)
      } else await request<void>(`${rootPath()}/${encodeURIComponent(row.propertyNo)}?issueState=${row.issueState}&dataStamp=${row.dataStamp}`, { method: 'DELETE' })
    }
    selected.value = []; await loadRows(); notice.value = `${targets.length} 筆屬性已刪除。`; emit('updated')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備屬性刪除失敗；已完成的逐筆操作不會自動復原。'; await loadRows() }
  finally { busy.value = false }
}
async function approveSelected(): Promise<void> {
  const targets = rows.value.filter((row) => selected.value.includes(row.propertyNo) && row.issueState === 0)
  if (!targets.length || busy.value || !window.confirm(`確定核准選取的 ${targets.length} 筆屬性？`)) return
  busy.value = true; error.value = ''
  try {
    for (const row of targets) {
      if (mock) {
        const all = readMock(); const index = all.findIndex((item) => item.equipment === row.equipment && item.propertyNo === row.propertyNo)
        if (index >= 0) all[index] = { ...all[index], issueState: 2, dataStamp: all[index].dataStamp + 1, reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString() }
        writeMock(all)
      } else await request<void>(`${rootPath()}/${encodeURIComponent(row.propertyNo)}/approve`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ issueState: row.issueState, creator: props.currentUserNo, dataStamp: row.dataStamp }) })
    }
    selected.value = []; await loadRows(); notice.value = `${targets.length} 筆屬性已核准。`; emit('updated')
  } catch (e) { error.value = e instanceof Error ? e.message : '設備屬性核准失敗；已完成的逐筆操作不會自動復原。'; await loadRows() }
  finally { busy.value = false }
}

onMounted(() => { void loadRows() })
</script>

<template>
  <div class="property-overlay" @click.self="emit('close')">
    <section class="property-window" role="dialog" aria-modal="true" :aria-label="`設備 ${equipmentNo} 屬性資料`">
      <header class="property-title"><strong>設備屬性資料</strong><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
      <div v-if="mock" class="property-dev-note">DEV 假資料｜操作只保存在此瀏覽器，不會呼叫 API 或寫入資料庫。</div>
      <div class="property-query"><label>設備編號 <input :value="equipmentNo" readonly /></label><label>PropertyNo <input v-model="keyword" @keyup.enter="loadRows" /></label><button type="button" :disabled="loading" @click="loadRows">查詢</button></div>
      <p v-if="error" class="property-message error" role="alert">{{ error }}</p><p v-if="notice" class="property-message success" role="status">{{ notice }}</p>
      <div class="property-content">
        <div class="property-table-scroll">
          <table class="property-table"><thead><tr><th>選取</th><th>設備</th><th>PropertyNo</th><th>Property Value</th><th>Sequence</th><th>Description</th><th>狀態</th></tr></thead>
            <tbody><tr v-for="row in visibleRows" :key="row.propertyNo" :class="{ 'property-selected': selected.includes(row.propertyNo) }" @click="toggleSelected(row.propertyNo)">
              <td @click.stop><input type="checkbox" :checked="selected.includes(row.propertyNo)" :aria-label="`選取 ${row.propertyNo}`" @change="toggleSelected(row.propertyNo)" /></td>
              <td>{{ row.equipment }}</td><td>{{ row.propertyNo }}</td><td>{{ row.defaultValue }}</td><td>{{ row.propertySequence }}</td><td>{{ row.description }}</td><td>{{ stateText(row.issueState) }}</td>
            </tr><tr v-if="!loading && visibleRows.length === 0"><td colspan="7" class="property-empty">沒有符合條件的屬性資料</td></tr><tr v-if="loading"><td colspan="7" class="property-empty">載入中…</td></tr></tbody>
          </table>
        </div>
        <aside class="property-actions"><button class="property-add" type="button" @click="openForm('new')">新增(A)</button><button type="button" :disabled="selected.length !== 1 || busy" @click="openForm('edit', rows.find((row) => row.propertyNo === selected[0]))">修改(E)</button><button type="button" :disabled="selected.length === 0 || busy" @click="deleteSelected">刪除(D)</button><button type="button" :disabled="selected.length !== 1 || busy" @click="openForm('copy', rows.find((row) => row.propertyNo === selected[0]))">複製(C)</button><button type="button" :disabled="selected.length === 0 || busy" @click="approveSelected">核准</button><button type="button" @click="emit('close')">關閉</button></aside>
      </div>
      <footer class="property-count">資料總筆數 <strong>{{ visibleRows.length }}</strong></footer>
    </section>

    <div v-if="mode" class="property-form-backdrop" @click.self="closeForm">
      <form class="property-form" @submit.prevent="save(false)" @input="dirty = true" @change="dirty = true">
        <header><strong>設備屬性定義 ({{ mode === 'edit' ? 'EDIT' : mode === 'copy' ? 'COPY' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="closeForm">×</button></header>
        <p v-if="error" class="property-message error" role="alert">{{ error }}</p>
        <fieldset><legend>設備屬性資料</legend>
          <label><span>設備編號</span><input :value="equipmentNo" readonly /></label>
          <label><span>PropertyNo</span><span class="property-input-with-button"><input v-model="form.propertyNo" required :readonly="mode === 'edit'" /><button v-if="mode !== 'edit'" type="button" @click="definitionOpen = true">...</button></span></label>
          <label><span>Property Value</span><span class="property-input-with-button"><input v-model="form.defaultValue" /><button type="button" @click="showValueOptions">...</button></span></label>
          <label><span>Sequence</span><input v-model.number="form.propertySequence" type="number" min="0" max="99" step="1" /></label>
          <label><span>Description</span><textarea v-model="form.description" rows="4" /></label>
        </fieldset>
        <div class="property-audit"><span>建立人　{{ audit.creator || '—' }}</span><span>建立日期　{{ audit.createDate || '—' }}</span><span>修改人　{{ audit.reviser || '—' }}</span><span>修改日期　{{ audit.reviseDate || '—' }}</span></div>
        <footer><button class="property-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="closeForm">關閉</button><button type="button" :disabled="busy || !dirty" @click="save(true)">套用</button></footer>
      </form>
    </div>

    <div v-if="definitionOpen" class="property-picker-backdrop" @click.self="definitionOpen = false"><section class="property-picker"><header><strong>選取屬性定義</strong><button type="button" @click="definitionOpen = false">×</button></header><input v-model="definitionSearch" placeholder="搜尋 PropertyNo / 名稱" /><table><thead><tr><th>PropertyNo</th><th>名稱</th><th>預設值</th><th>順序</th></tr></thead><tbody><tr v-for="item in filteredDefinitions" :key="item.elementNo" tabindex="0" @click="chooseDefinition(item)" @keydown.enter="chooseDefinition(item)"><td>{{ item.elementNo }}</td><td>{{ item.elementName }}</td><td>{{ item.defaultValue }}</td><td>{{ item.elementSequence }}</td></tr></tbody></table></section></div>
    <div v-if="valueOpen" class="property-picker-backdrop" @click.self="valueOpen = false"><section class="property-picker"><header><strong>選取 Property Value</strong><button type="button" @click="valueOpen = false">×</button></header><p v-if="valueOptions.length === 0">此屬性沒有可選的 MES Parameter 值。</p><table v-else><tbody><tr v-for="value in valueOptions" :key="value" tabindex="0" @click="chooseValue(value)" @keydown.enter="chooseValue(value)"><td>{{ value }}</td></tr></tbody></table></section></div>
  </div>
</template>

<style scoped>
.property-overlay,.property-form-backdrop,.property-picker-backdrop{position:fixed;inset:0;z-index:35;display:grid;place-items:center;padding:12px;background:#0005;font:13px 'Segoe UI','Microsoft JhengHei',sans-serif;color:#333}
.property-window,.property-form,.property-picker{display:flex;flex-direction:column;gap:8px;width:min(1050px,97vw);max-height:94vh;overflow:auto;padding:12px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 28px #0003}
.property-title,.property-form>header,.property-picker>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}
.property-title button,.property-form>header button,.property-picker>header button{width:30px;height:28px;border:1px solid #ccc;background:#fafafa;font-size:20px}
.property-dev-note{padding:7px 9px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}
.property-query{display:flex;align-items:center;gap:8px;padding:8px;border:1px solid #d5d5d5}.property-query label{display:flex;align-items:center;gap:7px}.property-query input{height:28px;border:1px solid #888;padding:4px 7px}.property-query label:first-child input{background:#eee}
.property-message{margin:0;padding:6px 8px}.property-message.error{color:#9c2424;background:#fff0f0}.property-message.success{color:#246c35;background:#f0fff2}
.property-content{display:grid;grid-template-columns:minmax(0,1fr) 110px;gap:8px;min-height:240px}.property-table-scroll{overflow:auto;border:1px solid #ccc}.property-table,.property-picker table{width:100%;border-collapse:collapse;white-space:nowrap}.property-table th,.property-picker th{background:#4ca5e9;color:#fff}.property-table th,.property-table td,.property-picker th,.property-picker td{padding:6px 8px;border:1px solid #ccc;text-align:left}.property-table tbody tr{cursor:pointer}.property-table tbody tr:hover,.property-table tbody tr.property-selected,.property-picker tbody tr:hover{background:#e8f2fb}.property-table input{accent-color:#555}.property-empty{text-align:center!important;color:#777;padding:20px!important}
.property-actions{display:flex;flex-direction:column;gap:6px}.property-actions button,.property-query button,.property-form footer button{min-height:31px;border:1px solid #c7c7c7;background:#fafafa;font:inherit}.property-actions .property-add,.property-form footer .property-confirm{background:#ffa62b;color:#fff;border-color:#f0a126}
.property-count{padding:4px 2px;color:#777}.property-form-backdrop,.property-picker-backdrop{z-index:36}.property-form{width:min(790px,96vw)}.property-form fieldset{display:grid;gap:10px;margin:0;padding:12px;border:1px solid #aaa}.property-form fieldset legend{padding:0 6px}.property-form fieldset>label{display:grid;grid-template-columns:140px minmax(0,1fr);align-items:center;gap:8px}.property-form input,.property-form textarea,.property-form button,.property-picker input{min-width:0;min-height:30px;padding:4px 7px;border:1px solid #888;border-radius:0;font:inherit}.property-form input:not([readonly]){background:#fff1aa}.property-form input[readonly]{background:#eee}.property-input-with-button{display:flex;gap:5px}.property-input-with-button input{flex:1}.property-input-with-button button{width:40px;background:#fafafa}.property-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:9px;border:1px solid #ccc;color:#777}.property-form>footer{display:flex;justify-content:flex-end;gap:8px}.property-form>footer button{min-width:95px;min-height:36px}
.property-picker-backdrop{z-index:37}.property-picker{width:min(620px,94vw)}.property-picker input{width:100%}.property-picker tbody tr{cursor:pointer}.property-picker tbody tr:hover{background:#e8f2fb}
@media(max-width:760px){.property-content{grid-template-columns:1fr}.property-actions{display:grid;grid-template-columns:repeat(3,minmax(0,1fr))}.property-audit{grid-template-columns:1fr 1fr}.property-query{flex-wrap:wrap}.property-form fieldset>label{grid-template-columns:1fr}}
</style>
