<script setup lang="ts">
import { computed, ref } from 'vue'

type SoftwareBin = { plan: string; software: string; hardware: string; fail: boolean }
type HardwareBin = { no: string; fail: boolean; description: string }
type Program = {
  no: string; version: string; description: string; manageSoftware: boolean; state: number; current: boolean
  hardwareBins: HardwareBin[]; softwareBins: SoftwareBin[]; files: string[]; creator: string; reviser?: string; stamp: number
}
type EditorMode = 'new' | 'edit' | 'copy' | 'version'
type Panel = 'program' | 'hardware' | 'software' | 'files' | ''

const props = defineProps<{ developmentPreview: boolean; currentUserNo: string }>()
const emit = defineEmits<{ close: [] }>()
const storageKey = 'ymes.dev.prd202-ft-programs.v1'
const rows = ref(load())
const query = ref('')
const stateFilter = ref('all')
const currentOnly = ref(false)
const selected = ref('')
const panel = ref<Panel>('')
const mode = ref<EditorMode>('new')
const program = ref(blank())
const copyBins = ref(true)
const message = ref('')
const error = ref('')
const visible = computed(() => rows.value.filter(row =>
  (stateFilter.value === 'all' || row.state.toString() === stateFilter.value)
  && (!currentOnly.value || row.current)
  && [row.no, row.version, row.description].some(value => value.toUpperCase().includes(query.value.trim().toUpperCase())),
))
const active = computed(() => visible.value.find(row => key(row) === selected.value) ?? visible.value[0])
const editable = computed(() => active.value?.state === 0)
const busy = ref(false)
const preparedBins = ref<SoftwareBin[]>([])
const panelTitle = computed(() => {
  if (panel.value === 'program') return { new: '新增測試程式', edit: '修改測試程式', copy: '複製測試程式', version: '新增程式版本' }[mode.value]
  return { hardware: '硬體 Bin', software: '軟體 Bin', files: '程式檔案', '': '' }[panel.value]
})
const user = computed(() => props.currentUserNo || 'DEV001')

function key(row: Program): string { return JSON.stringify([row.no, row.version]) }

function blank(): Program {
  return { no: '', version: '01', description: '', manageSoftware: false, state: 0, current: false,
    hardwareBins: [], softwareBins: [], files: [], creator: '', stamp: 0 }
}

function load(): Program[] {
  if (!props.developmentPreview) return []
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) ?? 'null')
    if (Array.isArray(stored)) return stored
  } catch { /* Use DEV fixtures when local storage is unavailable. */ }
  return [
    { ...blank(), no: 'DEV-FT-01', description: 'DEV 核准測試程式', state: 2, current: true, creator: 'DEV001' },
    { ...blank(), no: 'DEV-FT-02', description: 'DEV 待核准測試程式', manageSoftware: true, creator: 'DEV001' },
  ]
}

function clone(row: Program): Program {
  return { ...row, hardwareBins: row.hardwareBins.map(bin => ({ ...bin })), softwareBins: row.softwareBins.map(bin => ({ ...bin })), files: [...row.files] }
}

function persist(next: Program[]): boolean {
  if (!props.developmentPreview) return false
  try {
    localStorage.setItem(storageKey, JSON.stringify(next))
  } catch {
    error.value = '無法儲存本機 DEV 測試程式。'
    return false
  }
  rows.value = next
  error.value = ''
  return true
}

function openEditor(next: EditorMode): void {
  const row = active.value
  if (next !== 'new' && !row) return
  if (next === 'edit' && !editable.value) return
  if (next === 'version' && (row?.state !== 2 || !row.current)) return
  program.value = next === 'new' || !row ? blank() : clone(row)
  if (next === 'copy') program.value.no = ''
  if (next === 'version') program.value.version = ''
  error.value = ''
  copyBins.value = true
  mode.value = next
  panel.value = 'program'
}

function selectSaved(saved: Program): void {
  selected.value = key(saved)
  query.value = ''
  stateFilter.value = 'all'
  currentOnly.value = false
  panel.value = ''
}

