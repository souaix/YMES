<script setup lang="ts">
import { computed } from 'vue'
import { reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface AccessoryRow {
  accessoryNo: string
  accessoryType: string
  accessoryCategory: string
  accessoryState: number
  stateColor: string
}
interface AccessoryLog {
  accessoryNo: string
  accessoryType: string
  accessoryState: number
  userNo: string
  startTime: number
  description: string
}
type PickerKind = 'category' | 'type' | null

function timeAgo(hours: number): number {
  return Date.now() - hours * 60 * 60 * 1000
}

function formatTime(value: number): string {
  return new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'medium', hour12: false }).format(value)
}

const stateColors: Record<number, string> = {
  0: '#9aa2a8', 1: '#5c8db8', 2: '#5b9a69', 3: '#d29d42', 4: '#c86d63', 5: '#8b76ad',
}
const categories = [
  { code: 'MASK', name: '光罩配件' },
  { code: 'PROBE', name: '探針配件' },
  { code: 'TOOLING', name: '治具配件' },
]
const typeByCategory: Record<string, Array<{ code: string; name: string }>> = {
  MASK: [{ code: 'RETICLE', name: '光罩' }, { code: 'PEL', name: '保護膜' }],
  PROBE: [{ code: 'PROBE_CARD', name: '探針卡' }, { code: 'PROBE_PIN', name: '探針' }],
  TOOLING: [{ code: 'JIG', name: '治具' }, { code: 'SOCKET', name: '測試座' }],
}
const rows = reactive<AccessoryRow[]>([
  { accessoryNo: 'RET-ETCH-01-003', accessoryType: 'RETICLE', accessoryCategory: 'MASK', accessoryState: 2, stateColor: stateColors[2] },
  { accessoryNo: 'RET-ETCH-01-006', accessoryType: 'RETICLE', accessoryCategory: 'MASK', accessoryState: 1, stateColor: stateColors[1] },
  { accessoryNo: 'RET-ETCH-02-001', accessoryType: 'RETICLE', accessoryCategory: 'MASK', accessoryState: 3, stateColor: stateColors[3] },
  { accessoryNo: 'PEL-LITHO-01-002', accessoryType: 'PEL', accessoryCategory: 'MASK', accessoryState: 0, stateColor: stateColors[0] },
  { accessoryNo: 'PC-PRB-01-001', accessoryType: 'PROBE_CARD', accessoryCategory: 'PROBE', accessoryState: 2, stateColor: stateColors[2] },
  { accessoryNo: 'PC-PRB-02-004', accessoryType: 'PROBE_CARD', accessoryCategory: 'PROBE', accessoryState: 3, stateColor: stateColors[3] },
  { accessoryNo: 'PIN-PRB-07-021', accessoryType: 'PROBE_PIN', accessoryCategory: 'PROBE', accessoryState: 1, stateColor: stateColors[1] },
  { accessoryNo: 'JIG-MOL-01-009', accessoryType: 'JIG', accessoryCategory: 'TOOLING', accessoryState: 5, stateColor: stateColors[5] },
  { accessoryNo: 'JIG-MOL-02-001', accessoryType: 'JIG', accessoryCategory: 'TOOLING', accessoryState: 2, stateColor: stateColors[2] },
  { accessoryNo: 'SOCKET-FT-04-003', accessoryType: 'SOCKET', accessoryCategory: 'TOOLING', accessoryState: 4, stateColor: stateColors[4] },
])
const logRows = ref<AccessoryLog[]>([
  { accessoryNo: 'RET-ETCH-02-001', accessoryType: 'RETICLE', accessoryState: 3, userNo: '10428', startTime: timeAgo(1), description: 'DEV 假資料：送修檢查' },
  { accessoryNo: 'PC-PRB-02-004', accessoryType: 'PROBE_CARD', accessoryState: 3, userNo: '11019', startTime: timeAgo(3), description: 'DEV 假資料：接觸異常檢查' },
  { accessoryNo: 'JIG-MOL-01-009', accessoryType: 'JIG', accessoryState: 5, userNo: '10211', startTime: timeAgo(7), description: 'DEV 假資料：保養後等待驗收' },
])

const criteria = reactive({ category: '', accessoryType: '' })
const selectedNos = ref(new Set<string>())
const pickerKind = ref<PickerKind>(null)
const activeTab = ref<'states' | 'log'>('states')
const loaded = ref(false)
const visibleLogRows = ref<AccessoryLog[]>([])
const feedback = ref('請先選擇配件類別，再選擇類型載入現況。')
const lastChange = ref('')
const pickerTitle = computed(() => pickerKind.value === 'category' ? '選擇配件類別' : '選擇配件類型')
const pickerOptions = computed(() => pickerKind.value === 'category'
  ? categories
  : criteria.category ? typeByCategory[criteria.category] ?? [] : [])
