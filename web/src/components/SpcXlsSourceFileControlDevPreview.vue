<script setup lang="ts">
import { computed, ref } from 'vue'

type TemplateType = 0 | 1 | 2 | 3
type FormMode = 'idle' | 'add' | 'edit' | 'copy'
interface TemplateRecord {
  id: number; xlsNo: string; xlsType: TemplateType; xlsName: string; xlsFileName: string; description: string
  creator: string; createDate: string; revisor: string; reviseDate: string; mockSize: number
}
interface FileChoice { name: string; size: number }
interface TemplateForm { xlsNo: string; xlsName: string; xlsFileName: string; description: string; creator: string; createDate: string; revisor: string; reviseDate: string }

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const seedTemplates: Record<TemplateType, Omit<TemplateRecord, 'id'>[]> = {
  0: [
    { xlsNo: 'MESSERIES1', xlsType: 0, xlsName: 'SPCExport_STD_190111', xlsFileName: 'SPCExport_STD_190111.xls', description: 'Control Chart 標準匯出樣板', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 28672 },
    { xlsNo: 'MESSERIES2', xlsType: 0, xlsName: 'SPCExport_STD_190111H', xlsFileName: 'SPCExport_STD_190111H.xlsm', description: 'Control Chart 巨量資料樣板', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 40960 },
  ],
  1: [
    { xlsNo: 'AutoRPTVarMonthReport', xlsType: 1, xlsName: 'AutoRPTVarMonthReport', xlsFileName: 'AutoRPTVarMonthReport.xls', description: '計量月報自動寄送樣板', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 22528 },
    { xlsNo: 'AutoRPTMeasureData', xlsType: 1, xlsName: 'AutoRPTMeasureData', xlsFileName: 'AutoRPTMeasureData.xls', description: '計量量測數據自動寄送樣板', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 19456 },
  ],
  2: [
    { xlsNo: 'AutoRPTPareto', xlsType: 2, xlsName: 'AutoRPTPareto', xlsFileName: 'AutoRPTPareto.xls', description: '計數柏拉圖自動寄送樣板', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 17408 },
  ],
  3: [
    { xlsNo: 'MESSERIES1', xlsType: 3, xlsName: 'COAExport_STD01', xlsFileName: 'COAExport_STD01.xls', description: 'COA 出貨報告樣板一', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 30720 },
    { xlsNo: 'MESSERIES2', xlsType: 3, xlsName: 'COAExport_STD02', xlsFileName: 'COAExport_STD02.xls', description: 'COA 出貨報告樣板二', creator: 'IMES', createDate: '2026-01-05 09:00:00', revisor: '', reviseDate: '', mockSize: 32768 },
  ],
}
const templates = ref<TemplateRecord[]>(seedTemplates[0].map((row, index) => ({ ...row, id: index + 1 })))
const activeType = ref<TemplateType>(0)
const activeId = ref<number | null>(null)
const selectedIds = ref<number[]>([])
const formMode = ref<FormMode>('idle')
const selectedFile = ref<FileChoice | null>(null)
const copySourceId = ref<number | null>(null)
const deleteDialogOpen = ref(false)
const nextId = ref(10)
const notice = ref('選擇樣板類別後可瀏覽、維護或下載 DEV 樣板；所有檔案內容均為虛構資料。')
const noticeKind = ref<'info' | 'success' | 'error'>('info')
const form = ref<TemplateForm>(emptyForm())

function emptyForm(): TemplateForm { return { xlsNo: '', xlsName: '', xlsFileName: '', description: '', creator: '', createDate: '', revisor: '', reviseDate: '' } }
const typeLabels: Record<TemplateType, string> = { 0: 'Control Chart', 1: 'AutoRPT · Variable', 2: 'AutoRPT · Attribute', 3: 'COA' }
const templateTypes: TemplateType[] = [0, 1, 2, 3]
const visibleTemplates = computed(() => templates.value.filter((template) => template.xlsType === activeType.value).sort((a, b) => a.xlsNo.localeCompare(b.xlsNo)))
const activeTemplate = computed(() => templates.value.find((template) => template.id === activeId.value) || null)
const isEditing = computed(() => formMode.value !== 'idle')
const effectiveDeleteIds = computed(() => selectedIds.value.length ? selectedIds.value : activeId.value === null ? [] : [activeId.value])

