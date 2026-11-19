<script setup lang="ts">
import { computed, ref, shallowRef } from 'vue'
import { developmentCpProgramTypes, loadDevelopmentPrograms, type TestProgram } from './testProgramDevelopment'
import { loadDevelopmentUploadFileTypes, uploadFileOptions } from './uploadFileDevelopment'

type ProgramFileType = 50 | 51
type FileBatch = { id: string; type: ProgramFileType; program: string; version: string; path: string; date: string; user: string; memo: string; names: string[] }
type Snapshot = { current: FileBatch[]; history: FileBatch[] }
const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.pdb05-program-files.v1'
const types = uploadFileOptions.filter(option => [50, 51].includes(option.type))
const query = ref({ type: 50 as ProgramFileType, program: '', version: '', programType: '', current: false, history: false })
const filters = ref({ ...query.value })
const programs = ref<TestProgram[]>(props.developmentPreview ? loadDevelopmentPrograms(true) : [])
const registeredTypes = ref(props.developmentPreview ? loadDevelopmentUploadFileTypes() : [])
const snapshot = ref(load())
const selected = ref('')
const editor = ref(false)
const editingFiles = shallowRef<File[]>([])
const liveFiles = new Map<string, File[]>()
const memo = ref('')
const historyId = ref('')
const error = ref('')
const notice = ref('')
const fileType = computed(() => registeredTypes.value.find(setting => setting.type === filters.value.type && setting.state === 2))
const visible = computed(() => programs.value.filter(row => (!filters.value.program || row.no.includes(filters.value.program))
  && (!filters.value.version || row.version === filters.value.version)
  && (!filters.value.programType || row.programType === filters.value.programType) && (!filters.value.current || row.current)))
const active = computed(() => visible.value.find(row => programKey(row) === selected.value) ?? visible.value[0])
const versions = computed(() => {
  const row = active.value
  if (!row) return []
  const scope = filters.value.history ? snapshot.value.history : snapshot.value.current
  return scope.filter(item => item.type === filters.value.type && item.program === row.no && item.version === row.version)
})
const activeBatch = computed(() => versions.value.find(batch => batch.id === historyId.value) ?? versions.value[0])
const displayFiles = computed(() => activeBatch.value?.names ?? (filters.value.history ? [] : active.value?.files ?? []))
const states = { '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>

function load(): Snapshot {
  if (!props.developmentPreview) return { current: [], history: [] }
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (Array.isArray(saved?.current) && Array.isArray(saved?.history)) return saved
  } catch { /* Use an empty DEV history when storage is unavailable. */ }
  return { current: [], history: [] }
}

function programKey(row: TestProgram): string { return JSON.stringify([row.no, row.version]) }

function runQuery(): void {
  if (!props.developmentPreview) return
  filters.value = { ...query.value, program: query.value.program.trim().toUpperCase(), version: query.value.version.trim().toUpperCase(),
    programType: query.value.type === 50 ? query.value.programType : '' }
  programs.value = loadDevelopmentPrograms(filters.value.type === 50)
  registeredTypes.value = loadDevelopmentUploadFileTypes()
  selected.value = historyId.value = ''
  notice.value = error.value = ''
}

function check(): void {
  if (!active.value) return
  registeredTypes.value = loadDevelopmentUploadFileTypes()
  if (!fileType.value) { error.value = '找不到核准的 DEV 檔案類型設定。'; return }
  notice.value = `DEV 已找到 ${fileType.value.host}:${fileType.value.port} 的核准設定。`
  error.value = ''
}

function openUpload(): void {
  if (!active.value || !fileType.value) return
  editingFiles.value = []
  memo.value = ''
  error.value = notice.value = ''
  editor.value = true
}

function chooseFiles(event: Event): void {
  const input = event.target as HTMLInputElement
  const next = new Map(editingFiles.value.map(file => [file.name, file]))
  for (const file of Array.from(input.files ?? [])) next.set(file.name, file)
  editingFiles.value = [...next.values()]
  input.value = ''
}