function saveProgram(): void {
  const saved = clone(program.value)
  saved.no = saved.no.trim().toUpperCase()
  saved.version = saved.version.trim().toUpperCase()
  if (!saved.no || !saved.version) { error.value = '程式編號與版次必填。'; return }
  if (mode.value === 'edit') {
    if (editable.value && active.value && key(active.value) === key(saved)) saveDetails(saved)
    return
  }
  if (rows.value.some(row => key(row) === key(saved))) { error.value = '程式版次已存在。'; return }
  if (!copyBins.value) { saved.hardwareBins = []; saved.softwareBins = [] }
  Object.assign(saved, { state: 0, current: false, files: [], creator: user.value, reviser: undefined, stamp: 0 })
  if (persist([...rows.value, saved])) { selectSaved(saved); message.value = '已新增測試程式。' }
}

function saveDetails(saved: Program): void {
  if (!editable.value || !active.value || key(active.value) !== key(saved)) return
  const updated = { ...saved, reviser: user.value, stamp: active.value.stamp + 1 }
  if (persist(rows.value.map(row => key(row) === key(updated) ? updated : row))) {
    selectSaved(updated)
    message.value = '已儲存測試程式設定。'
  }
}

function approve(): void {
  const row = active.value
  if (!row || !editable.value) return
  if (row.manageSoftware && !row.softwareBins.some(bin => row.hardwareBins.some(hardware => hardware.no === bin.hardware))) {
    error.value = '啟用軟體 Bin 管理時，核准前須匯入和硬體 Bin 對應的資料。'
    return
  }
  if (!window.confirm(`確定核准 ${row.no} / ${row.version}？`)) return
  const approved = { ...row, state: 2, current: true, reviser: user.value, stamp: row.stamp + 1 }
  const next = rows.value.map(item => {
    if (key(item) === key(row)) return approved
    if (item.no === row.no && item.current) return { ...item, current: false, reviser: user.value, stamp: item.stamp + 1 }
    return item
  })
  if (persist(next)) { selectSaved(approved); message.value = '已核准並設為目前版本。' }
}

function remove(): void {
  const row = active.value
  if (!row || ![-1, 0, 2].includes(row.state) || !window.confirm(`確定刪除 ${row.no} / ${row.version}？`)) return
  if (persist(rows.value.filter(item => key(item) !== key(row)))) message.value = '已刪除測試程式及其 DEV 明細。'
}

function openPanel(next: 'hardware' | 'software' | 'files'): void {
  if (!active.value) return
  program.value = clone(active.value)
  preparedBins.value = []
  error.value = ''
  panel.value = next
}

async function readSoftwareBins(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !editable.value || !active.value?.manageSoftware) return
  busy.value = true
  preparedBins.value = []
  error.value = ''
  try {
    const lines = (await file.text()).replace(/^\uFEFF/, '').split(/\r?\n/)
    const good = lines[1]?.match(/^\s*Good\s*:(.*)$/i)?.[1]
    if (good === undefined || lines[2]?.trim().toUpperCase().split(/\s+/).join(' ') !== 'TESTPLAN_NAME SOFTWARE_BIN HARDWARE_BIN') {
      throw new Error('請檢查第二列 Good 設定與第三列欄名。')
    }
    const goodBins = new Set(good.split(',').map(value => value.trim().toUpperCase()).filter(Boolean))
    const plans = new Set<string>()
    const parsed: SoftwareBin[] = []
    for (const [index, line] of lines.slice(3).entries()) {
      if (!line.trim()) continue
      const [plan, software, hardware, extra] = line.trim().split(/\s+/)
      if (!plan || !software || !hardware || extra || plan.length > 50 || software.length > 7 || hardware.length > 7 || plans.has(plan.toUpperCase())) {
        throw new Error(`第 ${index + 4} 列格式錯誤或 TestPlan_Name 重複。`)
      }
      plans.add(plan.toUpperCase())
      parsed.push({ plan, software: 'BIN' + software.toUpperCase(), hardware: 'BIN' + hardware.toUpperCase(), fail: !goodBins.has(hardware.toUpperCase()) })
    }
    for (const goodBin of goodBins) {
      if (goodBin.length > 7) throw new Error('Good Bin 編號過長。')
      const no = 'BIN' + goodBin
      if (!parsed.some(bin => bin.hardware.toUpperCase() === no)) parsed.push({ plan: 'GoodBin', software: no, hardware: no, fail: false })
    }
    if (!parsed.length) throw new Error('工作檔沒有任何 Bin 資料。')
    preparedBins.value = parsed
  } catch (reason) {
    error.value = reason instanceof Error ? reason.message : '無法解析軟體 Bin 檔案。'
  } finally { busy.value = false }
}