function setNotice(message: string, kind: 'info' | 'success' | 'error' = 'info'): void { notice.value = message; noticeKind.value = kind }
function ensureSeed(type: TemplateType): void {
  if (templates.value.some((template) => template.xlsType === type)) return
  templates.value.push(...seedTemplates[type].map((template) => ({ ...template, id: nextId.value++ })))
  setNotice(`${typeLabels[type]} 清單為空，載入本機 DEV 樣板範例。`)
}
function setType(type: TemplateType): void {
  if (type === activeType.value) return
  activeType.value = type
  activeId.value = null
  selectedIds.value = []
  formMode.value = 'idle'
  selectedFile.value = null
  copySourceId.value = null
  form.value = emptyForm()
  ensureSeed(type)
}
function activate(template: TemplateRecord): void {
  activeId.value = template.id
  form.value = { xlsNo: template.xlsNo, xlsName: template.xlsName, xlsFileName: template.xlsFileName, description: template.description, creator: template.creator, createDate: template.createDate, revisor: template.revisor, reviseDate: template.reviseDate }
}
function onSelectRow(template: TemplateRecord): void { activate(template) }
function toggleSelected(id: number, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked
  selectedIds.value = checked ? [...new Set([...selectedIds.value, id])] : selectedIds.value.filter((selectedId) => selectedId !== id)
}
function startAdd(): void {
  formMode.value = 'add'
  selectedFile.value = null
  copySourceId.value = null
  form.value = { ...emptyForm(), creator: 'DEV-USER', createDate: '2026-11-23 10:00:00' }
}
function startEdit(): void {
  if (!activeTemplate.value) return setNotice('請先選取要編輯的樣板。', 'error')
  activate(activeTemplate.value)
  formMode.value = 'edit'
  selectedFile.value = null
  copySourceId.value = null
}
function startCopy(): void {
  if (!activeTemplate.value) return setNotice('請先選取要複製的樣板。', 'error')
  const source = activeTemplate.value
  activate(source)
  copySourceId.value = source.id
  selectedFile.value = null
  formMode.value = 'copy'
}
function onFileSelected(event: Event): void {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  const extension = file.name.split('.').pop()?.toLowerCase() || ''
  if (!['xls', 'xlsx', 'xlsm'].includes(extension)) {
    selectedFile.value = null
    setNotice('只接受 .xls、.xlsx 或 .xlsm 檔名。', 'error')
    return
  }
  selectedFile.value = { name: file.name, size: file.size }
  form.value.xlsFileName = file.name
  setNotice(`已選取 ${file.name}（DEV 只記錄檔名與大小，不讀取或上傳內容）。`)
}
function confirmForm(): void {
  form.value.xlsNo = form.value.xlsNo.trim().toUpperCase()
  if (!form.value.xlsNo) return setNotice('XlsNo 為必填。', 'error')
  if (!form.value.xlsFileName) return setNotice('請選擇 Excel 檔案。', 'error')
  if (form.value.xlsName.length > 25) return setNotice('XlsName 最多 25 字。', 'error')
  if (formMode.value === 'add' || formMode.value === 'copy') {
    if (templates.value.some((template) => template.xlsType === activeType.value && template.xlsNo.toUpperCase() === form.value.xlsNo)) return setNotice('同一類別已有相同 XlsNo（DEV 防重）。', 'error')
    const copiedSource = templates.value.find((template) => template.id === copySourceId.value)
    if (formMode.value === 'add' && !selectedFile.value) return setNotice('新增樣板需選取 Excel 檔案。', 'error')
    if (formMode.value === 'copy' && !copiedSource) return setNotice('找不到要複製的來源樣板。', 'error')
    const newTemplate: TemplateRecord = {
      id: nextId.value++, xlsNo: form.value.xlsNo, xlsType: activeType.value, xlsName: form.value.xlsName,
      xlsFileName: selectedFile.value?.name || copiedSource?.xlsFileName || form.value.xlsFileName,
      description: form.value.description, creator: form.value.creator || 'DEV-USER', createDate: form.value.createDate || '2026-11-23 10:00:00',
      revisor: '', reviseDate: '', mockSize: selectedFile.value?.size || copiedSource?.mockSize || 0,
    }
    templates.value.push(newTemplate)
    activeId.value = newTemplate.id
    setNotice(`${formMode.value === 'copy' ? '複製' : '新增'}完成：${newTemplate.xlsNo}（DEV 假資料）。`, 'success')
  } else if (formMode.value === 'edit') {
    const current = templates.value.find((template) => template.id === activeId.value)
    if (!current) return setNotice('找不到要編輯的樣板。', 'error')
    current.xlsName = form.value.xlsName
    current.description = form.value.description
    if (selectedFile.value) {
      current.xlsFileName = selectedFile.value.name
      current.mockSize = selectedFile.value.size
    }
    current.revisor = 'DEV-USER'
    current.reviseDate = '2026-11-23 10:00:00'
    setNotice(`更新完成：${current.xlsNo}（DEV 假資料）。`, 'success')
  }
  formMode.value = 'idle'
  selectedFile.value = null
  copySourceId.value = null
}
function cancelForm(): void {
  formMode.value = 'idle'
  selectedFile.value = null
  copySourceId.value = null
  if (activeTemplate.value) activate(activeTemplate.value)
  else form.value = emptyForm()
  setNotice('已取消本次編輯。')
}
function requestDelete(): void {
  if (!effectiveDeleteIds.value.length) return setNotice('請先選取要刪除的樣板。', 'error')
  deleteDialogOpen.value = true
}
function confirmDelete(): void {
  const ids = new Set(effectiveDeleteIds.value)
  const count = ids.size
  templates.value = templates.value.filter((template) => !ids.has(template.id))
  if (activeId.value !== null && ids.has(activeId.value)) activeId.value = null
  selectedIds.value = []
  deleteDialogOpen.value = false
  if (activeId.value === null) form.value = emptyForm()
  setNotice(`已刪除 ${count} 筆 DEV 樣板；實際來源逐筆刪除可能部分成功。`, 'success')
}
function downloadTemplate(): void {
  if (!activeTemplate.value) return setNotice('請先選取要下載的樣板。', 'error')
  setNotice(`DEV 模擬下載 ${activeTemplate.value.xlsFileName}（${activeTemplate.value.mockSize.toLocaleString()} bytes）；未產生檔案。`, 'success')
}
</script>

