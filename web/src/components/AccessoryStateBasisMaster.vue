<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'

type StateType = 0 | 1
type FormMode = 'add' | 'edit' | 'copy'

interface StateRow {
  accessoryState: number
  stateType: StateType
  stateName: string
  stateColor: number
  description: string | null
  dataStamp: number | null
  creator: string | null
  createDate: string | null
  reviser: string | null
  reviseDate: string | null
}

interface StateForm {
  accessoryState: string
  stateType: string
  stateName: string
  stateColorHex: string
  description: string
  dataStamp: number | null
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}

const props = withDefaults(defineProps<{
  apiBaseUrl: string
  currentUserNo?: string
  developmentPreview?: boolean
}>(), {
  currentUserNo: '',
  developmentPreview: false,
})

const basePath = import.meta.env.BASE_URL.replace(/\/+$/, '')
const rowsKey = 'ymes-dev-mock:accessory-state-basis:ep205:v1'
const developmentPreview = computed(() => props.developmentPreview)
const rows = ref<StateRow[]>([])
const activeKey = ref<number | null>(null)
const selectedKeys = ref<number[]>([])
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const dialogMode = ref<FormMode | null>(null)
const form = ref<StateForm>(emptyForm())
const formError = ref('')

function emptyForm(): StateForm {
  return {
    accessoryState: '',
    stateType: '0',
    stateName: '',
    stateColorHex: '#808080',
    description: '',
    dataStamp: null,
    creator: '',
    createDate: '',
    reviser: '',
    reviseDate: '',
  }
}

function userNo(): string {
  return props.currentUserNo || 'DEV001'
}

function now(): string {
  return new Date().toISOString()
}

function colorHex(value: number): string {
  return '#' + ((Number(value) >>> 0) & 0xffffff).toString(16).padStart(6, '0')
}

function colorArgb(value: string): number {
  return (Number.parseInt(value.replace('#', ''), 16) | 0xff000000)
}

function stateTypeLabel(value: number): string {
  if (value === 0) return 'System'
  if (value === 1) return 'User Define'
  return ''
}

function dateText(value: string | null): string {
  if (!value) return ''
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('zh-TW', { hour12: false })
}

function readRows(): StateRow[] {
  try {
    const value = localStorage.getItem(rowsKey)
    if (!value) return structuredClone(seedRows)
    const parsed = JSON.parse(value) as unknown
    return Array.isArray(parsed) ? parsed as StateRow[] : structuredClone(seedRows)
  } catch {
    return structuredClone(seedRows)
  }
}

function writeRows(value: StateRow[]): void {
  localStorage.setItem(rowsKey, JSON.stringify(value))
}

const orderedRows = computed(() => [...rows.value].sort((a, b) => a.accessoryState - b.accessoryState))
const current = computed(() => rows.value.find(row => row.accessoryState === activeKey.value) ?? null)
const deleteTargets = computed(() => {
  if (selectedKeys.value.length) {
    const keys = new Set(selectedKeys.value)
    return rows.value.filter(row => keys.has(row.accessoryState))
  }
  return current.value ? [current.value] : []
})

function reconcile(): void {
  selectedKeys.value = selectedKeys.value.filter(key => rows.value.some(row => row.accessoryState === key))
  if (!rows.value.some(row => row.accessoryState === activeKey.value)) {
    activeKey.value = orderedRows.value[0]?.accessoryState ?? null
  }
}

function apiPath(path: string): string {
  if (!props.apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const root = new URL(props.apiBaseUrl.replace(/\/+$/, '') + '/', location.origin)
  return new URL(path.replace(/^\/+/, ''), root).toString()
}

async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiPath(path), { credentials: 'include', ...init })
  if (response.status === 401) {
    location.assign(basePath + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    if (response.status === 404) throw new Error('配件狀態 API 尚未建立，請依 todolist_homepc.md 完成資料表與 API。')
    throw new Error(body?.detail || body?.message || body?.title || 'API 回應錯誤 (' + response.status + ')。')
  }
  return response.status === 204 ? undefined as T : await response.json() as T
}