const filteredRows = computed(() => rows.filter((row) => row.accessoryCategory === criteria.category && row.accessoryType === criteria.accessoryType))
const selectedRows = computed(() => filteredRows.value.filter((row) => selectedNos.value.has(row.accessoryNo)))

function selectCategory(code: string) {
  criteria.category = code
  criteria.accessoryType = ''
  selectedNos.value = new Set()
  loaded.value = false
  lastChange.value = ''
  feedback.value = '已選擇配件類別；請再選擇配件類型載入 DEV 現況。'
}

function selectType(code: string) {
  criteria.accessoryType = code
  selectedNos.value = new Set()
  loaded.value = true
  feedback.value = `已載入 ${filteredRows.value.length} 筆 DEV 配件現況。`
  lastChange.value = ''
}

function chooseOption(code: string) {
  if (pickerKind.value === 'category') selectCategory(code)
  else if (pickerKind.value === 'type') selectType(code)
  pickerKind.value = null
}

function toggleRow(accessoryNo: string) {
  const next = new Set(selectedNos.value)
  if (next.has(accessoryNo)) next.delete(accessoryNo)
  else next.add(accessoryNo)
  selectedNos.value = next
}

function toggleAll() {
  selectedNos.value = selectedNos.value.size === filteredRows.value.length
    ? new Set()
    : new Set(filteredRows.value.map((row) => row.accessoryNo))
}

function refreshLog() {
  visibleLogRows.value = [...logRows.value].sort((left, right) => right.startTime - left.startTime)
  feedback.value = `已重新載入 ${visibleLogRows.value.length} 筆 DEV 狀態歷程。`
}

function changeSelectedState(targetState: number) {
  if (!selectedRows.value.length) {
    feedback.value = '請先在 STATES 清單勾選配件。'
    return
  }

  let changed = 0
  let skipped = 0
  const now = Date.now()
  for (const row of selectedRows.value) {
    if (row.accessoryState === targetState) {
      skipped += 1
      continue
    }

    row.accessoryState = targetState
    row.stateColor = stateColors[targetState]
    logRows.value.unshift({
      accessoryNo: row.accessoryNo,
      accessoryType: row.accessoryType,
      accessoryState: targetState,
      userNo: 'DEV_USER',
      startTime: now + changed,
      description: `DEV 假資料：模擬狀態切換至 ${targetState}`,
    })
    changed += 1
  }

  lastChange.value = `成功 ${changed} 筆／略過 ${skipped} 筆／失敗 0 筆（本頁假資料模擬）`
  feedback.value = `狀態變更完成。${lastChange.value}`
  if (changed > 0) refreshLog()
}

</script>