function saveSoftwareBins(): void {
  if (!editable.value || !program.value.manageSoftware || !preparedBins.value.length) return
  const hardware = new Map<string, HardwareBin>()
  for (const bin of preparedBins.value) {
    const description = program.value.hardwareBins.find(item => item.no === bin.hardware)?.description ?? ''
    hardware.set(bin.hardware, { no: bin.hardware, fail: bin.fail, description })
  }
  saveDetails({ ...program.value, softwareBins: [...preparedBins.value], hardwareBins: [...hardware.values()] })
}

function saveHardwareBins(): void {
  const bins = program.value.hardwareBins.map(bin => ({ ...bin, no: bin.no.trim().toUpperCase() }))
  const keys = new Set(bins.map(bin => bin.no))
  if (keys.has('') || keys.size !== bins.length) { error.value = '硬體 Bin 編號必填且不可重複。'; return }
  if (program.value.softwareBins.some(bin => !keys.has(bin.hardware))) { error.value = '匯入的軟體 Bin 仍引用被移除的硬體 Bin。'; return }
  saveDetails({ ...program.value, hardwareBins: bins })
}

function selectFileNames(event: Event): void {
  if (!editable.value) return
  const input = event.target as HTMLInputElement
  program.value.files = [...new Set([...program.value.files, ...Array.from(input.files ?? [], file => file.name)])]
  input.value = ''
}
</script>