async function load(): Promise<void> {
  loading.value = true
  error.value = ''
  try {
    if (developmentPreview.value) {
      rows.value = readRows()
    } else {
      rows.value = await api<StateRow[]>('api/equipment/accessory-states')
    }
    reconcile()
  } catch (cause) {
    rows.value = []
    activeKey.value = null
    selectedKeys.value = []
    error.value = cause instanceof Error ? cause.message : '配件狀態資料載入失敗。'
  } finally {
    loading.value = false
  }
}

function choose(row: StateRow, event: MouseEvent): void {
  const previousActiveKey = activeKey.value
  const key = row.accessoryState
  const position = orderedRows.value.findIndex(item => item.accessoryState === key)
  activeKey.value = key
  error.value = ''
  if (event.shiftKey && previousActiveKey !== null) {
    const anchor = orderedRows.value.findIndex(item => item.accessoryState === previousActiveKey)
    const start = Math.min(Math.max(anchor, 0), position)
    const end = Math.max(anchor, position)
    selectedKeys.value = orderedRows.value.slice(start, end + 1).map(item => item.accessoryState)
  } else if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key)
      ? selectedKeys.value.filter(item => item !== key)
      : [...selectedKeys.value, key]
  } else {
    selectedKeys.value = [key]
  }
}

function openForm(mode: FormMode): void {
  const row = mode === 'add' ? null : current.value
  if (mode !== 'add' && !row) {
    error.value = '請先選取一筆配件狀態資料。'
    return
  }


  const audit = mode === 'edit' && row
    ? {
        creator: row.creator || '',
        createDate: dateText(row.createDate),
        reviser: userNo(),
        reviseDate: new Date().toLocaleString('zh-TW', { hour12: false }),
      }
    : {
        creator: userNo(),
        createDate: new Date().toLocaleString('zh-TW', { hour12: false }),
        reviser: '',
        reviseDate: '',
      }

  dialogMode.value = mode
  form.value = row
    ? {
        accessoryState: String(row.accessoryState),
        stateType: String(row.stateType),
        stateName: row.stateName,
        stateColorHex: colorHex(row.stateColor),
        description: row.description || '',
        dataStamp: row.dataStamp,
        ...audit,
      }
    : {
        ...emptyForm(),
        stateColorHex: '#808080',
        dataStamp: null,
        ...audit,
      }
  if (mode === 'copy' && row) form.value.accessoryState = String(row.accessoryState)
  formError.value = ''
  notice.value = ''

}

