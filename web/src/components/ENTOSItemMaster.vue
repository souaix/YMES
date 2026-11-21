<script setup lang="ts">
import { computed, defineAsyncComponent, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'
import { createEt202Service, editOsItem, copyOsItem, validateOsItemWrite, scalarLength, Et202Error,
  type OsItemDto, type OsItemWrite, type ProcessOption } from '../services/et202'

const props = withDefaults(defineProps<{ apiBaseUrl: string; currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const devEnabled = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const devComponent = computed(() => devEnabled.value ? defineAsyncComponent(() => import('./ENTOSItemDevPreview.vue')) : null)
type Mode = '' | 'add' | 'edit' | 'copy'
interface Draft { osItemNo: string; osItemName: string; planDays: string; processNo: string; returnType: number; description: string | null }
const rows = ref<OsItemDto[]>([]), query = ref(''), selected = ref<string[]>([]), active = ref(''), anchor = ref('')
const loading = ref(false), rowsReady = ref(false), saving = ref(false), deleting = ref(false), uncertain = ref(false)
const error = ref(''), notice = ref(''), mode = ref<Mode>(''), draft = ref<Draft>(blank())
const source = ref<OsItemDto | null>(null), savedAudit = ref<OsItemDto | null>(null), original = ref('')
const formLoading = ref(false), formReady = ref(false), formError = ref(''), formNotice = ref('')
const pickerOpen = ref(false), processes = ref<ProcessOption[]>([]), processQuery = ref(''), processLoading = ref(false), processError = ref('')
const editor = ref<HTMLElement | null>(null), picker = ref<HTMLElement | null>(null)
const queryInput = ref<HTMLInputElement | null>(null), queryButton = ref<HTMLButtonElement | null>(null)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
let disposed = false, listEpoch = 0, formEpoch = 0, processEpoch = 0
let listAbort: AbortController | null = null, formAbort: AbortController | null = null, processAbort: AbortController | null = null
let returnFocus: HTMLElement | null = null, pickerReturnFocus: HTMLElement | null = null
const writing = computed(() => saving.value || deleting.value)
const dirty = computed(() => JSON.stringify(draft.value) !== original.value)
const blocked = computed(() => writing.value || loading.value || mode.value !== '' || confirmationMessage.value !== null)
const single = computed(() => selected.value.length === 1 ? rows.value.find(row => row.osItemNo === selected.value[0]) : undefined)
const allSelected = computed(() => rows.value.length > 0 && rows.value.every(row => selected.value.includes(row.osItemNo)))
const title = computed(() => mode.value === 'edit' ? '修改外包項目' : mode.value === 'copy' ? '複製外包項目' : '新增外包項目')
const auditRow = computed(() => mode.value === 'edit' || savedAudit.value?.osItemNo === draft.value.osItemNo ? savedAudit.value : null)
const visibleProcesses = computed(() => {
  const text = processQuery.value.toLowerCase()
  return processes.value.filter(row => !text || row.processNo.toLowerCase().includes(text) || (row.description || '').toLowerCase().includes(text))
})
const service = () => createEt202Service(props.apiBaseUrl, window.location.origin)
const failure = (value: unknown): string => value instanceof Error ? value.message : '操作未完成，請重新查詢。'
function blank(): Draft { return { osItemNo: '', osItemName: '', planDays: '0', processNo: '', returnType: 0, description: '' } }
function adopt(body: OsItemWrite): void { draft.value = { ...body, planDays: String(body.planDays) }; original.value = JSON.stringify(draft.value) }
function body(): OsItemWrite {
  if (!/^\d+$/.test(draft.value.planDays)) throw new Error('預計天數必須為 0 到 999 的整數。')
  return validateOsItemWrite({ ...draft.value, planDays: Number(draft.value.planDays) })
}
function formatDate(value: string | null | undefined): string {
  if (value == null) return ''
  const date = new Date(value)
  return Number.isFinite(date.getTime()) ? new Intl.DateTimeFormat('zh-TW', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }).format(date) : value
}
function returnLabel(value: number): string { return value === 0 ? '不回原製程' : value === 1 ? '回原製程' : String(value) }
function current(epoch: number): boolean { return !disposed && epoch === formEpoch && mode.value !== '' }
function markFailure(value: unknown): string {
  if (value instanceof Et202Error && value.unknownWrite) uncertain.value = true
  return failure(value) + (uncertain.value ? ' 請先重新查詢核對，勿重複送出。' : '')
}
async function readRows(selectNo?: string): Promise<boolean> {
  if (devEnabled.value || disposed) return false
  const epoch = ++listEpoch; listAbort?.abort(); listAbort = new AbortController(); loading.value = true; rowsReady.value = false
  selected.value = []; active.value = ''; anchor.value = ''; error.value = ''
  try {
    const result = await service().list(query.value, listAbort.signal)
    if (disposed || epoch !== listEpoch || devEnabled.value) return false
    rows.value = result; rowsReady.value = true
    if (selectNo !== undefined && result.some(row => row.osItemNo === selectNo)) { selected.value = [selectNo]; active.value = selectNo; anchor.value = selectNo }
    return true
  } catch (value) {
    if (!disposed && epoch === listEpoch) { rows.value = []; error.value = failure(value) }
    return false
  } finally { if (!disposed && epoch === listEpoch) loading.value = false }
}
async function queryRows(): Promise<void> {
  if (devEnabled.value || writing.value || mode.value || confirmationMessage.value !== null) return
  const wasUncertain = uncertain.value; notice.value = ''
  if (await readRows()) {
    uncertain.value = false
    if (wasUncertain) notice.value = '已重新查詢，請核對上次操作的實際結果後再繼續。'
  }
}
function selectRow(row: OsItemDto, event: MouseEvent | KeyboardEvent): void {
  if (blocked.value || !rowsReady.value) return
  if (event.shiftKey && anchor.value) {
    const a = rows.value.findIndex(x => x.osItemNo === anchor.value), b = rows.value.indexOf(row)
    selected.value = a >= 0 ? rows.value.slice(Math.min(a, b), Math.max(a, b) + 1).map(x => x.osItemNo) : [row.osItemNo]
  } else if (event.ctrlKey || event.metaKey) {
    selected.value = selected.value.includes(row.osItemNo) ? selected.value.filter(no => no !== row.osItemNo) : [...selected.value, row.osItemNo]
    anchor.value = row.osItemNo
  } else { selected.value = [row.osItemNo]; anchor.value = row.osItemNo }
  active.value = selected.value.includes(row.osItemNo) ? row.osItemNo : selected.value[selected.value.length - 1] || ''
}
function toggleRow(row: OsItemDto, checked: boolean): void {
  if (blocked.value || !rowsReady.value) return
  selected.value = checked ? [...new Set([...selected.value, row.osItemNo])] : selected.value.filter(no => no !== row.osItemNo)
  active.value = checked ? row.osItemNo : selected.value[selected.value.length - 1] || ''; anchor.value = row.osItemNo
}
function toggleAll(checked: boolean): void {
  if (blocked.value || !rowsReady.value) return
  selected.value = checked ? rows.value.map(row => row.osItemNo) : []; active.value = selected.value[0] || ''; anchor.value = active.value
}
function tryReturnFocus(target: HTMLElement | null | undefined): boolean {
  if (!target?.isConnected || target.matches(':disabled') || target.closest('[inert],[hidden]') || target.getClientRects().length === 0) return false
  if (!target.matches('button,input:not([type="hidden"]),textarea,select,a[href],[tabindex],[contenteditable="true"]')) return false
  target.focus()
  return document.activeElement === target
}
function restoreReturnFocus(target: HTMLElement | null): void {
  if (disposed || tryReturnFocus(target)) return
  if (mode.value) {
    const field = editor.value?.querySelector<HTMLInputElement>(mode.value === 'edit' ? '[data-focus="edit"]' : '[data-focus="new"]')
    if (!tryReturnFocus(field)) tryReturnFocus(editor.value)
    return
  }
  if (!tryReturnFocus(queryInput.value)) tryReturnFocus(queryButton.value)
}
function hasUsableDialogFocus(shell: HTMLElement): boolean {
  const target = document.activeElement
  return target instanceof HTMLElement && shell.contains(target) && target.isConnected && !target.matches(':disabled') && !target.closest('[inert],[hidden]') && target.getClientRects().length > 0
    && target.matches('button,input:not([type="hidden"]),textarea,select,a[href],[tabindex],[contenteditable="true"]')
}
async function focusEditor(epoch = formEpoch, onlyIfLost = false): Promise<void> {
  await nextTick()
  const shell = editor.value
  if (!current(epoch) || devEnabled.value || pickerOpen.value || confirmationMessage.value !== null || !shell?.isConnected) return
  if (onlyIfLost && hasUsableDialogFocus(shell) && document.activeElement !== shell) return
  const target = shell.querySelector<HTMLInputElement>(mode.value === 'edit' ? '[data-focus="edit"]' : '[data-focus="new"]')
  if (!formReady.value || uncertain.value || !tryReturnFocus(target)) tryReturnFocus(shell)
}
async function restorePickerFocus(epoch: number, owner: number, target: HTMLElement | null): Promise<void> {
  await nextTick()
  const shell = picker.value
  if (!current(owner) || devEnabled.value || epoch !== processEpoch || !pickerOpen.value || writing.value || confirmationMessage.value !== null || !shell?.isConnected) return
  if (hasUsableDialogFocus(shell)) return
  if (target && shell.contains(target) && tryReturnFocus(target)) return
  if (!tryReturnFocus(shell.querySelector<HTMLInputElement>('[aria-label="流程編號查找條件"]'))) tryReturnFocus(shell)
}
async function openForm(next: Exclude<Mode, ''>): Promise<void> {
  if (devEnabled.value || blocked.value || !rowsReady.value || uncertain.value || (next !== 'add' && !single.value)) return
  const chosen = single.value; const epoch = ++formEpoch
  formAbort?.abort(); formAbort = new AbortController(); returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  mode.value = next; formReady.value = false; formLoading.value = next !== 'add'; source.value = null; savedAudit.value = null
  formError.value = ''; formNotice.value = ''; adopt({ osItemNo: '', osItemName: '', planDays: 0, processNo: '', returnType: 0, description: '' })
  await nextTick(); editor.value?.focus()
  if (next === 'add') { formReady.value = true; await focusEditor(epoch); return }
  try {
    const row = await service().get(chosen!.osItemNo, formAbort.signal)
    if (!current(epoch)) return
    if (row.osItemNo !== chosen!.osItemNo) throw new Error('資料身份已變更，請關閉後重新查詢。')
    if (next === 'edit') { source.value = row; savedAudit.value = row; adopt(editOsItem(row)) }
    else adopt(copyOsItem(row))
    formReady.value = true
  } catch (value) { if (current(epoch)) formError.value = failure(value) }
  finally { if (current(epoch)) { formLoading.value = false; await focusEditor(epoch) } }
}
function dismissForm(): void {
  ++formEpoch; formAbort?.abort(); closePicker(); mode.value = ''; formReady.value = false; source.value = null
  void nextTick(() => restoreReturnFocus(returnFocus))
}
async function closeForm(): Promise<void> {
  if (writing.value || confirmationMessage.value !== null) return
  if (pickerOpen.value) { closePicker(); return }
  const epoch = formEpoch
  if (dirty.value && !await requestConfirmation('尚有未儲存內容，要放棄變更嗎？')) return
  if (current(epoch)) dismissForm()
}
function closePage(): void {
  if (writing.value || confirmationMessage.value !== null || mode.value) return
  ++listEpoch; listAbort?.abort(); emit('close')
}
async function reconcile(): Promise<void> {
  if (writing.value || confirmationMessage.value !== null) return
  if (!await requestConfirmation('將關閉編輯並重新查詢，請核對上次操作的實際結果。')) return
  if (disposed) return
  dismissForm(); await queryRows()
}
async function save(closeAfter: boolean): Promise<void> {
  if (devEnabled.value || !mode.value || writing.value || formLoading.value || !formReady.value || pickerOpen.value || confirmationMessage.value !== null || uncertain.value) return
  if (mode.value === 'edit' && !dirty.value) { if (closeAfter) dismissForm(); return }
  let request: OsItemWrite
  try { request = body() } catch (value) { formError.value = failure(value); return }
  const epoch = formEpoch, existing = source.value; formError.value = ''; formNotice.value = ''; saving.value = true
  await nextTick(); editor.value?.focus()
  let saved: OsItemDto
  try {
    if (mode.value === 'edit') {
      if (!existing) throw new Error('未取得完整原始資料，請重新查詢。')
      saved = await service().update(existing.osItemNo, request, existing.dataStamp)
    } else saved = await service().create(request) // Add/Copy Apply deliberately stays Add/Copy.
  } catch (value) {
    if (current(epoch)) formError.value = markFailure(value)
    saving.value = false; if (current(epoch)) await focusEditor(epoch, true); return
  }
  if (!current(epoch)) { saving.value = false; return }
  savedAudit.value = saved; if (mode.value === 'edit') source.value = saved
  adopt(editOsItem(saved)); formNotice.value = '資料已儲存。'; notice.value = '資料已儲存。'
  if (closeAfter) dismissForm()
  await readRows(saved.osItemNo) // Read failure does not reclassify an acknowledged save or replay it.
  saving.value = false
  if (mode.value) await focusEditor(epoch, true)
  else await nextTick(() => restoreReturnFocus(returnFocus))
}
async function deleteSelected(): Promise<void> {
  if (devEnabled.value || blocked.value || !rowsReady.value || uncertain.value || !selected.value.length) return
  const queue = rows.value.filter(row => selected.value.includes(row.osItemNo)).map(row => ({ ...row }))
  if (!queue.length || !await requestConfirmation(`確定刪除選取的 ${queue.length} 筆外包項目？`)) return
  if (disposed || writing.value) return
  deleting.value = true; error.value = ''; notice.value = ''; let completed = 0; let caught: unknown
  for (const row of queue) {
    try {
      const result = await service().remove(row.osItemNo, row.dataStamp)
      if (disposed) { deleting.value = false; return }
      completed += result.deletedCount; rows.value = rows.value.filter(x => x.osItemNo !== row.osItemNo)
      selected.value = selected.value.filter(no => no !== row.osItemNo)
    } catch (value) { caught = value; break }
  }
  if (!disposed) {
    notice.value = `已刪除 ${completed}/${queue.length} 筆。`
    if (caught) error.value = markFailure(caught)
    const problem = error.value
    if (!uncertain.value) { await readRows(); if (problem) error.value = problem }
    active.value = ''; anchor.value = ''; deleting.value = false
  }
}
async function openPicker(): Promise<void> {
  if (!mode.value || writing.value || !formReady.value || uncertain.value || confirmationMessage.value !== null || pickerOpen.value) return
  pickerReturnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
  pickerOpen.value = true; processQuery.value = ''; processes.value = []; processError.value = ''
  await nextTick(); picker.value?.focus(); await loadProcesses()
}
async function loadProcesses(): Promise<void> {
  if (!pickerOpen.value || writing.value || devEnabled.value) return
  const target = document.activeElement instanceof HTMLElement ? document.activeElement : null
  const epoch = ++processEpoch, owner = formEpoch; processAbort?.abort(); processAbort = new AbortController(); processLoading.value = true; processError.value = ''
  try {
    const result = await service().processes(processAbort.signal)
    if (!disposed && epoch === processEpoch && owner === formEpoch && pickerOpen.value) processes.value = result
  } catch (value) { if (!disposed && epoch === processEpoch && pickerOpen.value) processError.value = failure(value) }
  finally { if (!disposed && epoch === processEpoch) { processLoading.value = false; await restorePickerFocus(epoch, owner, target) } }
}
function closePicker(): void {
  ++processEpoch; processAbort?.abort(); pickerOpen.value = false; processLoading.value = false
  void nextTick(() => { if (!disposed && mode.value) restoreReturnFocus(pickerReturnFocus) })
}
function chooseProcess(row: ProcessOption): void {
  if (!pickerOpen.value || processLoading.value || writing.value || uncertain.value || row.issueState !== 2 || row.processClass !== 3) return
  if (row.processNo.replace(/^ +| +$/g, '') === '' || scalarLength(row.processNo) > 30 || /[\u0000-\u001F\u007F-\u009F]/.test(row.processNo)) { processError.value = '此流程編號不符合外包項目的必填及 30 字元限制。'; return }
  draft.value.processNo = row.processNo; closePicker()
}
function trapTab(event: KeyboardEvent): void {
  const shell = event.currentTarget as HTMLElement
  const controls = Array.from(shell.querySelectorAll<HTMLElement>('button,input,textarea,select,[tabindex="0"]')).filter(x => !x.matches(':disabled') && !x.closest('[inert]') && x.getClientRects().length > 0)
  const first = controls[0], last = controls[controls.length - 1]
  if (!first) { event.preventDefault(); shell.focus(); return }
  if (!shell.contains(document.activeElement) || document.activeElement === shell || event.shiftKey && document.activeElement === first || !event.shiftKey && document.activeElement === last) {
    event.preventDefault(); (event.shiftKey ? last : first)?.focus()
  }
}
onMounted(() => { if (!devEnabled.value) void queryRows() })
onBeforeUnmount(() => { disposed = true; ++listEpoch; ++formEpoch; ++processEpoch; listAbort?.abort(); formAbort?.abort(); processAbort?.abort() })
</script>

<template>
  <component v-if="devEnabled && devComponent" :is="devComponent" :api-base-url="apiBaseUrl" :current-user-no="currentUserNo" :development-preview="true" @close="emit('close')" />
  <section v-else class="ositem-formal">
    <div class="page-layer" :inert="mode!=='' || writing || confirmationMessage!==null">
      <form class="query-strip" novalidate @submit.prevent="queryRows">
        <label for="et202-query">外包項目編號</label><input ref="queryInput" id="et202-query" v-model="query" class="kd-input" aria-label="外包項目編號查詢" :disabled="writing"><span>%</span>
        <button ref="queryButton" type="submit" class="btn" :disabled="loading || writing">查詢</button><span v-if="loading" role="status">載入中…</span>
      </form>
      <div class="work-area"><div class="grid-panel">
        <div class="toolbar">
          <button class="btn primary" type="button" :disabled="blocked || uncertain || !rowsReady" @click="openForm('add')">新增(A)</button>
          <button class="btn" type="button" :disabled="blocked || uncertain || !single" @click="openForm('edit')">修改(E)</button>
          <button class="btn" type="button" :disabled="blocked || uncertain || !selected.length || !rowsReady" @click="deleteSelected">刪除(D)</button>
          <button class="btn" type="button" :disabled="blocked || uncertain || !single" @click="openForm('copy')">複製(C)</button>
        </div>
        <div class="table-scroll"><table class="data-grid"><thead><tr>
          <th class="selector"><input type="checkbox" aria-label="選取所有外包項目" :checked="allSelected" :indeterminate="selected.length>0 && !allSelected" :disabled="blocked || !rowsReady || !rows.length" @change="toggleAll(($event.target as HTMLInputElement).checked)"></th>
          <th>外包項目編號</th><th>外包項目名稱</th><th>預計天數</th><th>流程編號</th><th>退回原流程</th><th>說明</th><th>建立人</th><th>建立日期</th><th>修改人</th><th>修改日期</th>
        </tr></thead><tbody>
          <tr v-for="row in rows" :key="row.osItemNo" :class="{selected:selected.includes(row.osItemNo),active:active===row.osItemNo}" :aria-selected="selected.includes(row.osItemNo)" :tabindex="blocked || !rowsReady ? -1 : 0" @click="selectRow(row,$event)" @keydown.enter.prevent="selectRow(row,$event)">
            <td class="selector" @click.stop><input type="checkbox" :checked="selected.includes(row.osItemNo)" :disabled="blocked || !rowsReady" :aria-label="'選取外包項目 '+row.osItemNo" @change="toggleRow(row,($event.target as HTMLInputElement).checked)"></td>
            <td>{{ row.osItemNo }}</td><td>{{ row.osItemName }}</td><td class="numeric">{{ row.planDays }}</td><td>{{ row.processNo }}</td><td>{{ returnLabel(row.returnType) }}</td><td class="description">{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ formatDate(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ formatDate(row.reviseDate) }}</td>
          </tr>
          <tr v-if="!rows.length"><td colspan="11" class="empty">{{ loading?'載入中…':rowsReady?'沒有符合條件的資料。':'尚未取得資料，請重新查詢。' }}</td></tr>
        </tbody></table></div><footer class="grid-footer">資料總筆數 {{ rows.length }}</footer>
      </div><aside class="side-actions"><button type="button" class="btn" :disabled="writing || confirmationMessage!==null" @click="closePage">關閉(X)</button></aside></div>
      <p v-if="error" class="notice error" role="alert">{{ error }}</p><p v-if="notice" class="notice success" role="status">{{ notice }}</p>
    </div>
    <Teleport to="body">
      <div v-if="mode" class="et202-shade" @click.self="closeForm"><section ref="editor" class="et202-dialog" role="dialog" aria-modal="true" :aria-label="title" tabindex="-1" :inert="pickerOpen || confirmationMessage!==null" @keydown.esc.stop.prevent="closeForm" @keydown.tab="trapTab">
        <div :inert="saving || formLoading"><header class="modal-heading"><h2>{{ title }}</h2><button class="btn" type="button" aria-label="關閉外包項目編輯" :disabled="writing" @click="closeForm">×</button></header>
          <form novalidate @submit.prevent="save(true)"><fieldset class="editor-fields" :disabled="writing || !formReady || uncertain"><legend>外包項目資料</legend>
            <label>外包項目編號<input v-model="draft.osItemNo" class="kd-input" :readonly="mode==='edit'" aria-label="外包項目編號" data-focus="new"></label>
            <label>外包項目名稱<input v-model="draft.osItemName" class="kd-input" aria-label="外包項目名稱" data-focus="edit"></label>
            <label>預計天數<span><input v-model="draft.planDays" class="kd-input days" inputmode="numeric" aria-label="預計天數"> 天</span></label>
            <label>流程編號<span class="process-input"><input v-model="draft.processNo" class="kd-input" aria-label="流程編號"><button class="btn" type="button" aria-label="查找流程" @click="openPicker">…</button></span></label>
            <div class="radio-row"><span>退回原流程</span><label><input v-model="draft.returnType" type="radio" :value="0">不回原流程</label><label><input v-model="draft.returnType" type="radio" :value="1">回原流程</label></div>
            <label>說明<textarea v-model="draft.description" class="kd-input" rows="4" aria-label="說明"></textarea></label>
          </fieldset><fieldset class="audit-fields"><legend>建立／修改資訊</legend>
            <label>建立人<input :value="auditRow?.creator ?? ''" readonly aria-label="外包項目建立人"></label><label>建立日期<input :value="formatDate(auditRow?.createDate)" readonly aria-label="外包項目建立日期"></label>
            <label>修改人<input :value="auditRow?.reviser ?? ''" readonly aria-label="外包項目修改人"></label><label>修改日期<input :value="formatDate(auditRow?.reviseDate)" readonly aria-label="外包項目修改日期"></label>
          </fieldset>
          <p v-if="formError" class="notice error" role="alert">{{ formError }}</p><p v-if="formNotice" class="notice success" role="status">{{ formNotice }}</p>
          <footer class="modal-actions"><button class="btn" type="button" :disabled="writing" @click="closeForm">關閉</button><button v-if="uncertain" class="btn" type="button" :disabled="writing" @click="reconcile">重新查詢核對</button>
            <button class="btn" type="button" :disabled="writing || !formReady || uncertain || mode==='edit' && !dirty" @click="save(false)">套用</button><button class="btn primary" type="submit" :disabled="writing || !formReady || uncertain">確定</button></footer>
          </form></div><p v-if="saving || formLoading" class="notice" role="status">{{ saving?'儲存中，請稍候…':'載入原始資料中…' }}</p>
      </section></div>
      <div v-if="pickerOpen" class="et202-shade picker-shade" @click.self="closePicker"><section ref="picker" class="et202-dialog picker-dialog" role="dialog" aria-modal="true" aria-label="選擇外包流程" tabindex="-1" @keydown.esc.stop.prevent="closePicker" @keydown.tab="trapTab">
        <header class="modal-heading"><h2>選擇流程（已簽核／外包類）</h2><button type="button" class="btn" aria-label="關閉流程查找" @click="closePicker">×</button></header>
        <form class="picker-search" @submit.prevent="loadProcesses"><label>流程編號<input v-model="processQuery" class="kd-input" aria-label="流程編號查找條件"></label><button class="btn" type="submit" :disabled="processLoading">查詢</button></form>
        <p v-if="processError" class="notice error" role="alert">{{ processError }}</p>
        <div class="table-scroll picker-scroll"><table class="data-grid"><thead><tr><th>流程編號</th><th>版本</th><th>說明</th></tr></thead><tbody>
          <tr v-for="row in visibleProcesses" :key="JSON.stringify([row.processNo,row.processVersion])" :tabindex="processLoading?-1:0" @click="chooseProcess(row)" @keydown.enter.prevent="chooseProcess(row)"><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.description }}</td></tr>
          <tr v-if="!visibleProcesses.length"><td colspan="3" class="empty">{{ processLoading?'流程資料載入中…':'沒有可選的已簽核外包流程。' }}</td></tr>
        </tbody></table></div><footer class="modal-actions"><button class="btn" type="button" @click="closePicker">關閉</button></footer>
      </section></div>
    </Teleport>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