<template>
  <main class="ft-program">
    <header><h1>FT 測試程式</h1><button @click="emit('close')">關閉</button></header>
    <div class="toolbar">
      <label>編號、版次或說明 <input v-model="query" /></label>
      <label>狀態 <select v-model="stateFilter"><option value="all">全部</option><option value="0">未核准</option><option value="1">簽核中</option><option value="2">已核准</option><option value="-1">停用</option></select></label>
      <label><input v-model="currentOnly" type="checkbox" />目前版本</label>
    </div>
    <div class="toolbar">
      <button :disabled="!developmentPreview" @click="openEditor('new')">新增</button>
      <button :disabled="!editable" @click="openEditor('edit')">修改</button>
      <button :disabled="!active" @click="openEditor('copy')">複製</button>
      <button :disabled="active?.state !== 2 || !active?.current" @click="openEditor('version')">新增版本</button>
      <button :disabled="!editable" @click="approve">核准</button>
      <button :disabled="!active || ![-1, 0, 2].includes(active.state)" @click="remove">刪除</button>
      <button :disabled="!active" @click="openPanel('hardware')">硬體 Bin</button>
      <button :disabled="!active?.manageSoftware" @click="openPanel('software')">軟體 Bin</button>
      <button :disabled="!active" @click="openPanel('files')">程式檔案</button>
    </div>
    <p v-if="message" role="status">{{ message }}</p>
    <p v-if="error && !panel" role="alert">{{ error }}</p>
    <div class="grid">
      <table>
        <thead><tr><th scope="col">程式編號</th><th scope="col">版次</th><th scope="col">說明</th><th scope="col">狀態</th><th scope="col">目前版本</th><th scope="col">軟體 Bin 管理</th></tr></thead>
        <tbody><tr v-for="row in visible" :key="key(row)" :class="{ selected: active === row }">
          <td><button :aria-pressed="active === row" @click="selected = key(row); error = ''">{{ row.no }}</button></td>
          <td>{{ row.version }}</td><td>{{ row.description }}</td><td>{{ ({ '-1': '停用', 0: '未核准', 1: '簽核中', 2: '已核准' } as Record<string, string>)[row.state] ?? row.state }}</td><td>{{ row.current ? '是' : '' }}</td><td>{{ row.manageSoftware ? '是' : '否' }}</td>
        </tr><tr v-if="!visible.length"><td colspan="6">無測試程式</td></tr></tbody>
      </table>
    </div>
    <div v-if="panel" class="overlay">
      <section class="editor" role="dialog" aria-modal="true" :aria-label="panelTitle">
        <header><h2>{{ panelTitle }}</h2><button :disabled="busy" @click="panel = ''; error = ''">關閉</button></header>
        <form v-if="panel === 'program'" @submit.prevent="saveProgram">
          <label>程式編號 <input v-model="program.no" maxlength="50" required :readonly="mode === 'edit' || mode === 'version'" /></label>
          <label>版次 <input v-model="program.version" maxlength="3" required :readonly="mode === 'edit'" /></label>
          <label>說明 <input v-model="program.description" maxlength="255" /></label>
          <label><input v-model="program.manageSoftware" type="checkbox" />啟用軟體 Bin 管理</label>
          <label v-if="mode === 'copy' || mode === 'version'"><input v-model="copyBins" type="checkbox" />帶入硬體與軟體 Bin</label>
          <button>儲存</button>
        </form>
        <template v-else-if="panel === 'hardware'">
          <div v-for="(bin, index) in program.hardwareBins" :key="index" class="toolbar">
            <label>Bin <input v-model="bin.no" maxlength="10" :readonly="!editable" /></label>
            <label>結果 <select v-model="bin.fail" :disabled="!editable"><option :value="false">Good</option><option :value="true">Fail</option></select></label>
            <label>說明 <input v-model="bin.description" maxlength="255" :readonly="!editable" /></label>
            <button :disabled="!editable" @click="program.hardwareBins.splice(index, 1)">移除</button>
          </div>
          <footer><button :disabled="!editable" @click="program.hardwareBins.push({ no: '', fail: false, description: '' })">加入 Bin</button><button :disabled="!editable" @click="saveHardwareBins">儲存</button></footer>
        </template>
        <template v-else-if="panel === 'software'">
          <label>軟體 Bin 文字檔 <input type="file" accept=".txt" :disabled="!editable || busy" @change="readSoftwareBins" /></label>
          <table><thead><tr><th>TestPlan</th><th>Software Bin</th><th>Hardware Bin</th><th>結果</th></tr></thead>
            <tbody><tr v-for="(bin, index) in preparedBins.length ? preparedBins : program.softwareBins" :key="index"><td>{{ bin.plan }}</td><td>{{ bin.software }}</td><td>{{ bin.hardware }}</td><td>{{ bin.fail ? 'Fail' : 'Good' }}</td></tr></tbody>
          </table>
          <footer><button :disabled="!editable || busy || !preparedBins.length" @click="saveSoftwareBins">確認匯入 Bin</button></footer>
        </template>
        <template v-else>
          <p>DEV 儲存程式檔名。正式檔案管理待接續。</p>
          <label>選擇檔案 <input type="file" multiple :disabled="!editable" @change="selectFileNames" /></label>
          <div v-for="(name, index) in program.files" :key="name" class="toolbar"><span>{{ name }}</span><button :disabled="!editable" @click="program.files.splice(index, 1)">移除</button></div>
          <footer><button :disabled="!editable" @click="saveDetails(program)">儲存</button></footer>
        </template>
        <p v-if="error" role="alert">{{ error }}</p>
      </section>
    </div>
  </main>
</template>

<style scoped>
.ft-program { display: grid; align-content: start; gap: 8px; min-width: 0; padding: 10px; background: #fff; color: #292820; font: 12px "Segoe UI", "Microsoft JhengHei", sans-serif; }
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
.editor { display: grid; gap: 12px; width: min(680px, 95vw); max-height: 90vh; overflow: auto; padding: 16px; background: #fff; box-shadow: 0 8px 30px #0004; }
.editor form { display: grid; justify-items: start; gap: 10px; }
footer { justify-content: flex-end; }
[role=alert] { color: #a32020; }
</style>