<template>
  <section v-if="dev" class="bu1-page" aria-label="配件狀態監控管理 DEV">
    <header class="page-heading">
      <div class="title-line"><h1>配件狀態監控_管理</h1><span class="dev-badge">DEV 假資料</span></div>
      <p>依配件類別及類型載入現況，可多選配件模擬狀態切換，並檢視狀態歷程。</p>
    </header>

    <section class="filters" aria-label="配件查詢條件">
      <div class="filter-field"><label>Accessory Category</label><div class="picker-input"><input readonly :value="criteria.category" placeholder="請選擇類別" /><button type="button" @click="pickerKind = 'category'">…</button></div></div>
      <div class="filter-field"><label>Accessory Type</label><div class="picker-input"><input readonly :value="criteria.accessoryType" placeholder="請先選擇類別" /><button type="button" :disabled="!criteria.category" @click="pickerKind = 'type'">…</button></div></div>
      <button type="button" class="refresh-button" @click="refreshLog">Refresh LOG</button>
    </section>

    <section class="content-card">
      <nav class="tabs" aria-label="資料檢視">
        <button type="button" :class="{ active: activeTab === 'states' }" @click="activeTab = 'states'">STATES</button>
        <button type="button" :class="{ active: activeTab === 'log' }" @click="activeTab = 'log'">LOG</button>
      </nav>

      <div v-if="activeTab === 'states'" class="state-content">
        <div class="table-wrap">
          <table class="data-table">
            <thead><tr><th class="check-cell"><input type="checkbox" :checked="loaded && filteredRows.length > 0 && selectedNos.size === filteredRows.length" :disabled="!loaded" aria-label="全選配件" @change="toggleAll" /></th><th>ACCESSORYNO</th><th>ACCESSORYTYPE</th><th>ACCESSORYCATEGORY</th><th>ACCESSORYSTATE</th><th>STATECOLOR</th></tr></thead>
            <tbody>
              <tr v-for="row in filteredRows" :key="row.accessoryNo" :class="{ selected: selectedNos.has(row.accessoryNo) }">
                <td class="check-cell"><input type="checkbox" :checked="selectedNos.has(row.accessoryNo)" :aria-label="`選取 ${row.accessoryNo}`" @change="toggleRow(row.accessoryNo)" /></td>
                <td>{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.accessoryCategory }}</td>
                <td><span class="state-pill" :style="{ backgroundColor: row.stateColor }">{{ row.accessoryState }}</span></td>
                <td><span class="color-chip" :style="{ backgroundColor: row.stateColor }"></span>{{ row.stateColor }}</td>
              </tr>
              <tr v-if="!loaded"><td colspan="6" class="empty-cell">選擇配件類別與類型後載入 DEV 假資料。</td></tr>
              <tr v-else-if="filteredRows.length === 0"><td colspan="6" class="empty-cell">目前選取條件沒有 DEV 假資料。</td></tr>
            </tbody>
          </table>
        </div>
        <aside class="state-actions" aria-label="模擬配件狀態">
          <div class="action-heading"><strong>Change state</strong><span>已選 {{ selectedRows.length }} 筆</span></div>
          <button v-for="state in [0, 1, 2, 3, 4, 5]" :key="state" type="button" :disabled="!selectedRows.length" @click="changeSelectedState(state)">切換至 {{ state }}</button>
          <p>若配件已是目標狀態，會計為略過。</p>
          <output v-if="lastChange">{{ lastChange }}</output>
        </aside>
      </div>

      <div v-else class="table-wrap log-wrap">
        <table class="data-table log-table">
          <thead><tr><th>STARTTIME</th><th>ACCESSORYNO</th><th>ACCESSORYTYPE</th><th>ACCESSORYSTATE</th><th>USERNO</th><th>DESCRIPTION</th></tr></thead>
          <tbody>
            <tr v-for="(row, index) in visibleLogRows" :key="`${row.accessoryNo}-${row.startTime}-${index}`"><td>{{ formatTime(row.startTime) }}</td><td>{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.accessoryState }}</td><td>{{ row.userNo }}</td><td>{{ row.description }}</td></tr>
            <tr v-if="visibleLogRows.length === 0"><td colspan="6" class="empty-cell">按 Refresh LOG 載入 DEV 假歷程，或從 STATES 列檢視指定配件。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <footer class="page-footer"><p role="status">{{ feedback }}</p><button type="button" class="close-button" @click="emit('close')">Close (X)</button></footer>

    <div v-if="pickerKind" class="modal-backdrop" @click.self="pickerKind = null">
      <section class="picker-dialog" role="dialog" aria-modal="true" :aria-label="pickerTitle">
        <header><div><h2>{{ pickerTitle }}</h2><p>選擇器內容為 DEV 假資料。</p></div><button type="button" aria-label="關閉" @click="pickerKind = null">×</button></header>
        <div class="picker-options"><button v-for="option in pickerOptions" :key="option.code" type="button" @click="chooseOption(option.code)"><strong>{{ option.code }}</strong><span>{{ option.name }}</span></button></div>
      </section>
    </div>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.bu1-page { display: flex; flex-direction: column; gap: 9px; height: 100%; min-height: 0; padding: 12px 14px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-heading { flex: none; }
.title-line { display: flex; align-items: center; gap: 9px; }
h1 { margin: 0; font-size: 19px; }
.page-heading p { margin: 4px 0 0; color: #777366; font-size: 12px; }
.dev-badge { padding: 3px 8px; border-radius: 12px; background: #fff0bd; color: #6d5713; font-size: 10px; font-weight: 700; }
.filters { display: flex; flex: none; align-items: end; gap: 12px; padding: 9px; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.filter-field { display: grid; width: min(310px, 35%); gap: 4px; }
.filter-field label { color: #555247; font-size: 11px; font-weight: 600; }
.picker-input { display: flex; min-width: 0; }
.picker-input input { flex: 1; width: 100%; min-width: 0; height: 26px; padding: 3px 6px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #26251f; font: inherit; font-size: 11px; }
.picker-input button, .refresh-button, .row-action, .close-button { min-height: 28px; padding: 4px 10px; border: 1px solid #b9b4a6; border-radius: 3px; background: #f8f6ef; color: #37352f; font: inherit; cursor: pointer; }
.picker-input button { width: 29px; margin-left: 4px; }
.picker-input button:disabled { opacity: .45; cursor: not-allowed; }
.refresh-button { margin-left: auto; border-color: #3b3930; background: #3b3930; color: white; }
.content-card { display: flex; flex: 1 1 auto; flex-direction: column; min-height: 0; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
.tabs { display: flex; flex: none; gap: 2px; padding: 5px 7px 0; border-bottom: 1px solid #d8d3c5; background: #f1eee5; }
.tabs button { min-width: 90px; padding: 7px 12px; border: 0; border-bottom: 2px solid transparent; background: transparent; color: #6d695e; font: inherit; font-size: 11px; font-weight: 700; cursor: pointer; }
.tabs button.active { border-bottom-color: #876f25; color: #302e28; }
.state-content { display: flex; flex: 1 1 auto; min-height: 0; }
.table-wrap { flex: 1 1 auto; min-width: 0; min-height: 0; overflow: auto; }
.data-table { width: max-content; min-width: 100%; border-collapse: collapse; font-size: 11px; white-space: nowrap; }
thead { position: sticky; top: 0; z-index: 1; background: #f5f3eb; color: #625f55; text-align: left; }
th, td { padding: 7px 9px; border-bottom: 1px solid #eeeae0; }
th { font-size: 10px; font-weight: 700; }
.check-cell { width: 30px; text-align: center; }
.data-table input { accent-color: #89732c; }
tbody tr.selected { background: #fff8dc; }
.state-pill { display: inline-grid; min-width: 22px; height: 20px; place-items: center; border-radius: 10px; color: white; font-weight: 700; }
.color-chip { display: inline-block; width: 12px; height: 12px; margin-right: 6px; border: 1px solid #7775; border-radius: 50%; vertical-align: -2px; }
.state-actions { display: flex; flex: 0 0 150px; flex-direction: column; gap: 5px; padding: 10px; border-left: 1px solid #e7e3d8; background: #faf8f2; }
.action-heading { display: grid; gap: 3px; margin-bottom: 4px; color: #555247; font-size: 11px; }
.action-heading span, .state-actions p, .state-actions output { color: #777366; font-size: 10px; }
.state-actions button { min-height: 27px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #37352f; font: inherit; font-size: 11px; cursor: pointer; }
.state-actions button:disabled { opacity: .45; cursor: not-allowed; }
.state-actions p { margin: 3px 0; line-height: 1.5; }
.state-actions output { padding: 6px; border-radius: 3px; background: #fff0bd; line-height: 1.5; }
.log-wrap { flex: 1 1 auto; }
.log-table { min-width: 780px; }
.empty-cell { padding: 24px 10px; color: #8a867a; text-align: center; }
.page-footer { display: flex; flex: none; align-items: center; justify-content: space-between; min-height: 29px; gap: 10px; }
.page-footer p { margin: 0; color: #686457; font-size: 11px; }
.close-button { min-width: 78px; min-height: 26px; border-color: #3b3930; background: #3b3930; color: white; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 18px; background: #18171277; }
.picker-dialog { width: min(460px, 100%); max-height: min(500px, 90vh); display: flex; flex-direction: column; padding: 14px; border: 1px solid #c9c3b4; border-radius: 7px; background: #fffdf7; box-shadow: 0 16px 48px #0003; }
.picker-dialog header { display: flex; justify-content: space-between; gap: 14px; margin-bottom: 10px; }
.picker-dialog h2 { margin: 0; font-size: 16px; }
.picker-dialog p { margin: 4px 0 0; color: #777366; font-size: 11px; }
.picker-dialog header button { width: 28px; height: 28px; border: 0; border-radius: 4px; background: transparent; color: #625f55; font-size: 21px; cursor: pointer; }
.picker-options { display: grid; gap: 5px; overflow: auto; }
.picker-options button { display: flex; align-items: center; gap: 12px; min-height: 35px; padding: 5px 8px; border: 1px solid #e3dfd4; border-radius: 4px; background: white; color: #36342e; text-align: left; font: inherit; cursor: pointer; }
.picker-options strong { min-width: 130px; font-size: 11px; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
button:focus-visible, input:focus-visible { outline: 2px solid #d8b622; outline-offset: 2px; }
@media (max-width: 700px) { .filters { align-items: stretch; flex-direction: column; } .filter-field { width: 100%; } .refresh-button { margin-left: 0; } .state-content { flex-direction: column; } .state-actions { display: grid; grid-template-columns: repeat(3, 1fr); flex: none; border-top: 1px solid #e7e3d8; border-left: 0; } .action-heading, .state-actions p, .state-actions output { grid-column: 1 / -1; } }
</style>