function validateForm(): string {
  const rawState = form.value.accessoryState.trim()
  const state = Number(rawState)
  const stateType = Number(form.value.stateType)
  if (!rawState) return '配件狀態為必填。'
  if (!Number.isInteger(state) || state < -99 || state > 99) return '配件狀態必須為 -99 到 99 的整數。'
  if (stateType !== 0 && stateType !== 1) return '請選擇有效的狀態類別。'
  if (stateType === 1 && state >= 0 && state <= 10) return 'User Define 狀態不可使用 0 到 10。'
  if (!form.value.stateName.trim()) return '狀態名稱為必填。'
  if (form.value.stateName.length > 50) return '狀態名稱不可超過 50 個字元。'
  if (form.value.description.length > 255) return '說明不可超過 255 個字元。'
  if (!/^#[0-9a-fA-F]{6}$/.test(form.value.stateColorHex)) return '請設定有效的狀態色彩。'
  if (dialogMode.value !== 'edit' && rows.value.some(row => row.accessoryState === state)) return '配件狀態編號已存在。'
  return ''
}

async function saveForm(closeAfterSave: boolean): Promise<void> {
  if (!dialogMode.value) return
  const problem = validateForm()
  if (problem) {
    formError.value = problem
    return
  }

  const mode = dialogMode.value
  const state = Number(form.value.accessoryState)
  const payload = {
    accessoryState: state,
    stateType: Number(form.value.stateType),
    stateName: form.value.stateName.trim(),
    stateColor: colorArgb(form.value.stateColorHex),
    description: form.value.description || null,
    dataStamp: form.value.dataStamp,
  }

  busy.value = true
  formError.value = ''
  try {
    if (developmentPreview.value) {
      const next = readRows()
      const stamp = now()

      if (mode === 'edit') {
        const index = next.findIndex(row => row.accessoryState === state)
        if (index < 0) throw new Error('找不到要修改的配件狀態。')
        next[index] = {
          ...next[index],
          stateType: payload.stateType as StateType,
          stateName: payload.stateName,
          stateColor: payload.stateColor,
          description: payload.description,
          dataStamp: (next[index].dataStamp ?? 0) + 1,
          reviser: userNo(),
          reviseDate: stamp,
        }
      } else {
        if (next.some(row => row.accessoryState === state)) throw new Error('配件狀態編號已存在。')
        next.push({
          ...payload,
          stateType: payload.stateType as StateType,
          description: payload.description,
          dataStamp: 0,
          creator: userNo(),
          createDate: stamp,
          reviser: null,
          reviseDate: null,
        })
      }
      writeRows(next)
    } else if (mode === 'edit') {
      await api('api/equipment/accessory-states/' + encodeURIComponent(String(state)), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    } else {
      await api('api/equipment/accessory-states', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
    }

    await load()
    activeKey.value = state
    selectedKeys.value = [state]
    notice.value = mode === 'edit' ? '配件狀態已修改。' : '配件狀態已新增。'

    if (mode === 'edit') {
      const updated = rows.value.find(row => row.accessoryState === state)
      if (updated) {
        form.value.dataStamp = updated.dataStamp
        form.value.reviser = userNo()
        form.value.reviseDate = new Date().toLocaleString('zh-TW', { hour12: false })
      }
    }
    if (closeAfterSave) dialogMode.value = null
  } catch (cause) {
    formError.value = cause instanceof Error ? cause.message : '儲存配件狀態失敗。'
    if (!developmentPreview.value) await load()
  } finally {
    busy.value = false
  }
}

async function removeSelected(): Promise<void> {
  const targets = deleteTargets.value
  if (!targets.length || busy.value) return
  const prompt = targets.length === 1
    ? '確定刪除配件狀態 ' + targets[0].accessoryState + '？'
    : '確定刪除選取的 ' + targets.length + ' 筆配件狀態？'
  if (!window.confirm(prompt)) return

  busy.value = true
  error.value = ''
  notice.value = ''
  try {
    if (developmentPreview.value) {
      const deleted = new Set(targets.map(row => row.accessoryState))
      writeRows(readRows().filter(row => !deleted.has(row.accessoryState)))
    } else {
      for (const row of targets) {
        await api('api/equipment/accessory-states/' + encodeURIComponent(String(row.accessoryState)), {
          method: 'DELETE',
        })
      }
    }
    selectedKeys.value = []
    await load()
    notice.value = '配件狀態已刪除。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '刪除配件狀態失敗。'
    if (!developmentPreview.value) await load()
  } finally {
    busy.value = false
  }
}

function closePage(): void {
  history.pushState({}, '', basePath + '/Equipment')
  dispatchEvent(new PopStateEvent('popstate'))
}

function closeDialog(): void {
  if (!busy.value) dialogMode.value = null
}

const dialogTitle = computed(() => {
  if (dialogMode.value === 'add') return '設備配件狀態定義（新增）'
  if (dialogMode.value === 'edit') return '設備配件狀態定義（修改）'
  if (dialogMode.value === 'copy') return '設備配件狀態定義（複製）'
  return ''
})

const seedRows: StateRow[] = [
  { accessoryState: 0, stateType: 0, stateName: 'DEV 未安裝', stateColor: -8355712, description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-16T08:00:00', reviser: null, reviseDate: null },
  { accessoryState: 1, stateType: 0, stateName: 'DEV 可用', stateColor: -16744448, description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-16T08:00:00', reviser: null, reviseDate: null },
  { accessoryState: 2, stateType: 0, stateName: 'DEV 使用中', stateColor: -16776961, description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-16T08:00:00', reviser: null, reviseDate: null },
  { accessoryState: 3, stateType: 0, stateName: 'DEV 待維修', stateColor: -23296, description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-16T08:00:00', reviser: null, reviseDate: null },
  { accessoryState: 11, stateType: 1, stateName: 'DEV 備品', stateColor: -7667712, description: 'Development 假資料', dataStamp: 0, creator: 'DEV001', createDate: '2026-11-16T08:00:00', reviser: null, reviseDate: null },
]

onMounted(() => { void load() })
watch(() => [props.developmentPreview, props.apiBaseUrl], () => { void load() })
</script>

<template>
  <section class="state-basis-page">
    <header class="page-heading">
      <h1>配件狀態設定</h1>
      <span v-if="developmentPreview" class="source-badge">Development 假資料</span>
    </header>

    <p v-if="developmentPreview" class="mock-note">本機 Development 假資料，只存於瀏覽器 localStorage。</p>
    <p v-if="error" class="message error" role="alert">{{ error }}</p>
    <p v-if="notice" class="message success" role="status">{{ notice }}</p>

    <div class="master-layout">
      <section class="grid-frame" aria-label="配件狀態清單">
        <div class="grid-scroll">
          <table class="state-grid">
            <thead>
              <tr>
                <th class="row-selector" aria-label="資料列選取"></th>
                <th>配件狀態</th>
                <th>狀態類別</th>
                <th>狀態名稱</th>
                <th>狀態色彩</th>
                <th>說明</th>
                <th>建立人</th>
                <th>建立日期</th>
                <th>修改人</th>
                <th>修改日期</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="row in orderedRows"
                :key="row.accessoryState"
                :class="{ active: activeKey === row.accessoryState, selected: selectedKeys.includes(row.accessoryState) }"
                @click="choose(row, $event)"
              >
                <td class="row-selector"><span class="row-marker">{{ activeKey === row.accessoryState ? '▶' : '' }}</span></td>
                <td class="number-cell">{{ row.accessoryState }}</td>
                <td>{{ stateTypeLabel(row.stateType) }}</td>
                <td>{{ row.stateName }}</td>
                <td><span class="color-chip" :style="{ backgroundColor: colorHex(row.stateColor) }"></span></td>
                <td>{{ row.description }}</td>
                <td>{{ row.creator }}</td>
                <td>{{ dateText(row.createDate) }}</td>
                <td>{{ row.reviser }}</td>
                <td>{{ dateText(row.reviseDate) }}</td>
              </tr>
              <tr v-if="!orderedRows.length">
                <td colspan="10" class="empty-row">{{ loading ? '載入中…' : '查無資料' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <aside class="action-column" aria-label="配件狀態操作">
        <button class="win-button primary" :disabled="busy" @click="openForm('add')">新增(A)</button>
        <button class="win-button" :disabled="busy || !current" @click="openForm('edit')">修改(E)</button>
        <button class="win-button" :disabled="busy || !current" @click="openForm('copy')">複製(C)</button>
        <button class="win-button" :disabled="busy || !deleteTargets.length" @click="removeSelected">刪除(D)</button>
        <button class="win-button close-button" :disabled="busy" @click="closePage">關閉</button>
      </aside>
    </div>
  </section>

  <div v-if="dialogMode" class="dialog-backdrop" @click.self="closeDialog">
    <section class="definition-dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
      <header class="dialog-title">{{ dialogTitle }}</header>
      <fieldset class="definition-group">
        <legend>EQP ACC State Data</legend>
        <label class="field-row">
          <span>配件狀態</span>
          <input
            v-model="form.accessoryState"
            class="kd-input state-number"
            inputmode="numeric"
            :readonly="dialogMode === 'edit'"
            :disabled="busy"
          >
        </label>
        <label class="field-row">
          <span>狀態類別</span>
          <select v-model="form.stateType" class="kd-input" :disabled="busy">
            <option value="0">System</option>
            <option value="1">User Define</option>
          </select>
        </label>
        <label class="field-row">
          <span>狀態名稱</span>
          <input v-model="form.stateName" class="kd-input" maxlength="50" :disabled="busy">
        </label>
        <label class="field-row color-row">
          <span>狀態色彩</span>
          <span class="color-editor">
            <span class="color-preview" :style="{ backgroundColor: form.stateColorHex }"></span>
            <input v-model="form.stateColorHex" type="color" aria-label="選擇狀態色彩" :disabled="busy">
          </span>
        </label>
        <label class="field-row description-row">
          <span>說明</span>
          <textarea v-model="form.description" class="kd-input description-input" maxlength="255" :disabled="busy"></textarea>
        </label>
      </fieldset>

      <fieldset class="audit-group">
        <legend>資料異動資訊</legend>
        <label><span>建立人</span><input :value="form.creator" readonly></label>
        <label><span>建立日期</span><input :value="form.createDate" readonly></label>
        <label><span>修改人</span><input :value="form.reviser" readonly></label>
        <label><span>修改日期</span><input :value="form.reviseDate" readonly></label>
      </fieldset>

      <p v-if="formError" class="message error" role="alert">{{ formError }}</p>
      <footer class="dialog-actions">
        <button class="win-button primary" :disabled="busy" @click="saveForm(true)">套用</button>
        <button class="win-button" :disabled="busy" @click="closeDialog">關閉</button>
        <button class="win-button confirm-button" :disabled="busy" @click="saveForm(false)">確定</button>
      </footer>
    </section>
  </div>
</template>

<style scoped>
.state-basis-page{box-sizing:border-box;display:flex;flex-direction:column;min-width:0;height:100%;padding:10px 12px;background:#fff}
.page-heading{display:flex;align-items:center;justify-content:space-between;min-height:42px;border-bottom:1px solid #d6d2c8}
.page-heading h1{margin:0;color:#35332e;font-size:21px;font-weight:600}
.source-badge{padding:4px 9px;border:1px solid #c8a861;background:#fff3c4;color:#755315;font-size:12px}
.mock-note{margin:6px 0;color:#776a53;font-size:12px}
.message{margin:7px 0;padding:7px 9px;border:1px solid #d2cfc7;font-size:13px}
.message.error{border-color:#ca8a82;background:#fff0ed;color:#8b2820}
.message.success{border-color:#a7c49a;background:#f1f8ec;color:#315d24}
.master-layout{display:flex;flex:1;gap:8px;min-height:0;margin-top:8px}
.grid-frame{display:flex;flex:1;min-width:0;min-height:0;padding:7px;border:1px solid #bcb8af}
.grid-scroll{width:100%;overflow:auto}
.state-grid{width:100%;min-width:1040px;border-collapse:collapse;table-layout:fixed;color:#25231e;font-size:13px}
.state-grid th,.state-grid td{height:28px;padding:3px 7px;border:1px solid #c9c6bf;overflow:hidden;text-align:left;text-overflow:ellipsis;white-space:nowrap}
.state-grid th{position:sticky;top:0;z-index:1;background:#ffe080;color:#3b3426;font-weight:600}
.state-grid tbody tr{cursor:default}
.state-grid tbody tr:hover{background:#fff6d9}
.state-grid tbody tr.selected{background:#ffecad}
.state-grid tbody tr.active .row-selector{background:#f6d46b}
.row-selector{width:27px;padding:0!important;text-align:center!important}
.row-marker{color:#514320;font-size:10px}
.number-cell{text-align:right!important}
.state-grid th:nth-child(2){width:95px}.state-grid th:nth-child(3){width:115px}.state-grid th:nth-child(4){width:165px}.state-grid th:nth-child(5){width:100px}.state-grid th:nth-child(6){width:220px}.state-grid th:nth-child(7),.state-grid th:nth-child(9){width:90px}.state-grid th:nth-child(8),.state-grid th:nth-child(10){width:145px}
.color-chip{display:inline-block;width:50px;height:17px;border:1px solid #777;vertical-align:middle}
.empty-row{height:70px!important;text-align:center!important;color:#777}
.action-column{display:flex;flex:0 0 90px;flex-direction:column;gap:8px}
.win-button{min-height:34px;padding:5px 8px;border:1px solid #b6b2aa;border-radius:3px;background:linear-gradient(#fff,#f0efec);color:#282620;font-size:13px;white-space:nowrap}
.win-button:hover:not(:disabled){border-color:#9b8451;background:#fff4cf}
.win-button:disabled{color:#aaa;opacity:.72}
.win-button.primary{border-color:#d69734;background:#ffdc79;color:#513d1b}
.win-button.close-button{margin-top:auto}
.dialog-backdrop{position:fixed;inset:0;z-index:1000;display:flex;align-items:center;justify-content:center;padding:18px;background:#0006}
.definition-dialog{box-sizing:border-box;width:min(520px,calc(100vw - 28px));padding:14px 15px 12px;border:1px solid #929087;background:#f7f6f2;box-shadow:0 8px 26px #0004;color:#292721}
.dialog-title{margin:-2px 0 8px;font-size:17px;font-weight:600}
.definition-group,.audit-group{margin:5px 0 9px;padding:9px 12px 11px;border:1px solid #c2beb5}
.definition-group legend,.audit-group legend{padding:0 5px;color:#575248;font-size:13px}
.field-row{display:grid;grid-template-columns:90px minmax(0,1fr);align-items:center;gap:9px;margin:5px 0}
.field-row>span:first-child{text-align:right}
.kd-input{box-sizing:border-box;width:100%;height:28px;padding:4px 7px;border:1px solid #8c887f;border-radius:0;background:#fff0ae;color:#282620;font:14px "Microsoft JhengHei",Arial,sans-serif}
.kd-input:disabled,.kd-input:read-only{background:#efeee9;color:#68655e}
.state-number{max-width:100px;text-align:right}
.color-editor{display:flex;align-items:center;gap:8px}
.color-preview{width:74px;height:22px;border:1px solid #777}
.color-editor input[type=color]{width:48px;height:29px;padding:2px;border:1px solid #aaa;background:#f3f1eb}
.description-row{align-items:start}
.description-input{height:90px;resize:vertical}
.audit-group{display:grid;grid-template-columns:1fr 1fr;gap:7px}
.audit-group label{display:grid;grid-template-columns:67px minmax(0,1fr);align-items:center;gap:6px;font-size:12px}
.audit-group label span{text-align:right}
.audit-group input{box-sizing:border-box;width:100%;min-width:0;height:27px;padding:3px 5px;border:1px solid #b7b4ad;background:#efeee9;color:#666}
.dialog-actions{display:flex;justify-content:flex-end;gap:7px;padding-top:4px}
.confirm-button{border-color:#d69734;background:#f5b84c;color:#fff}
@media(max-width:760px){.state-basis-page{height:auto;min-height:100%;padding:8px}.master-layout{flex-direction:column}.action-column{flex:0 0 auto;flex-direction:row;flex-wrap:wrap}.action-column .close-button{margin:0 0 0 auto}.audit-group{grid-template-columns:1fr}.state-grid{min-width:980px}}
</style>
