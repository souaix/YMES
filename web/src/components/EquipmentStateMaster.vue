<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'

type StateMode = 'new' | 'edit' | 'copy' | null
interface EquipmentState {
  equipmentState: number
  stateType: number
  stateName: string
  stateColor: number
  description: string | null
  engineerGroupNo?: string | null
  utilizationType: number
  stateColorHex?: string | null
  stateTypeText?: string
  utilizationTypeText?: string
}
interface ValueOption {
  value: number
  label: string
}
interface StateForm {
  equipmentState: string
  stateType: number
  stateName: string
  color: string
  description: string
  utilizationType: number | null
}

const props = withDefaults(defineProps<{ apiBaseUrl: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const usesDevelopmentMock = computed(() => import.meta.env.DEV && props.developmentPreview)
const storageKey = 'ymes-dev-mock:equipment-states:v1'
const referenceStorageKey = 'ymes-dev-mock:equipment-state-references:v1'
const fallbackStateTypes: ValueOption[] = [
  { value: 0, label: '系統' },
  { value: 1, label: '使用者定義' },
]
const fallbackUtilizationTypes: ValueOption[] = [
  { value: 0, label: '非稼動' },
  { value: 1, label: '稼動' },
]
const seedRows: EquipmentState[] = [
  { equipmentState: 81, stateType: 1, stateName: 'DEV 假資料：待機', stateColor: -16711936, description: 'DEV 假資料，供畫面操作與色彩預覽', utilizationType: 1, stateColorHex: '00FF00' },
  { equipmentState: 82, stateType: 1, stateName: 'DEV 假資料：加工中', stateColor: -16776961, description: 'DEV 假資料，狀態編號為示範值', utilizationType: 1, stateColorHex: '0000FF' },
  { equipmentState: 83, stateType: 1, stateName: 'DEV 假資料：維修中', stateColor: -65536, description: 'DEV 假資料；此筆模擬已被點檢表引用', utilizationType: 0, stateColorHex: 'FF0000' },
]
const rows = ref<EquipmentState[]>([])
const stateTypeOptions = ref<ValueOption[]>(fallbackStateTypes)
const utilizationTypeOptions = ref<ValueOption[]>(fallbackUtilizationTypes)
const selectedNumbers = ref<number[]>([])
const currentNumber = ref<number | null>(null)
const busy = ref(false)
const loading = ref(false)
const error = ref('')
const notice = ref('')
const mode = ref<StateMode>(null)
const formDirty = ref(false)
const form = ref<StateForm>(blankForm())
const equipmentStateInput = ref<HTMLInputElement | null>(null)
const stateTypeInput = ref<HTMLSelectElement | null>(null)
const colorInput = ref<HTMLInputElement | null>(null)

const current = computed(() => rows.value.find((row) => row.equipmentState === currentNumber.value) ?? null)
const currentModeTitle = computed(() => mode.value === 'edit' ? '修改設備狀態' : mode.value === 'copy' ? '複製設備狀態' : '新增設備狀態')

function blankForm(): StateForm {
  return { equipmentState: '', stateType: stateTypeOptions.value[0]?.value ?? 0, stateName: '', color: '#808080', description: '', utilizationType: null }
}
function read<T>(key: string, initial: T): T {
  try {
    const value = localStorage.getItem(key)
    return value ? JSON.parse(value) as T : structuredClone(initial)
  } catch { return structuredClone(initial) }
}
function write<T>(key: string, value: T): void {
  localStorage.setItem(key, JSON.stringify(value))
}
function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(base.endsWith('/') ? base : base + '/', window.location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || '設備狀態 API 回應錯誤 (' + response.status + ')。')
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function colorCss(value: number): string {
  const red = (value >>> 16) & 255
  const green = (value >>> 8) & 255
  const blue = value & 255
  return 'rgb(' + red + ', ' + green + ', ' + blue + ')'
}
function colorToArgb(value: string): number {
  const normalized = value.replace('#', '')
  const red = Number.parseInt(normalized.slice(0, 2), 16)
  const green = Number.parseInt(normalized.slice(2, 4), 16)
  const blue = Number.parseInt(normalized.slice(4, 6), 16)
  return ((255 << 24) | (red << 16) | (green << 8) | blue) | 0
}
function colorToHex(value: string): string {
  return value.replace('#', '').toUpperCase()
}
function stateTypeOptionLabel(option: ValueOption): string {
  if (option.value === 0) return '系統'
  if (option.value === 1) return '使用者定義'
  return option.label
}
function utilizationTypeOptionLabel(option: ValueOption): string {
  if (option.value === 0) return '非稼動'
  if (option.value === 1) return '稼動'
  return option.label
}
function stateTypeLabel(value: number, supplied?: string): string {
  const option = stateTypeOptions.value.find((item) => item.value === value)
  return option ? stateTypeOptionLabel(option) : supplied || String(value)
}
function utilizationLabel(value: number, supplied?: string): string {
  const option = utilizationTypeOptions.value.find((item) => item.value === value)
  return option ? utilizationTypeOptionLabel(option) : supplied || String(value)
}
function closePage(): void {
  window.history.pushState({}, '', appBase || '/')
  window.dispatchEvent(new PopStateEvent('popstate'))
}
function toggleSelected(value: number): void {
  selectedNumbers.value = selectedNumbers.value.includes(value)
    ? selectedNumbers.value.filter((item) => item !== value)
    : [...selectedNumbers.value, value]
}
function selectRow(value: number): void {
  currentNumber.value = value
  error.value = ''
}
function openForm(nextMode: Exclude<StateMode, null>): void {
  const row = current.value
  if (nextMode !== 'new' && !row) {
    error.value = '請先選取一筆設備狀態。'
    return
  }
  if (nextMode === 'edit' && !row) return
  mode.value = nextMode
  form.value = row && nextMode !== 'new'
    ? {
        equipmentState: nextMode === 'edit' ? String(row.equipmentState) : '',
        stateType: row.stateType,
        stateName: row.stateName,
        color: rgbHex(row.stateColor),
        description: row.description || '',
        utilizationType: row.utilizationType,
      }
    : blankForm()
  error.value = ''
  notice.value = ''
  formDirty.value = false
  void nextTick(() => {
    const target = nextMode === 'edit' ? stateTypeInput.value : equipmentStateInput.value
    target?.focus()
    if (target === equipmentStateInput.value) equipmentStateInput.value?.select()
  })
}
function openColorPicker(): void {
  colorInput.value?.click()
}
function rgbHex(value: number): string {
  const red = ((value >>> 16) & 255).toString(16).padStart(2, '0')
  const green = ((value >>> 8) & 255).toString(16).padStart(2, '0')
  const blue = (value & 255).toString(16).padStart(2, '0')
  return '#' + red + green + blue
}
function closeForm(): void {
  if (busy.value) return
  mode.value = null
  formDirty.value = false
  error.value = ''
}
function validateForm(requireUtilization: boolean): string {
  const rawCode = String(form.value.equipmentState ?? '').trim()
  if (!rawCode) return '設備狀態編號為必填。'
  if (!/^\d+$/.test(rawCode)) return '設備狀態編號必須是整數。'
  const code = Number(rawCode)
  if (code < 0 || code > 99) return '設備狀態編號需介於 0 到 99。'
  if (form.value.stateType === 1 && code < 11) return '使用者定義類型不可使用 0 到 10。'
  if (!form.value.stateName.trim()) return '狀態名稱為必填。'
  if (form.value.stateName.trim().length > 50) return '狀態名稱不可超過 50 個字元。'
  if (form.value.description.length > 255) return '說明不可超過 255 個字元。'
  if (requireUtilization && form.value.utilizationType !== 0 && form.value.utilizationType !== 1) return '請選擇稼動類型。'
  if (!/^#[0-9a-fA-F]{6}$/.test(form.value.color)) return '請選擇有效的狀態顏色。'
  const duplicate = rows.value.some((row) => row.equipmentState === code && !(mode.value === 'edit' && row.equipmentState === currentNumber.value))
  if (duplicate) return '設備狀態編號已存在。'
  return ''
}
function requestBody(includeHex: boolean): Omit<EquipmentState, 'stateTypeText' | 'utilizationTypeText'> {
  const body: Omit<EquipmentState, 'stateTypeText' | 'utilizationTypeText'> = {
    equipmentState: Number(form.value.equipmentState),
    stateType: form.value.stateType,
    stateName: form.value.stateName.trim(),
    stateColor: colorToArgb(form.value.color),
    description: form.value.description.trim() || null,
    utilizationType: form.value.utilizationType ?? (includeHex ? 1 : 0),
  }
  if (includeHex) body.stateColorHex = colorToHex(form.value.color)
  return body
}
async function saveForm(keepOpen: boolean): Promise<void> {
  if (!mode.value || busy.value) return
  const problem = validateForm(!keepOpen)
  if (problem) { error.value = problem; return }
  const saveMode = mode.value
  const body = requestBody(!keepOpen)
  busy.value = true
  error.value = ''
  try {
    if (usesDevelopmentMock.value) {
      const latest = read<EquipmentState[]>(storageKey, seedRows)
      if (saveMode === 'edit') {
        const index = latest.findIndex((row) => row.equipmentState === currentNumber.value)
        if (index < 0) throw new Error('設備狀態已不存在，請重新載入。')
        latest[index] = { ...latest[index], ...body }
      } else {
        if (latest.some((row) => row.equipmentState === body.equipmentState)) throw new Error('設備狀態編號已存在。')
        latest.push(body)
      }
      write(storageKey, latest)
      rows.value = latest
    } else if (saveMode === 'edit') {
      await request<void>('api/equipment/states/' + encodeURIComponent(String(currentNumber.value)), {
        method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    } else {
      await request<void>('api/equipment/states', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
    }
    currentNumber.value = body.equipmentState
    if (!usesDevelopmentMock.value) await loadRows()
    notice.value = saveMode === 'edit' ? '設備狀態已修改。' : saveMode === 'copy' ? '設備狀態已複製。' : '設備狀態已新增。'
    if (keepOpen && saveMode === 'edit') formDirty.value = false
    if (keepOpen && saveMode !== 'edit') {
      formDirty.value = false
      mode.value = saveMode
      await nextTick()
      equipmentStateInput.value?.focus()
      equipmentStateInput.value?.select()
    } else if (!keepOpen) {
      mode.value = null
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : '設備狀態儲存失敗。'
  } finally {
    busy.value = false
  }
}
async function deleteSelected(): Promise<void> {
  if (busy.value) return
  const targetNumbers = new Set(selectedNumbers.value)
  if (currentNumber.value !== null) targetNumbers.add(currentNumber.value)
  const targets = rows.value.filter((row) => targetNumbers.has(row.equipmentState))
  if (targets.length === 0) { error.value = '請先選取要刪除的設備狀態。'; return }
  if (!window.confirm('確定刪除選取的 ' + targets.length + ' 筆設備狀態？')) return
  busy.value = true
  error.value = ''
  notice.value = ''
  formDirty.value = false
  let changed = false
  let deleteError = ''
  try {
    if (usesDevelopmentMock.value) {
      for (const row of targets) {
        const referenced = read<number[]>(referenceStorageKey, [83])
        if (referenced.includes(row.equipmentState)) throw new Error('設備狀態「' + row.stateName + '」已被設備現況、設備狀態控制或設備點檢表引用，不能刪除。')
        const latest = read<EquipmentState[]>(storageKey, seedRows)
        const remaining = latest.filter((item) => item.equipmentState !== row.equipmentState)
        write(storageKey, remaining)
        rows.value = remaining
        changed = true
      }
    } else {
      for (const row of targets) {
        await request<void>('api/equipment/states/' + encodeURIComponent(String(row.equipmentState)), { method: 'DELETE' })
        changed = true
      }
    }
    selectedNumbers.value = []
    currentNumber.value = null
    notice.value = '已刪除 ' + targets.length + ' 筆設備狀態。'
  } catch (e) {
    deleteError = e instanceof Error ? e.message : '設備狀態刪除失敗。'
  } finally {
    busy.value = false
  }
  if (changed && !usesDevelopmentMock.value) await loadRows()
  if (deleteError) error.value = deleteError
}
async function loadRows(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    if (usesDevelopmentMock.value) {
      rows.value = read<EquipmentState[]>(storageKey, seedRows)
      stateTypeOptions.value = read<ValueOption[]>('ymes-dev-mock:equipment-state-type-options:v1', fallbackStateTypes)
      utilizationTypeOptions.value = read<ValueOption[]>('ymes-dev-mock:equipment-state-utilization-options:v1', fallbackUtilizationTypes)
    } else {
      const [data, options] = await Promise.all([
        request<EquipmentState[]>('api/equipment/states'),
        request<{ stateTypes: ValueOption[]; utilizationTypes: ValueOption[] }>('api/equipment/states/options'),
      ])
      rows.value = data
      stateTypeOptions.value = options.stateTypes
      utilizationTypeOptions.value = options.utilizationTypes
    }
    rows.value = rows.value.slice().sort((a, b) => a.equipmentState - b.equipmentState)
    selectedNumbers.value = selectedNumbers.value.filter((value) => rows.value.some((row) => row.equipmentState === value))
    if (currentNumber.value !== null && !rows.value.some((row) => row.equipmentState === currentNumber.value)) currentNumber.value = null
  } catch (e) {
    rows.value = []
    currentNumber.value = null
    error.value = e instanceof Error ? e.message : '設備狀態清單載入失敗。'
  } finally {
    loading.value = false
  }
}

onMounted(() => { void loadRows() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => { void loadRows() })
</script>

<template>
  <section class="eqstate-page">
    <div v-if="usesDevelopmentMock" class="eqstate-dev-note">開發模式：目前資料為 DEV 假資料，儲存在此瀏覽器；不會寫入 MES 或正式資料庫。</div>

    <p v-if="error && !mode" class="eqstate-message eqstate-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !mode" class="eqstate-message eqstate-success" role="status">{{ notice }}</p>

    <div class="eqstate-main">
      <div class="eqstate-grid-wrap">
      <table class="eqstate-grid">
        <thead>
          <tr>
            <th class="eqstate-check"></th>
            <th>設備狀態編號</th>
            <th>狀態類型</th>
            <th>狀態名稱</th>
            <th>狀態顏色</th>
            <th>說明</th>
            <th>稼動類型</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7" class="eqstate-empty">載入中…</td>
          </tr>
          <tr v-else-if="rows.length === 0">
            <td colspan="7" class="eqstate-empty">目前沒有設備狀態資料。</td>
          </tr>
          <tr
            v-for="row in rows"
            :key="row.equipmentState"
            :class="{ 'eqstate-selected': currentNumber === row.equipmentState, 'eqstate-marked': selectedNumbers.includes(row.equipmentState) }"
            @click="selectRow(row.equipmentState)"
            @dblclick="selectRow(row.equipmentState); openForm('edit')"
          >
            <td class="eqstate-check" @click.stop>
              <input
                type="checkbox"
                :aria-label="'選取設備狀態 ' + row.equipmentState"
                :checked="selectedNumbers.includes(row.equipmentState)"
                @change="toggleSelected(row.equipmentState); selectRow(row.equipmentState)"
              >
            </td>
            <td class="eqstate-number">{{ row.equipmentState }}</td>
            <td>{{ stateTypeLabel(row.stateType, row.stateTypeText) }}</td>
            <td>{{ row.stateName }}</td>
            <td>
              <span class="eqstate-color-cell">
                <i :style="{ backgroundColor: colorCss(row.stateColor) }"></i>
                {{ row.stateColorHex || colorToHex(rgbHex(row.stateColor)) }}
              </span>
            </td>
            <td>{{ row.description || '' }}</td>
            <td>{{ utilizationLabel(row.utilizationType, row.utilizationTypeText) }}</td>
          </tr>
        </tbody>
      </table>
      </div>
      <aside class="eqstate-actions">
        <button type="button" class="eqstate-primary" :disabled="busy" @click="openForm('new')">新增(A)</button>
        <button type="button" :disabled="busy || !current" @click="openForm('edit')">修改(E)</button>
        <button type="button" :disabled="busy || (!current && selectedNumbers.length === 0)" @click="deleteSelected">刪除(D)</button>
        <button type="button" :disabled="busy || !current" @click="openForm('copy')">複製(C)</button>
        <button type="button" class="eqstate-close-page" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>

    <div v-if="mode" class="eqstate-overlay" @click.self="closeForm">
      <form class="eqstate-dialog" @submit.prevent="saveForm(false)">
        <header class="eqstate-dialog-header">
          <strong>{{ currentModeTitle }}</strong>
          <button type="button" class="eqstate-close" :disabled="busy" aria-label="關閉" @click="closeForm">×</button>
        </header>
        <fieldset class="eqstate-group">
          <legend>設備狀態資料</legend>
          <div class="eqstate-dialog-body">
          <label>
            <span>設備狀態編號 <b>*</b></span>
            <input ref="equipmentStateInput" v-model="form.equipmentState" class="eqstate-code-input" type="text" inputmode="numeric" :readonly="mode === 'edit'" @input="formDirty = true">
          </label>
          <label>
            <span>狀態類型 <b>*</b></span>
            <select ref="stateTypeInput" v-model.number="form.stateType" class="eqstate-type-input" @change="formDirty = true">
              <option v-for="option in stateTypeOptions" :key="option.value" :value="option.value">{{ stateTypeOptionLabel(option) }}</option>
            </select>
          </label>
          <label>
            <span>狀態名稱 <b>*</b></span>
            <input v-model="form.stateName" type="text" maxlength="50" @input="formDirty = true">
          </label>
          <div class="eqstate-color-field">
            <span>狀態顏色</span>
            <span class="eqstate-color-control">
              <span class="eqstate-color-preview" :style="{ backgroundColor: form.color }"></span>
              <button type="button" class="eqstate-color-button" @click="openColorPicker">選擇顏色</button>
              <input ref="colorInput" v-model="form.color" type="color" aria-label="選擇狀態顏色" @input="formDirty = true">
            </span>
          </div>
          <div class="eqstate-utilization">
            <span>稼動類型 <b>*</b></span>
            <span class="eqstate-radios">
              <label v-for="option in utilizationTypeOptions" :key="option.value">
                <input v-model.number="form.utilizationType" type="radio" :value="option.value" @change="formDirty = true">
                {{ utilizationTypeOptionLabel(option) }}
              </label>
            </span>
          </div>
          <label class="eqstate-description">
            <span>說明</span>
            <textarea v-model="form.description" maxlength="255" rows="4" @input="formDirty = true"></textarea>
          </label>
          <p class="eqstate-hint">使用者定義狀態編號不可使用 0–10；修改時狀態編號不可變更。</p>
          </div>
        </fieldset>
        <p v-if="error" class="eqstate-message eqstate-error" role="alert">{{ error }}</p>
        <footer class="eqstate-dialog-footer">
          <button type="submit" class="eqstate-primary" :disabled="busy">確定</button>
          <button type="button" :disabled="busy" @click="closeForm">關閉</button>
          <button type="button" :disabled="busy || !formDirty" @click="saveForm(true)">套用</button>
        </footer>
      </form>
    </div>
  </section>
</template>

<style scoped>
.eqstate-page{display:flex;min-height:0;height:100%;flex-direction:column;gap:6px;background:#fff;color:#333;font-size:13px}
.eqstate-main{display:grid;min-height:0;flex:1;grid-template-columns:minmax(0,1fr) 86px;gap:7px}
.eqstate-dev-note{padding:7px 10px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}

.eqstate-actions{display:flex;min-height:0;flex-direction:column;gap:6px}.eqstate-actions button{width:100%;min-height:32px}.eqstate-actions .eqstate-close-page{margin-top:auto}
.eqstate-actions button,.eqstate-dialog-footer button{min-height:29px;padding:4px 13px;border:1px solid #bcbcbc;border-radius:3px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.eqstate-actions button:hover,.eqstate-dialog-footer button:hover{background:#fff2c3;border-color:#c9a843}
.eqstate-actions button:disabled,.eqstate-dialog-footer button:disabled{cursor:not-allowed;opacity:.55}
.eqstate-actions .eqstate-primary,.eqstate-dialog-footer .eqstate-primary{border-color:#e49a24;background:#f5a623;color:#fff}
.eqstate-count{padding-right:4px;color:#777;font-size:12px;white-space:nowrap}
.eqstate-message{margin:0;padding:7px 10px;border:1px solid}
.eqstate-error{border-color:#dca7a2;background:#fff1ef;color:#942f24}
.eqstate-success{border-color:#c8d8ac;background:#f4f8eb;color:#496622}
.eqstate-grid-wrap{min-height:0;flex:1;overflow:auto;border:1px solid #bdbdbd;background:#fff}
.eqstate-grid{width:100%;border-collapse:collapse;white-space:nowrap}
.eqstate-grid th{position:sticky;top:0;z-index:1;padding:6px 8px;border:1px solid #d2c36d;background:#e7d978;color:#453d20;text-align:left;font-weight:600}
.eqstate-grid td{height:29px;padding:4px 8px;border:1px solid #d6d6d6}
.eqstate-grid tbody tr{cursor:pointer}
.eqstate-grid tbody tr:nth-child(even){background:#fafafa}
.eqstate-grid tbody tr:hover{background:#fff6d6}
.eqstate-grid tbody tr.eqstate-marked{background:#fff7dc}
.eqstate-grid tbody tr.eqstate-selected{background:#fff0b7}
.eqstate-grid .eqstate-check{width:29px;text-align:center}
.eqstate-grid .eqstate-number{text-align:right;font-variant-numeric:tabular-nums}
.eqstate-check input{margin:0;accent-color:#b38b24}
.eqstate-color-cell{display:inline-flex;align-items:center;gap:7px}
.eqstate-color-cell i,.eqstate-color-preview{display:inline-block;width:30px;height:17px;border:1px solid #777}
.eqstate-empty{height:52px!important;color:#777;text-align:center}
.eqstate-overlay{position:fixed;inset:0;z-index:80;display:grid;place-items:center;padding:18px;background:rgba(0,0,0,.3)}
.eqstate-dialog{display:flex;width:min(387px,100%);height:min(345px,calc(100vh - 36px));max-height:calc(100vh - 36px);flex-direction:column;overflow:auto;border:1px solid #999;background:#fdfdfd;box-shadow:0 5px 20px rgba(0,0,0,.22)}
.eqstate-dialog-header{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #c9c9c9;background:#f4f1e8;font-size:15px}
.eqstate-close{width:28px;height:26px;border:0;background:transparent;font-size:22px;cursor:pointer}
.eqstate-group{display:flex;min-height:0;flex:1;flex-direction:column;margin:5px 10px 6px;padding:2px 6px 5px;border:1px solid #bdbdbd}
.eqstate-group legend{padding:0 4px;color:#555}
.eqstate-dialog-body{display:flex;min-height:0;flex:1;flex-direction:column;gap:4px;overflow:auto;padding:2px 1px}
.eqstate-dialog-body>label,.eqstate-dialog-body>.eqstate-color-field,.eqstate-dialog-body>.eqstate-utilization{display:grid;min-width:0;grid-template-columns:96px minmax(0,1fr);align-items:center;gap:8px}
.eqstate-dialog-body>label>span:first-child,.eqstate-dialog-body>.eqstate-color-field>span:first-child,.eqstate-dialog-body>.eqstate-utilization>span:first-child{color:#555;text-align:right}
.eqstate-dialog-body b{color:#9c6714}
.eqstate-dialog input[type="text"],.eqstate-dialog input[type="number"],.eqstate-dialog select,.eqstate-dialog textarea{width:100%;min-height:24px;padding:3px 6px;border:1px solid #999;border-radius:0;background:#fff0b3;color:#333;font:inherit}
.eqstate-dialog .eqstate-code-input{max-width:58px;text-align:right}.eqstate-dialog .eqstate-type-input{max-width:119px}.eqstate-dialog input[type="text"]{max-width:154px}
.eqstate-dialog input[readonly]{background:#eee}
.eqstate-dialog textarea{resize:vertical}
.eqstate-color-control{position:relative;display:flex;min-height:24px;align-items:center;gap:4px}
.eqstate-color-control input{position:absolute;width:1px;height:1px;padding:0;border:0;opacity:0;pointer-events:none}
.eqstate-color-preview{width:74px;height:20px;border:0}
.eqstate-color-button{min-height:23px;padding:1px 7px;border:1px solid #aaa;border-radius:2px;background:#fafafa;color:#333;font:inherit;cursor:pointer}
.eqstate-radios{display:flex;min-height:24px;align-items:center;gap:18px}
.eqstate-radios label{display:flex;align-items:center;gap:5px}
.eqstate-radios input{accent-color:#ad7d18}
.eqstate-dialog-body .eqstate-description{align-items:start}.eqstate-dialog-body .eqstate-description textarea{max-width:239px;min-height:61px;grid-column:2}
.eqstate-hint{margin:0;color:#766332;font-size:12px}
.eqstate-dialog-footer{display:flex;justify-content:flex-end;gap:6px;padding:7px 10px;border-top:1px solid #d0d0d0}.eqstate-dialog-footer button{min-height:32px}
@media(max-width:680px){.eqstate-main{grid-template-columns:minmax(0,1fr) 76px}.eqstate-dialog{width:min(387px,100%)}.eqstate-grid{min-width:770px}}
</style>