:global(.framework-main:has(> .ositem-formal)){display:flex;flex-direction:column;height:100dvh;min-height:0;overflow:hidden}
:global(.framework-main:has(> .ositem-formal) > .framework-header){flex-shrink:0}
.ositem-formal.framework-content{flex:1;height:auto}
.ositem-formal{height:100%;max-height:100%;min-height:0;min-width:0;display:flex;flex-direction:column;overflow:hidden;box-sizing:border-box;background:#fff;color:#30302e;font:14px/1.4 "Segoe UI","Microsoft JhengHei",sans-serif}.page-layer{display:flex;flex:1;min-height:0;flex-direction:column}.query-strip{display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:8px 12px;border-bottom:1px solid #d5d1c8}.query-strip input{width:170px}.work-area{display:flex;flex:1;min-height:0;gap:10px;padding:8px}.grid-panel{display:flex;flex:1;min-width:0;min-height:0;flex-direction:column;border:1px solid #c8c4bb}.toolbar{display:flex;gap:5px;flex-wrap:wrap;padding:6px;border-bottom:1px solid #ccc7bc}.btn{min-height:30px;border:1px solid #bdb9b0;border-radius:3px;background:#faf9f6;color:#30302e;padding:4px 12px;white-space:nowrap;cursor:pointer;font:inherit}.btn:hover:not(:disabled){background:#eeece6}.btn:disabled{opacity:.5;cursor:default}.primary{background:#3e3d39;color:#fff;border-color:#3e3d39}.primary:hover:not(:disabled){background:#53514c}.table-scroll{flex:1;min-height:0;overflow:auto}.data-grid{border-collapse:separate;border-spacing:0;white-space:nowrap;min-width:1280px;width:100%}.data-grid th,.data-grid td{height:29px;padding:4px 7px;border-right:1px solid #d2cec4;border-bottom:1px solid #d2cec4;text-align:left}.data-grid th{position:sticky;top:0;z-index:1;background:#ece9e1;color:#30302e;font-weight:500}.data-grid tbody tr{cursor:pointer}.data-grid tbody tr:hover{background:#faf5dd}.data-grid tbody tr.selected{background:#fff0bb}.data-grid tbody tr.active{background:#f6e6a7}.data-grid .selector{width:28px;text-align:center}.selector input{accent-color:#61594c}.numeric{text-align:right!important}.description{min-width:250px;max-width:400px;white-space:pre-wrap;overflow-wrap:anywhere}.empty{text-align:center!important;color:#777;padding:18px!important}.grid-footer{padding:4px 8px;color:#666;border-top:1px solid #c8c4bb}.side-actions{display:flex;flex-direction:column;justify-content:flex-end;width:88px}.notice{margin:6px 12px;padding:7px 9px;overflow-wrap:anywhere}.error{color:#8e2525;background:#fff2ef;border:1px solid #d9b8b0}.success{color:#365b39;background:#f0f6ec;border:1px solid #c6d6bd}.kd-input{box-sizing:border-box;min-width:0;height:29px;border:1px solid #aaa69c;padding:3px 6px;background:#fff2bf;color:#282825;border-radius:0;font:inherit}.kd-input[readonly]{background:#ecebe7}.et202-shade{position:fixed;inset:0;z-index:900;display:flex;align-items:center;justify-content:center;box-sizing:border-box;padding:14px;background:#19181666;color:#30302e;font:14px/1.4 "Segoe UI","Microsoft JhengHei",sans-serif}.et202-dialog{box-sizing:border-box;width:min(560px,96vw);max-height:94vh;overflow:auto;outline:none;border:1px solid #aaa69c;background:#fff;box-shadow:0 6px 28px #0003}.modal-heading{display:flex;align-items:center;justify-content:space-between;padding:8px 12px;border-bottom:1px solid #c8c4bb;background:#f1eee6}.modal-heading h2{margin:0;font-size:17px;font-weight:500}.editor-fields{display:flex;flex-direction:column;gap:8px;margin:10px 12px;padding:10px 12px;border:1px solid #c8c4bb}.editor-fields>label{display:grid;grid-template-columns:110px minmax(0,1fr);align-items:center;gap:9px}.editor-fields legend,.audit-fields legend{padding:0 5px;color:#666}.days{width:90px}.process-input{display:flex;gap:5px}.process-input input{flex:1;min-width:0}.radio-row{display:flex;gap:12px;flex-wrap:wrap;align-items:center}.radio-row>span{width:107px}.radio-row label{display:flex;gap:4px;align-items:center}.radio-row input{accent-color:#61594c}.editor-fields textarea{height:auto;min-height:92px;resize:vertical}.audit-fields{display:grid;grid-template-columns:1fr 1fr;gap:7px 10px;margin:8px 12px;padding:10px;border:1px solid #c8c4bb}.audit-fields label{display:flex;gap:7px;align-items:center;white-space:nowrap}.audit-fields input{box-sizing:border-box;min-width:0;width:100%;height:27px;background:#eeede9;border:1px solid #c4c0b7;padding:3px 5px;color:#444}.modal-actions{display:flex;justify-content:flex-end;gap:8px;flex-wrap:wrap;padding:9px 12px}.picker-shade{z-index:950}.picker-dialog{width:min(700px,96vw)}.picker-search{display:flex;align-items:center;gap:8px;flex-wrap:wrap;padding:10px 12px}.picker-search label{display:flex;gap:8px;align-items:center}.picker-scroll{max-height:50vh;min-height:90px}.picker-scroll .data-grid{min-width:560px}.btn:focus-visible,input:focus-visible,textarea:focus-visible,tr:focus-visible{outline:2px solid #82734f;outline-offset:1px}@media(max-width:650px){.work-area{gap:5px;padding:5px}.side-actions{width:78px}.audit-fields{grid-template-columns:1fr}.editor-fields>label{grid-template-columns:90px minmax(0,1fr)}.et202-shade{padding:7px}}
</style>