<template>
  <section v-if="props.developmentPreview" class="xls-control-page" aria-label="Excel 匯出樣板">
    <header class="page-head"><div><p class="eyebrow">SPC / SC204</p><h1>Excel 匯出樣板</h1><p class="subtitle">管理 Control Chart、AutoRPT 與 COA 匯出樣板；檔案操作只模擬 metadata。</p></div><button class="button" @click="emit('close')">Close (X)</button></header>
    <div class="category-tabs" role="tablist" aria-label="樣板類別"><button v-for="type in templateTypes" :key="type" role="tab" :aria-selected="activeType === type" :class="{ active: activeType === type }" @click="setType(type)">{{ typeLabels[type] }}</button><span>DEV 假資料 · 不讀取／上傳二進位檔</span></div>

    <div class="workspace">
      <section class="list-panel panel">
        <header class="panel-head"><div><h2>{{ typeLabels[activeType] }} 樣板</h2><span>{{ visibleTemplates.length }} 筆</span></div><div class="toolbar"><button class="button primary" :disabled="isEditing" @click="startAdd">Add</button><button class="button" :disabled="isEditing || !activeTemplate" @click="startEdit">Edit</button><button class="button" :disabled="isEditing || !activeTemplate" @click="startCopy">Copy</button><button class="button danger" :disabled="isEditing || !effectiveDeleteIds.length" @click="requestDelete">Delete</button><button class="button" :disabled="isEditing || !activeTemplate" @click="downloadTemplate">Download</button></div></header>
        <div class="table-scroll"><table><thead><tr><th></th><th>XlsNo</th><th>XlsName</th><th>XlsFileName</th><th>Description</th><th>Creator / CreateDate</th><th>Revisor / ReviseDate</th></tr></thead><tbody>
          <tr v-for="template in visibleTemplates" :key="template.id" :class="{ active: activeId === template.id }" @click="onSelectRow(template)"><td><input type="checkbox" :checked="selectedIds.includes(template.id)" :disabled="isEditing" :aria-label="`選取 ${template.xlsNo} 供刪除`" @click.stop @change="toggleSelected(template.id, $event)"></td><td>{{ template.xlsNo }}</td><td>{{ template.xlsName }}</td><td>{{ template.xlsFileName }}</td><td>{{ template.description || '—' }}</td><td>{{ template.creator }}<small>{{ template.createDate }}</small></td><td>{{ template.revisor || '—' }}<small>{{ template.reviseDate || '—' }}</small></td></tr>
          <tr v-if="!visibleTemplates.length"><td colspan="7" class="empty">此類別目前沒有 DEV 樣板</td></tr>
        </tbody></table></div>
        <footer class="grid-foot">刪除依 XlsNo + XlsType；樣板二進位內容不會在 DEV 預覽中下載。</footer>
      </section>

      <section class="edit-panel panel" :class="{ locked: !isEditing }">
        <header class="panel-head"><div><h2>{{ formMode === 'add' ? '新增樣板' : formMode === 'edit' ? '編輯樣板' : formMode === 'copy' ? '複製樣板' : '樣板資訊' }}</h2><span>{{ formMode === 'idle' ? '選取列表項目查看' : typeLabels[activeType] }}</span></div><span v-if="formMode !== 'idle'" class="edit-pill">{{ formMode.toUpperCase() }}</span></header>
        <div class="form-grid">
          <label>XlsNo<input v-model="form.xlsNo" maxlength="25" :readonly="formMode === 'edit'" :disabled="!isEditing" @blur="form.xlsNo = form.xlsNo.trim().toUpperCase()"></label>
          <label>XlsName<input v-model="form.xlsName" maxlength="25" :disabled="!isEditing"></label>
          <label class="wide">Excel file<input type="file" accept=".xls,.xlsx,.xlsm" :disabled="!isEditing" @change="onFileSelected"><small>{{ selectedFile ? `${selectedFile.name} · ${selectedFile.size.toLocaleString()} bytes（僅記錄資訊）` : form.xlsFileName || '接受 .xls、.xlsx、.xlsm' }}</small></label>
          <label class="wide">Description<textarea v-model="form.description" rows="3" :disabled="!isEditing"></textarea></label>
          <label>Creator<input :value="form.creator" readonly disabled></label><label>CreateDate<input :value="form.createDate" readonly disabled></label>
          <label>Revisor<input :value="form.revisor" readonly disabled></label><label>ReviseDate<input :value="form.reviseDate" readonly disabled></label>
        </div>
        <footer v-if="isEditing" class="form-actions"><span>DEV 不讀取本機檔案內容，也不會上傳 bytes。</span><button class="button" @click="cancelForm">Cancel</button><button class="button primary" @click="confirmForm">Confirm</button></footer>
        <p v-else class="readonly-note">SC204 的 DEV 表單可模擬 CRUD；正式環境尚未接 API。</p>
      </section>
    </div>
    <p class="notice" :class="noticeKind" role="status">{{ notice }}</p>

    <div v-if="deleteDialogOpen" class="modal-backdrop" @click.self="deleteDialogOpen = false"><section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="確認刪除樣板"><h2>確認刪除</h2><p>要刪除選取的 {{ effectiveDeleteIds.length }} 筆 DEV 樣板嗎？來源逐筆刪除可能部分成功。</p><div><button class="button" @click="deleteDialogOpen = false">Cancel</button><button class="button danger" @click="confirmDelete">Delete</button></div></section></div>
  </section>
  <section v-else class="pending">DEV 預覽已關閉。<button class="button" @click="emit('close')">Close</button></section>
