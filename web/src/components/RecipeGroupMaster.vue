<script setup lang="ts">
import { computed, ref } from 'vue'
import { loadDevelopmentRecipeGroups, recipeGroupStorageKey, type RecipeDetail, type RecipeGroup, type RecipeState } from './recipeGroupDevelopment'

const props = defineProps<{ currentUserNo: string; developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const rows = ref(props.developmentPreview ? loadDevelopmentRecipeGroups() : [])
const states = [{ key: 0, label: '未核准' }, { key: 1, label: '簽核中' }, { key: 2, label: '已核准' }, { key: -1, label: '不使用' }] as const
const state = ref<RecipeState>(0)
const keyword = ref('')
const query = ref('')
const activeKey = ref('')
const marked = ref<string[]>([])
const detailKey = ref('')
const groupKey = (row: Pick<RecipeGroup, 'recipeGroup' | 'recipeVersion'>) => JSON.stringify([row.recipeGroup, row.recipeVersion])
const itemKey = (row: Pick<RecipeDetail, 'opNo' | 'recipeNo'>) => JSON.stringify([row.opNo, row.recipeNo])
const visible = computed(() => rows.value.filter(row => row.issueState === state.value && row.recipeGroup.includes(query.value)))
const active = computed(() => visible.value.find(row => groupKey(row) === activeKey.value) ?? visible.value[0])
const details = computed(() => (active.value?.details ?? []).slice().sort((a, b) => a.opNo.localeCompare(b.opNo) || a.sequence - b.sequence || a.recipeNo.localeCompare(b.recipeNo)))
const selectedDetail = computed(() => details.value.find(row => itemKey(row) === detailKey.value))
const canEdit = computed(() => props.developmentPreview && active.value?.issueState === 0)
const groupMode = ref<'add' | 'edit' | 'copy' | 'version' | ''>('')
const detailMode = ref<'add' | 'edit' | ''>('')
const groupDraft = ref({ recipeGroup: '', recipeVersion: 1, description: '' })
const detailDraft = ref({ opNo: '', recipeNo: '', recipeValue: '', sequence: 0, description: '' })
const groupSource = ref<RecipeGroup>()
const detailSource = ref<RecipeDetail>()
const error = ref('')
const notice = ref('')
const opCatalog = ['DEV-OP-10', 'DEV-OP-20', 'DEV-OP-30']
const recipeCatalog = ['DEV-RECIPE-001', 'DEV-RECIPE-002', 'DEV-RECIPE-003']

function save(next: RecipeGroup[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(recipeGroupStorageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 設定。'; return false }
  rows.value = next
  error.value = ''
  return true
}

function created() { return { creator: props.currentUserNo || 'DEV001', createDate: new Date().toISOString(), dataStamp: 0 } }
function revised(row: { dataStamp: number }) { return { reviser: props.currentUserNo || 'DEV001', reviseDate: new Date().toISOString(), dataStamp: row.dataStamp + 1 } }

function openGroup(mode: typeof groupMode.value): void {
  const source = mode === 'add' ? undefined : active.value
  groupSource.value = source
  groupMode.value = mode
  groupDraft.value = { recipeGroup: source?.recipeGroup ?? '', recipeVersion: mode === 'version' ? (source?.recipeVersion ?? 0) + 1 : mode === 'edit' ? source?.recipeVersion ?? 1 : 1, description: source?.description ?? '' }
  error.value = ''
}

function saveGroup(close: boolean): void {
  const source = groupSource.value
  const record = { ...groupDraft.value, recipeGroup: groupDraft.value.recipeGroup.trim().toUpperCase(), description: groupDraft.value.description.trim() }
  const mode = groupMode.value
  if (!record.recipeGroup || record.recipeVersion < 1 || record.recipeVersion > 99) { error.value = '請輸入群組及有效版次（1–99）。'; return }
  if (mode !== 'edit' && rows.value.some(row => row.recipeGroup === record.recipeGroup && (mode !== 'version' || row.recipeVersion === record.recipeVersion))) { error.value = '群組或版次已存在。'; return }
  if (mode === 'edit' && (!source || source.issueState !== 0 || groupKey(record) !== groupKey(source))) return
  const newRow: RecipeGroup = mode === 'edit' && source ? { ...source, ...record, ...revised(source) } : {
    ...record, ...created(), issueState: 0, curVersion: 0,
    details: (source?.details ?? []).map(row => ({ opNo: row.opNo, recipeNo: row.recipeNo, recipeValue: row.recipeValue, sequence: row.sequence, description: row.description, ...created() })),
  }
  if (!save(mode === 'edit' ? rows.value.map(row => groupKey(row) === groupKey(newRow) ? newRow : row) : [...rows.value, newRow])) return
  state.value = 0; query.value = ''; keyword.value = ''; marked.value = []; detailKey.value = ''; activeKey.value = groupKey(newRow)
  groupMode.value = close ? '' : 'edit'; groupSource.value = newRow; groupDraft.value = record
  notice.value = '參數群組已儲存。'
}

function changeState(action: 'approve' | 'delete'): void {
  const current = active.value
  if (!current || state.value === 1 || (action === 'approve' && !canEdit.value)) return
  const checked = new Set([...marked.value, groupKey(current)])
  const selected = visible.value.filter(row => checked.has(groupKey(row)))
  const groups = new Set(selected.map(row => row.recipeGroup))
  if (action === 'approve' && groups.size !== selected.length) { error.value = '同一群組請逐版核准。'; return }
  if (!window.confirm(action === 'approve' ? '確定核准選取的群組？' : '確定刪除或停用選取的群組？')) return
  const targets = new Set(selected.map(groupKey))
  const next = rows.value.flatMap<RecipeGroup>(row => {
    if (!targets.has(groupKey(row))) return [action === 'approve' && groups.has(row.recipeGroup) ? { ...row, curVersion: 0 } : row]
    if (action === 'delete' && row.issueState !== 2) return []
    return [{ ...row, ...revised(row), issueState: action === 'approve' ? 2 : -1, curVersion: action === 'approve' ? 1 : 0 }]
  })
  if (save(next)) { marked.value = []; detailKey.value = ''; notice.value = action === 'approve' ? '群組已核准。' : '群組已刪除或停用。' }
}

function openDetail(mode: 'add' | 'edit'): void {
  if (!canEdit.value) return
  const source = mode === 'edit' ? selectedDetail.value : undefined
  detailSource.value = source
  detailMode.value = mode
  detailDraft.value = { opNo: source?.opNo ?? '', recipeNo: source?.recipeNo ?? '', recipeValue: source?.recipeValue ?? '', sequence: source?.sequence ?? 0, description: source?.description ?? '' }
  error.value = ''
}

function saveDetails(next: RecipeDetail[]): boolean {
  const group = active.value
  if (!group || !canEdit.value) return false
  return save(rows.value.map(row => groupKey(row) === groupKey(group) ? { ...row, details: next } : row))
}

function saveDetail(close: boolean): void {
  const record = { ...detailDraft.value, recipeValue: detailDraft.value.recipeValue.trim(), description: detailDraft.value.description.trim() }
  const source = detailSource.value
  if (!record.opNo || !record.recipeNo || !record.recipeValue || !Number.isInteger(record.sequence) || record.sequence < 0 || record.sequence > 99) { error.value = '請選取作業與參數、輸入參數值及 0–99 的順序。'; return }
  if (detailMode.value === 'add' && details.value.some(row => itemKey(row) === itemKey(record))) { error.value = '同一作業的參數已存在。'; return }
  const saved: RecipeDetail = source ? { ...source, ...record, ...revised(source) } : { ...record, ...created() }
  if (!saveDetails(source ? details.value.map(row => itemKey(row) === itemKey(source) ? saved : row) : [...details.value, saved])) return
  detailMode.value = close ? '' : 'edit'; detailSource.value = saved; detailKey.value = itemKey(saved); notice.value = '參數明細已儲存。'
}

function deleteDetail(): void {
  if (!selectedDetail.value || !window.confirm('確定刪除選取的參數？')) return
  if (saveDetails(details.value.filter(row => itemKey(row) !== detailKey.value))) { detailKey.value = ''; notice.value = '參數明細已刪除。' }
}

const format = (value?: string) => value?.replace('T', ' ').slice(0, 19) ?? ''
</script>

<template>
  <main class="recipe-master">
    <header><h1>產品參數群組</h1><button @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">參數群組等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase(); marked = []; detailKey = ''">
        <label v-for="item in states" :key="item.key"><input v-model="state" type="radio" :value="item.key" @change="marked = []; detailKey = ''" />{{ item.label }}</label>
        <input v-model="keyword" type="search" aria-label="群組編號" placeholder="群組編號" /><button>查詢</button>
      </form>
      <div class="toolbar"><button @click="openGroup('add')">ADD</button><button :disabled="!canEdit" @click="openGroup('edit')">EDIT</button><button :disabled="!active || state === 1" @click="changeState('delete')">DEL</button><button :disabled="!active" @click="openGroup('copy')">Copy</button><button :disabled="!canEdit" @click="changeState('approve')">Approve</button><button :disabled="active?.issueState !== 2 || active.curVersion !== 1" @click="openGroup('version')">Version</button></div>
      <div class="grid"><table><thead><tr><th>選取</th><th>RecipeGroup</th><th>RecipeVersion</th><th>CurVersion</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
        <tr v-for="row in visible" :key="groupKey(row)" :class="{ selected: active && groupKey(row) === groupKey(active) }" @click="activeKey = groupKey(row); detailKey = ''"><td><input v-model="marked" type="checkbox" :value="groupKey(row)" :aria-label="`選取 ${row.recipeGroup} ${row.recipeVersion}`" /></td><td><button @click="activeKey = groupKey(row); detailKey = ''">{{ row.recipeGroup }}</button></td><td>{{ row.recipeVersion }}</td><td>{{ row.curVersion ? 'Yes' : 'No' }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ format(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ format(row.reviseDate) }}</td></tr>
        <tr v-if="!visible.length"><td colspan="9">沒有符合的群組。</td></tr></tbody></table></div>
      <section v-if="active"><header><h2>Recipe Detail</h2><div><button :disabled="!canEdit" @click="openDetail('add')">ADD</button> <button :disabled="!canEdit || !selectedDetail" @click="openDetail('edit')">EDIT</button> <button :disabled="!canEdit || !selectedDetail" @click="deleteDetail">DEL</button></div></header>
        <div class="grid"><table><thead><tr><th>OPNo</th><th>RecipeNo</th><th>RecipeValue</th><th>Sequence</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead><tbody>
          <tr v-for="row in details" :key="itemKey(row)" :class="{ selected: detailKey === itemKey(row) }" @click="detailKey = itemKey(row)"><td><button @click="detailKey = itemKey(row)">{{ row.opNo }}</button></td><td>{{ row.recipeNo }}</td><td>{{ row.recipeValue }}</td><td>{{ row.sequence }}</td><td>{{ row.description }}</td><td>{{ row.creator }}</td><td>{{ format(row.createDate) }}</td><td>{{ row.reviser }}</td><td>{{ format(row.reviseDate) }}</td></tr>
          <tr v-if="!details.length"><td colspan="9">沒有參數明細。</td></tr></tbody></table></div></section>
      <p v-if="error && !groupMode && !detailMode" role="alert" class="error">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="groupMode" class="overlay"><form class="editor" role="dialog" aria-modal="true" aria-label="參數群組設定" @submit.prevent="saveGroup(true)"><h2>Recipe Basis {{ groupMode.toUpperCase() }}</h2>
      <label>RecipeGroup<input v-model="groupDraft.recipeGroup" :readonly="groupMode === 'edit' || groupMode === 'version'" maxlength="50" required /></label>
      <label>Version<input :value="groupDraft.recipeVersion" readonly /></label><label>Description<textarea v-model="groupDraft.description" maxlength="255" /></label>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="saveGroup(false)">Apply</button><button type="button" @click="groupMode = ''; error = ''">Close</button></footer>
    </form></div>
    <div v-if="detailMode" class="overlay"><form class="editor" role="dialog" aria-modal="true" aria-label="參數明細設定" @submit.prevent="saveDetail(true)"><h2>Recipe Detail {{ detailMode.toUpperCase() }}</h2>
      <p>{{ active?.recipeGroup }} / {{ active?.recipeVersion }}</p>
      <label>OPNo<select v-model="detailDraft.opNo" :disabled="detailMode === 'edit'" required><option value="">請選擇</option><option v-for="op in opCatalog" :key="op">{{ op }}</option></select></label>
      <label>RecipeNo<select v-model="detailDraft.recipeNo" :disabled="detailMode === 'edit'" required><option value="">請選擇</option><option v-for="recipe in recipeCatalog" :key="recipe">{{ recipe }}</option></select></label>
      <label>RecipeValue<input v-model="detailDraft.recipeValue" maxlength="255" required /></label><label>Sequence<input v-model.number="detailDraft.sequence" type="number" min="0" max="99" step="1" required /></label><label>Description<textarea v-model="detailDraft.description" maxlength="255" /></label>
      <p v-if="error" role="alert" class="error">{{ error }}</p><footer><button>Confirm</button><button type="button" @click="saveDetail(false)">Apply</button><button type="button" @click="detailMode = ''; error = ''">Close</button></footer>
    </form></div>
  </main>
</template>

<style scoped>
.recipe-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}header,.toolbar,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.selected{background:#fff2c6}button,input,select,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:12px;width:min(540px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.editor label{display:grid;gap:4px}.editor input,.editor select,.editor textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.editor input:read-only{background:#f4f3ec}footer{justify-content:flex-end}
</style>