function saveUpload(): void {
  const program = active.value
  const setting = fileType.value
  if (!program || !setting || !editingFiles.value.length) return
  if (editingFiles.value.length > 999) { error.value = '每批最多 999 個檔案。'; return }
  const date = new Date().toISOString()
  const id = crypto.randomUUID()
  const path = [setting.path.replace(/\/+$/, ''), program.no, program.version].join('/')
  const batch = { id, type: filters.value.type, program: program.no, version: program.version, path, date,
    user: props.currentUserNo || 'DEV001', memo: memo.value.trim(), names: editingFiles.value.map(file => file.name) }
  const current = snapshot.value.current.filter(item => item.type !== batch.type || item.program !== batch.program || item.version !== batch.version)
  const next = { current: [...current, batch], history: [batch, ...snapshot.value.history] }
  try { localStorage.setItem(storageKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 上傳紀錄。'; return }
  liveFiles.set(id, [...editingFiles.value])
  snapshot.value = next
  historyId.value = id
  editor.value = false
  error.value = ''
  notice.value = `已儲存 ${batch.names.length} 個檔案的 DEV 上傳紀錄。`
}

function available(name: string): boolean {
  return Boolean(activeBatch.value && liveFiles.get(activeBatch.value.id)?.some(file => file.name === name))
}

function download(name: string): void {
  const batch = activeBatch.value
  const file = batch && liveFiles.get(batch.id)?.find(item => item.name === name)
  if (!file) return
  const link = document.createElement('a')
  const url = URL.createObjectURL(file)
  link.href = url
  link.download = file.name
  link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
</script>

<template>
  <main class="program-files">
    <header><h1>測試程式檔案管理</h1><button @click="emit('close')">關閉</button></header>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="runQuery">
        <label>檔案類型 <select v-model.number="query.type"><option v-for="type in types" :key="type.type" :value="type.type">{{ type.label }}</option></select></label>
        <label>程式編號 <input v-model="query.program" /></label><label>版次 <input v-model="query.version" /></label>
        <label v-if="query.type === 50">程式類別 <select v-model="query.programType"><option value="">全部</option><option v-for="type in developmentCpProgramTypes" :key="type">{{ type }}</option></select></label>
        <label><input v-model="query.current" type="checkbox" />目前版本</label>
        <label><input v-model="query.history" type="checkbox" />檔案更新歷程</label>
        <button>查詢</button>
      </form>
      <div class="toolbar">
        <button :disabled="!active" @click="check">檢查 DEV 上傳設定</button>
        <button :disabled="!active || !fileType" @click="openUpload">登錄上傳</button>
      </div>
      <p v-if="notice" role="status">{{ notice }}</p><p v-if="error && !editor" role="alert">{{ error }}</p>
      <div class="grid"><table>
        <thead><tr><th>程式</th><th>版次</th><th>類別</th><th>狀態</th><th>目前版本</th><th>說明</th></tr></thead>
        <tbody>
          <tr v-for="row in visible" :key="programKey(row)" :class="{ selected: row === active }">
            <td><button :aria-pressed="row === active" @click="selected = programKey(row); historyId = ''; error = ''">{{ row.no }}</button></td>
            <td>{{ row.version }}</td><td>{{ row.programType }}</td><td>{{ states[row.state] }}</td><td>{{ row.current ? '是' : '' }}</td><td>{{ row.description }}</td>
          </tr>
          <tr v-if="!visible.length"><td colspan="6">無程式資料</td></tr>
        </tbody>
      </table></div>
      <section v-if="active" class="details">
        <h2>{{ active.no }} / {{ active.version }} 檔案明細</h2>
        <label v-if="filters.history && versions.length">更新歷程 <select v-model="historyId"><option value="">最新紀錄</option><option v-for="batch in versions" :key="batch.id" :value="batch.id">{{ batch.date }} {{ batch.user }}</option></select></label>
        <p v-if="activeBatch">{{ activeBatch.path }} · {{ activeBatch.date }} · {{ activeBatch.user }} {{ activeBatch.memo }}</p>
        <div class="grid"><table>
          <thead><tr><th>檔名</th><th>下載</th></tr></thead>
          <tbody>
            <tr v-for="name in displayFiles" :key="name"><td>{{ name }}</td><td><button :disabled="!available(name)" @click="download(name)">下載</button></td></tr>
            <tr v-if="!displayFiles.length"><td colspan="2">無檔案紀錄</td></tr>
          </tbody>
        </table></div>
        <p v-if="displayFiles.length && !displayFiles.some(available)">此處只有檔案紀錄。下載適用於本頁選取的檔案。</p>
      </section>
      <div v-if="editor" class="overlay">
        <form class="editor" role="dialog" aria-modal="true" aria-label="程式檔案登錄" @submit.prevent="saveUpload">
          <header><h2>程式檔案登錄</h2><button type="button" @click="editor = false; error = ''">關閉</button></header>
          <p>{{ active?.no }} / {{ active?.version }}</p>
          <label>DEV 檔案 <input type="file" multiple @change="chooseFiles" /></label>
          <ul v-if="editingFiles.length"><li v-for="file in editingFiles" :key="file.name">{{ file.name }} <button type="button" @click="editingFiles = editingFiles.filter(item => item.name !== file.name)">移除</button></li></ul>
          <label>備註 <input v-model="memo" maxlength="255" /></label>
          <p v-if="error" role="alert">{{ error }}</p><footer><button :disabled="!editingFiles.length">儲存 DEV 紀錄</button></footer>
        </form>
      </div>
    </template>
    <p v-else>測試程式檔案服務待接續。</p>
  </main>
</template>

<style scoped>
.program-files { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
h2 { font-size: 14px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.details { display: grid; gap: 6px; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 10px; width: min(550px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
