<script setup lang="ts">
import { computed, ref } from 'vue'

type State = -1 | 0 | 1 | 2
type ReferenceKind = 0 | 1 | 2
type Mode = 'add' | 'edit' | 'copy' | ''
type Rule = {
  boundary: 'max' | 'min'; opReference: ReferenceKind; referenceNo: string; referenceVersion: string; moTypeNo: number
  fromOpNo: string; toOpNo: string; fromBrNo: string; toBrNo: string; validMinute: number
  allowBetweenOpHold: boolean; eqpAvailableControl: boolean; mClassNo: string; holdDescription: string; holdItems: string[]
}
type TimeLimit = Rule & { issueState: State; dataStamp: number; creator: string; createDate: string; reviser?: string; reviseDate?: string }
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd106-time-limits.v1'
const kinds = [{ value: 0, label: '產品' }, { value: 1, label: '模組' }, { value: 2, label: '流程' }] as const
const refs = [
  { kind: 0, code: 'DEV-PRD-1002', version: '02', ops: ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30'] },
  { kind: 0, code: 'DEV-PRD-1005', version: '01', ops: ['DEV-OP-10', 'DEV-OP-30'] },
  { kind: 1, code: 'DEV-MODULE-20', version: '01', ops: ['DEV-OP-20', 'DEV-OP-30'] },
  { kind: 2, code: 'DEV-PRS-200', version: '02', ops: ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30'] },
]
const opNames: Record<string, string> = { 'DEV-OP-10': '點膠作業', 'DEV-OP-20': '組裝作業', 'DEV-OP-30': '終檢作業' }
const moTypes = [{ value: -1, label: '* 全部' }, { value: 1, label: '1 標準生產' }, { value: 2, label: '2 試產' }]
const brs = [{ value: 'N/A', label: 'N/A' }, { value: 'DEV-BR-CI', label: 'DEV-BR-CI CheckIn' }, { value: 'DEV-BR-CO', label: 'DEV-BR-CO CheckOut' }]
const holds = [{ main: 'DEV-MCLASS-A', sub: 'DEV-SCLASS-A1', no: 'DEV-HOLD-01', label: '製程時間超限' },
  { main: 'DEV-MCLASS-A', sub: 'DEV-SCLASS-A1', no: 'DEV-HOLD-02', label: '待複驗' },
  { main: 'DEV-MCLASS-A', sub: 'DEV-SCLASS-A2', no: 'DEV-HOLD-03', label: '確認作業紀錄' },
  { main: 'DEV-MCLASS-B', sub: 'DEV-SCLASS-B1', no: 'DEV-HOLD-04', label: '設備異常' }]

function blank(): Rule {
  return { boundary: 'max', opReference: 0, referenceNo: '', referenceVersion: '', moTypeNo: -1, fromOpNo: '', toOpNo: '', fromBrNo: 'N/A', toBrNo: 'N/A', validMinute: 0,
    allowBetweenOpHold: false, eqpAvailableControl: false, mClassNo: '', holdDescription: '', holdItems: [] }
}

function load(): TimeLimit[] {
  if (!props.developmentPreview) return []
  try {
    const saved = localStorage.getItem(storageKey)
    const parsed = saved ? JSON.parse(saved) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  return [{ ...blank(), referenceNo: 'DEV-PRD-1002', referenceVersion: '02', fromOpNo: 'DEV-OP-10', toOpNo: 'DEV-OP-20', validMinute: 30, issueState: 0, dataStamp: 0, creator: 'DEV001', createDate: '2026-11-19T08:00:00' }]
}

const rows = ref(load())
const states = [{ value: 0, label: '未核准' }, { value: 1, label: '簽核中' }, { value: 2, label: '已核准' }, { value: -1, label: '不使用' }] as const
const state = ref<State>(0)
const keyword = ref('')
const query = ref('')
const activeKey = ref('')
const marked = ref<string[]>([])
const keyOf = (row: Rule) => JSON.stringify([row.boundary, row.referenceNo, row.referenceVersion, row.moTypeNo, row.fromOpNo, row.toOpNo, row.fromBrNo, row.toBrNo])
const visible = computed(() => rows.value.filter(row => row.issueState === state.value && row.referenceNo.includes(query.value)))
const active = computed(() => visible.value.find(row => keyOf(row) === activeKey.value) ?? visible.value[0])
const editable = computed(() => active.value?.issueState === 0)
const mode = ref<Mode>('')
const draft = ref<Rule>(blank())
const editKey = ref('')
const referencePick = ref('')
const subClass = ref('')
const referenceKey = (row: { code: string; version: string }) => JSON.stringify([row.code, row.version])
const availableRefs = computed(() => refs.filter(row => row.kind === draft.value.opReference))
const currentReference = computed(() => availableRefs.value.find(row => referenceKey(row) === referencePick.value))
const opOptions = computed(() => currentReference.value?.ops ?? [])
const mainClasses = [...new Set(holds.map(row => row.main))]
const subClasses = computed(() => [...new Set(holds.filter(row => row.main === draft.value.mClassNo).map(row => row.sub))])
const holdOptions = computed(() => holds.filter(row => row.main === draft.value.mClassNo && (!subClass.value || row.sub === subClass.value)))
const error = ref('')
const notice = ref('')
const user = computed(() => props.currentUserNo || 'DEV001')

function saveRows(next: TimeLimit[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 設定。'; return false }
  rows.value = next; error.value = ''; return true
}

function chooseReference(): void {
  const reference = currentReference.value
  Object.assign(draft.value, { referenceNo: reference?.code ?? '', referenceVersion: reference?.version ?? '', fromOpNo: '', toOpNo: '', fromBrNo: 'N/A', toBrNo: 'N/A' })
}

function changeReferenceKind(): void {
  referencePick.value = ''; chooseReference()
  if (draft.value.opReference !== 0) draft.value.eqpAvailableControl = false
}

function open(next: Mode): void {
  const source = next === 'add' ? undefined : active.value
  draft.value = source ? { ...source, holdItems: [...source.holdItems] } : blank()
  referencePick.value = source ? referenceKey({ code: source.referenceNo, version: source.referenceVersion }) : ''
  editKey.value = source ? keyOf(source) : ''
  subClass.value = ''; error.value = ''; mode.value = next
}

function validate(rule: Rule): string {
  const ops = opOptions.value
  if (!currentReference.value || !ops.includes(rule.fromOpNo) || !ops.includes(rule.toOpNo)) return '請選取參照版次及起迄作業站。'
  if (!Number.isFinite(rule.validMinute) || rule.validMinute <= 0 || rule.validMinute > 99999.99 || Math.abs(rule.validMinute * 100 - Math.round(rule.validMinute * 100)) > 0.000001) return '時間須大於 0，最多兩位小數及 99999.99 分鐘。'
  if (ops.indexOf(rule.toOpNo) < ops.indexOf(rule.fromOpNo)) return '結束站不可早於起始站。'
  if (rule.fromOpNo === rule.toOpNo) {
    if (rule.fromBrNo !== 'DEV-BR-CI' || rule.toBrNo !== 'DEV-BR-CO') return '同站限制須使用 CheckIn 起算及 CheckOut 結束。'
    if (rule.allowBetweenOpHold) return '同站限制不可啟用中間站 Hold。'
  } else if ((rule.fromBrNo === 'N/A') !== (rule.toBrNo === 'N/A')) return '跨站的起迄規則須同時指定，或同為 N/A。'
  return ''
}

function save(close: boolean): void {
  const record = { ...draft.value, holdItems: [...draft.value.holdItems], holdDescription: draft.value.holdDescription.trim() }
  error.value = validate(record)
  if (error.value) return
  const key = keyOf(record)
  const old = mode.value === 'edit' ? rows.value.find(row => keyOf(row) === editKey.value) : undefined
  if (mode.value === 'edit' && (!old || old.issueState !== 0 || key !== editKey.value)) return
  if (!old && rows.value.some(row => keyOf(row) === key)) { error.value = '相同限制鍵已存在。'; return }
  const now = new Date().toISOString()
  const saved: TimeLimit = old
    ? { ...old, ...record, reviser: user.value, reviseDate: now, dataStamp: old.dataStamp + 1 }
    : { ...record, issueState: 0, dataStamp: 0, creator: user.value, createDate: now, reviser: undefined, reviseDate: undefined }
  if (!saveRows(old ? rows.value.map(row => keyOf(row) === key ? saved : row) : [...rows.value, saved])) return
  state.value = 0; query.value = ''; keyword.value = ''; activeKey.value = key; marked.value = []; editKey.value = key
  mode.value = close ? '' : 'edit'; notice.value = '生產時間限制已儲存。'
}

function changeState(action: 'approve' | 'delete'): void {
  if (!active.value || state.value === 1 || (action === 'approve' && !editable.value)) return
  const selected = new Set([...marked.value, keyOf(active.value)])
  if (!window.confirm(action === 'approve' ? '確定核准選取的時間限制？' : '確定刪除或停用選取的時間限制？')) return
  const next = rows.value.flatMap<TimeLimit>(row => {
    if (row.issueState !== state.value || !selected.has(keyOf(row))) return [row]
    if (action === 'delete' && row.issueState !== 2) return []
    return [{ ...row, issueState: action === 'approve' ? 2 : -1, reviser: user.value, reviseDate: new Date().toISOString(), dataStamp: row.dataStamp + 1 }]
  })
  if (saveRows(next)) { marked.value = []; notice.value = action === 'approve' ? '時間限制已核准。' : '時間限制已刪除或停用。' }
}
</script>

<template>
  <main class="time-master">
    <header><h1>生產時間限制（站與站）</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">時間限制等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase(); marked = []">
        <label v-for="item in states" :key="item.value"><input v-model="state" type="radio" :value="item.value" @change="marked = []" />{{ item.label }}</label>
        <input v-model="keyword" type="search" aria-label="參照編號" placeholder="參照編號" /><button>查詢</button>
      </form>
      <div class="toolbar"><button @click="open('add')">ADD</button><button :disabled="!editable" @click="open('edit')">EDIT</button><button :disabled="!active || state === 1" @click="changeState('delete')">DEL</button><button :disabled="!active" @click="open('copy')">Copy</button><button :disabled="!editable" @click="changeState('approve')">Approve</button></div>
      <div class="grid"><table><thead><tr><th>選取</th><th>Boundary</th><th>ReferenceNo</th><th>Version</th><th>MOType</th><th>FromOP</th><th>FromBR</th><th>ToOP</th><th>ToBR</th><th>分鐘</th><th>中間站 Hold</th><th>設備狀態檢核</th><th>MClassNo</th><th>Hold 說明</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="keyOf(row)" :class="{ selected: active && keyOf(active) === keyOf(row) }" @click="activeKey = keyOf(row)"><td><input v-model="marked" type="checkbox" :value="keyOf(row)" :aria-label="`選取 ${row.referenceNo} ${row.fromOpNo} ${row.toOpNo}`" /></td><td>{{ row.boundary }}</td><td><button @click="activeKey = keyOf(row)">{{ row.referenceNo }}</button></td><td>{{ row.referenceVersion }}</td><td>{{ row.moTypeNo === -1 ? '*' : row.moTypeNo }}</td><td>{{ row.fromOpNo }} {{ opNames[row.fromOpNo] }}</td><td>{{ row.fromBrNo }}</td><td>{{ row.toOpNo }} {{ opNames[row.toOpNo] }}</td><td>{{ row.toBrNo }}</td><td>{{ row.validMinute }}</td><td>{{ row.allowBetweenOpHold ? 'Yes' : 'No' }}</td><td>{{ row.eqpAvailableControl ? 'Yes' : 'No' }}</td><td>{{ row.mClassNo }}</td><td>{{ row.holdDescription }}</td><td>{{ row.creator }}</td><td>{{ row.createDate.replace('T', ' ').slice(0, 19) }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate?.replace('T', ' ').slice(0, 19) }}</td></tr>
        <tr v-if="!visible.length"><td colspan="18">沒有符合的生產時間限制。</td></tr></tbody></table></div>
      <p v-if="active?.holdItems.length">Hold 原因 {{ active.holdItems.join('、') }}</p>
      <p v-if="error && !mode" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay"><form class="editor" role="dialog" aria-modal="true" aria-label="生產時間限制設定" @submit.prevent="save(true)">
      <h2>生產時間限制 {{ mode.toUpperCase() }}</h2>
      <fieldset :disabled="mode === 'edit'" class="fields"><legend>時間限制鍵</legend>
        <label>Boundary <select v-model="draft.boundary"><option value="max">Max Time</option><option value="min">Min Time</option></select></label>
        <label>參照類型 <select v-model.number="draft.opReference" @change="changeReferenceKind"><option v-for="kind in kinds" :key="kind.value" :value="kind.value">{{ kind.label }}</option></select></label>
        <label>參照版次 <select v-model="referencePick" @change="chooseReference"><option value="">請選擇</option><option v-for="row in availableRefs" :key="referenceKey(row)" :value="referenceKey(row)">{{ row.code }} / {{ row.version }}</option></select></label>
        <label>MOType <select v-model.number="draft.moTypeNo"><option v-for="type in moTypes" :key="type.value" :value="type.value">{{ type.label }}</option></select></label>
        <label>FromOP <select v-model="draft.fromOpNo" @change="draft.fromBrNo = 'N/A'"><option value="">請選擇</option><option v-for="op in opOptions" :key="op">{{ op }}</option></select></label>
        <label>FromBR <select v-model="draft.fromBrNo"><option v-for="br in brs" :key="br.value" :value="br.value">{{ br.label }}</option></select></label>
        <label>ToOP <select v-model="draft.toOpNo" @change="draft.toBrNo = 'N/A'"><option value="">請選擇</option><option v-for="op in opOptions" :key="op">{{ op }}</option></select></label>
        <label>ToBR <select v-model="draft.toBrNo"><option v-for="br in brs" :key="br.value" :value="br.value">{{ br.label }}</option></select></label>
      </fieldset>
      <fieldset class="fields"><legend>限制與 Hold</legend>
        <label>ValidMinute <input v-model.number="draft.validMinute" type="number" min="0.01" max="99999.99" step="0.01" required /></label>
        <label class="checkbox"><input v-model="draft.eqpAvailableControl" type="checkbox" :disabled="draft.opReference !== 0" />啟用設備狀態檢核</label>
        <label class="checkbox"><input v-model="draft.allowBetweenOpHold" type="checkbox" />允許中間站觸發 Hold</label>
        <label>Hold 說明 <textarea v-model="draft.holdDescription" maxlength="4000" /></label>
        <label>MClassNo <select v-model="draft.mClassNo" @change="draft.holdItems = []; subClass = ''"><option value="">未指定</option><option v-for="code in mainClasses" :key="code">{{ code }}</option></select></label>
        <label>SClassNo <select v-model="subClass"><option value="">全部小類</option><option v-for="code in subClasses" :key="code">{{ code }}</option></select></label>
        <div v-if="holdOptions.length" class="holds"><label v-for="item in holdOptions" :key="item.no" class="checkbox"><input v-model="draft.holdItems" type="checkbox" :value="item.no" />{{ item.no }} {{ item.label }}</label></div>
      </fieldset>
      <p v-if="error" class="error" role="alert">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="save(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
    </form></div>
  </main>
</template>

<style scoped>
.time-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input,select,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:10px;width:min(680px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.fields{display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:8px;border:1px solid #dedbd0}.fields label{display:grid;gap:3px}.fields .checkbox{display:flex;align-items:center}.fields input:not([type=checkbox]),.fields select,.fields textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.holds{grid-column:1/-1;display:flex;flex-wrap:wrap;gap:8px}footer{justify-content:flex-end}
</style>
