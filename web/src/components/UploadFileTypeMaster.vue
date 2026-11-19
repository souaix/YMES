<script setup lang="ts">
import { computed, ref } from 'vue'
import { loadDevelopmentUploadFileTypes, uploadFileOptions, uploadFileTypeKey, type UploadFileType } from './uploadFileDevelopment'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const rows = ref(props.developmentPreview ? loadDevelopmentUploadFileTypes() : [])
const selected = ref<number>()
const query = ref('')
const state = ref('all')
const mode = ref<'' | 'add' | 'edit' | 'copy' | 'view'>('')
const draft = ref<UploadFileType>(blank())
const checked = ref('')
const error = ref('')
const notice = ref('')
const states = { '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>
const labels = new Map(uploadFileOptions.map(option => [option.type, option.label]))
const visible = computed(() => rows.value.filter(row =>
  (state.value === 'all' || row.state.toString() === state.value)
  && [String(row.type), labels.get(row.type) || '', row.description].some(value => value.includes(query.value.trim())),
))
const active = computed(() => visible.value.find(row => row.type === selected.value) ?? visible.value[0])
const editable = computed(() => mode.value !== 'view')

function blank(): UploadFileType {
  return { type: 50, host: 'ftp.example.invalid', account: 'DEV-FILE', port: 21, path: '/MesFileManagement', description: '', state: 0, stamp: 0 }
}

function show(next: Exclude<typeof mode.value, ''>): void {
  const row = active.value
  if (next === 'edit' && row?.state !== 0) return
  if (next !== 'add' && !row) return
  draft.value = next === 'add' || !row ? blank() : { ...row }
  if (next === 'add' || next === 'copy') {
    const available = uploadFileOptions.find(option => !rows.value.some(item => item.type === option.type))
    if (available) draft.value.type = available.type
  }
  mode.value = next
  error.value = checked.value = ''
}

function persist(next: UploadFileType[]): boolean {
  if (!props.developmentPreview) return false
  try { localStorage.setItem(uploadFileTypeKey, JSON.stringify(next)) }
  catch { error.value = '無法儲存本機 DEV 上傳設定。'; return false }
  rows.value = next
  error.value = ''
  return true
}

function validate(): boolean {
  const { type, host, account, port, path } = draft.value
  if (!labels.has(type) || !/^([a-z0-9-]+\.)+invalid$/i.test(host.trim()) || !account.trim() || !path.trim() || !Number.isInteger(port) || port < 1 || port > 65535) {
    error.value = '請選類型，填寫 .invalid 假主機、DEV 帳號、有效埠號及路徑。'
    checked.value = ''
    return false
  }
  return true
}

function previewConnection(): void {
  if (!validate()) return
  error.value = ''
  checked.value = 'DEV 欄位檢查通過。'
}

function save(close: boolean): void {
  if (!editable.value || !validate()) return
  const existing = rows.value.find(row => row.type === draft.value.type)
  if (mode.value !== 'edit' && existing) { error.value = '檔案類型已有設定。'; return }
  if (mode.value === 'edit' && existing?.state !== 0) return
  const saved = { ...draft.value, host: draft.value.host.trim(), account: draft.value.account.trim(), path: draft.value.path.trim(), state: 0, stamp: existing ? existing.stamp + 1 : 0 }
  if (persist(existing ? rows.value.map(row => row.type === saved.type ? saved : row) : [...rows.value, saved])) {
    draft.value = { ...saved }
    state.value = 'all'
    query.value = ''
    selected.value = saved.type
    mode.value = close ? '' : 'edit'
    notice.value = '已儲存 DEV 上傳設定。'
  }
}

function approve(): void {
  const row = active.value
  if (!row || row.state !== 0 || !window.confirm('確定核准上傳設定？')) return
  if (persist(rows.value.map(item => item.type === row.type ? { ...item, state: 2, stamp: item.stamp + 1 } : item))) notice.value = '已核准上傳設定。'
}

function remove(): void {
  const row = active.value
  if (!row || ![-1, 0, 2].includes(row.state) || !window.confirm('確定刪除上傳設定？')) return
  if (persist(rows.value.filter(item => item.type !== row.type))) notice.value = '已刪除上傳設定。'
}
</script>

<template>
  <main class="file-types">
    <header><h1>檔案上傳基本資料</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>檔案類型或說明 <input v-model="query" /></label>
      <label>狀態 <select v-model="state"><option value="all">全部</option><option v-for="(label, value) in states" :key="value" :value="value">{{ label }}</option></select></label>
      <button :disabled="!developmentPreview" @click="show('add')">新增</button><button :disabled="!active" @click="show('view')">檢視</button>
      <button :disabled="active?.state !== 0" @click="show('edit')">修改</button><button :disabled="!active" @click="show('copy')">複製</button>
      <button :disabled="active?.state !== 0" @click="approve">核准</button><button :disabled="!active || ![-1, 0, 2].includes(active.state)" @click="remove">刪除</button>
    </div>
    <p v-if="notice" role="status">{{ notice }}</p><p v-if="error && !mode" role="alert">{{ error }}</p>
    <div class="grid"><table>
      <thead><tr><th>檔案類型</th><th>服務</th><th>主機</th><th>埠</th><th>路徑</th><th>說明</th><th>狀態</th></tr></thead>
      <tbody>
        <tr v-for="row in visible" :key="row.type" :class="{ selected: row === active }"><td><button :aria-pressed="row === active" @click="selected = row.type">{{ row.type }} {{ labels.get(row.type) }}</button></td><td>FTP</td><td>{{ row.host }}</td><td>{{ row.port }}</td><td>{{ row.path }}</td><td>{{ row.description }}</td><td>{{ states[row.state] }}</td></tr>
        <tr v-if="!visible.length"><td colspan="7">無檔案上傳設定</td></tr>
      </tbody>
    </table></div>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-label="檔案上傳設定" @submit.prevent="save(true)" @input="checked = ''">
        <header><h2>檔案上傳設定</h2><button type="button" @click="mode = ''">關閉</button></header>
        <label>檔案類型 <select v-model.number="draft.type" :disabled="!editable || mode === 'edit'"><option v-for="option in uploadFileOptions" :key="option.type" :value="option.type">{{ option.label }}</option></select></label>
        <label>假主機 <input v-model="draft.host" maxlength="255" :readonly="!editable" required /></label>
        <label>DEV 帳號 <input v-model="draft.account" maxlength="50" :readonly="!editable" required /></label>
        <label>埠號 <input v-model.number="draft.port" type="number" min="1" max="65535" :readonly="!editable" required /></label>
        <label>路徑 <input v-model="draft.path" maxlength="255" :readonly="!editable" required /></label>
        <label>說明 <input v-model="draft.description" maxlength="255" :readonly="!editable" /></label>
        <p v-if="error" role="alert">{{ error }}</p><p v-if="checked" role="status">{{ checked }}</p>
        <footer><button type="button" @click="previewConnection">檢查 DEV 設定</button><template v-if="editable"><button>確定</button><button type="button" @click="save(false)">套用</button></template></footer>
      </form>
    </div>
  </main>
</template>

<style scoped>
.file-types { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
header, .toolbar, footer { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
header { justify-content: space-between; }
h1, h2, p { margin: 0; }
h1 { font-size: 17px; }
.toolbar, thead { background: #f5f3eb; }
.toolbar { padding: 6px; border: 1px solid #dedbd0; }
button, input, select { font: inherit; }
button { padding: 4px 10px; border: 1px solid #aaa797; border-radius: 3px; background: #f8f7f2; color: inherit; }
button:disabled { opacity: .5; }
.grid { overflow: auto; border: 1px solid #d9d6cb; }
table { width: 100%; border-collapse: collapse; text-align: left; white-space: nowrap; }
th, td { padding: 6px; border: 1px solid #e3e0d6; }
.selected { background: #fff2c6; }
.overlay { position: fixed; inset: 0; z-index: 91; display: grid; place-items: center; padding: 12px; background: #0005; }
.editor { display: grid; gap: 10px; width: min(550px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor label { display: grid; grid-template-columns: 90px 1fr; align-items: center; gap: 8px; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