</template>

<style scoped>
.xls-control-page{height:100%;min-height:0;overflow:auto;box-sizing:border-box;padding:14px;background:#f4f3ef;color:#292821;font:13px/1.45 "Segoe UI",Arial,sans-serif}.page-head{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:10px}.eyebrow{margin:0;color:#9a6b00;font-size:11px;font-weight:700;letter-spacing:.08em}.page-head h1{margin:2px 0;font-size:22px}.subtitle{margin:0;color:#777366}.button{min-height:29px;padding:4px 10px;border:1px solid #aaa79a;border-radius:3px;background:#fff;color:#292821;font:inherit;cursor:pointer}.button:hover:not(:disabled){background:#f9edbd;border-color:#c8a300}.button:disabled{opacity:.46;cursor:not-allowed}.button.primary{background:#e5c400;border-color:#c6a800;font-weight:700}.button.danger{border-color:#c7978f;color:#8b3027}.category-tabs{display:flex;align-items:center;gap:6px;margin-bottom:10px}.category-tabs button{padding:7px 12px;border:1px solid #d4d1c7;border-radius:3px;background:#fff;color:#555246;font:inherit;cursor:pointer}.category-tabs button.active{background:#fff0ad;border-color:#c6a800;color:#292821;font-weight:700}.category-tabs span{margin-left:auto;color:#777366;font-size:10px}.workspace{display:grid;grid-template-columns:minmax(0,1.3fr) minmax(310px,.8fr);gap:10px}.panel{min-width:0;border:1px solid #d4d1c7;border-radius:4px;background:#fff;overflow:hidden}.panel-head{display:flex;justify-content:space-between;align-items:center;gap:9px;padding:8px 10px;border-bottom:1px solid #e4e1d8;background:#faf9f6}.panel-head h2{margin:0;font-size:14px}.panel-head span{color:#777366;font-size:10px}.panel-head>div>span{display:block;margin-top:2px}.toolbar{display:flex;gap:4px;flex-wrap:wrap;justify-content:flex-end}.toolbar .button{min-height:27px;padding:3px 7px;font-size:11px}.table-scroll{max-height:420px;overflow:auto}.table-scroll table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:11px}.table-scroll th,.table-scroll td{padding:7px 8px;border-bottom:1px solid #eceae4;text-align:left;vertical-align:top}.table-scroll th{position:sticky;top:0;z-index:1;background:#f3f1ea;color:#5e5b50;font-weight:600}.table-scroll tbody tr{cursor:pointer}.table-scroll tbody tr:hover,.table-scroll tbody tr.active{background:#fff4c2}.table-scroll td small{display:block;margin-top:2px;color:#858174;font-size:10px}.empty{text-align:center!important;color:#888!important;padding:18px!important}.grid-foot{padding:7px 10px;border-top:1px solid #eceae4;color:#777366;font-size:10px}.edit-panel.locked{background:#fcfbf8}.edit-pill{padding:3px 7px;border:1px solid #e4d08a;border-radius:10px;background:#fff6d1;color:#705d13;font-size:9px!important;font-weight:700}.form-grid{display:grid;grid-template-columns:1fr 1fr;gap:9px;padding:10px}.form-grid label{display:flex;flex-direction:column;gap:4px;color:#625f55;font-size:10px}.form-grid label.wide{grid-column:1/-1}.form-grid input,.form-grid textarea{width:100%;box-sizing:border-box;border:1px solid #c8c5bb;border-radius:3px;padding:6px 7px;background:#fff;color:#292821;font:12px/1.35 inherit}.form-grid input:disabled,.form-grid textarea:disabled,.form-grid input[readonly]{background:#f1f0eb;color:#777366}.form-grid input[type=file]{font:11px inherit}.form-grid small{color:#858174;font-size:9px;overflow-wrap:anywhere}.form-actions{display:flex;align-items:center;gap:5px;padding:8px 10px;border-top:1px solid #e9e6dc}.form-actions span{flex:1;color:#777366;font-size:9px}.readonly-note{margin:0;padding:8px 10px;border-top:1px solid #e9e6dc;color:#777366;font-size:10px}.notice{position:sticky;bottom:0;margin:10px 0 0;padding:8px 10px;border:1px solid #e3d9a8;border-radius:3px;background:#fff8d8;color:#63551b}.notice.success{border-color:#acd0ae;background:#eff8ef;color:#346238}.notice.error{border-color:#e0b2aa;background:#fff0ed;color:#922f25}.modal-backdrop{position:fixed;inset:0;z-index:1000;display:grid;place-items:center;padding:15px;background:#20201866}.confirm-dialog{width:min(420px,94vw);padding:16px;border:1px solid #c5c1b4;border-radius:5px;background:#fff;box-shadow:0 16px 48px #0003}.confirm-dialog h2{margin:0 0 8px;font-size:17px}.confirm-dialog p{margin:0 0 15px;color:#625f55}.confirm-dialog>div{display:flex;justify-content:flex-end;gap:6px}.pending{padding:18px;color:#777366}.pending .button{margin-left:10px}
.form-grid input,.form-grid textarea{font:12px/1.35 "Segoe UI",Arial,sans-serif}.form-grid input[type=file]{font:11px "Segoe UI",Arial,sans-serif}
@media(max-width:1050px){.workspace{grid-template-columns:1fr}.category-tabs{flex-wrap:wrap}.category-tabs span{width:100%;margin-left:0}.table-scroll{max-height:320px}}
</style>
