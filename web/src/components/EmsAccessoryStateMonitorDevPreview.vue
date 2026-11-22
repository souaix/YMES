<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface Category { code: string; name: string }
interface AccessoryType { code: string; name: string; category: string; prefix: string; count: number }
interface Accessory { no: string; category: string; type: string; state: number; version: string; location: string; changedAt: string }
interface State { code: number; name: string; color: string }
type DetailMode = 'accessory-log' | 'equipment-log' | 'current-state' | 'equipment-state' | null

const categories: Category[] = [
  { code: 'MASK', name: '光罩配件' },
  { code: 'PROBE', name: '探針卡' },
  { code: 'MOLD', name: '封裝治具' },
]
const types: AccessoryType[] = [
  { code: 'MASK-RETICLE', name: '光罩載具', category: 'MASK', prefix: 'RET', count: 26 },
  { code: 'MASK-COVER', name: '光罩保護蓋', category: 'MASK', prefix: 'COV', count: 8 },
  { code: 'PROBE-CARD', name: '探針卡', category: 'PROBE', prefix: 'PRB', count: 23 },
  { code: 'PROBE-NEEDLE', name: '探針組', category: 'PROBE', prefix: 'NDL', count: 7 },
  { code: 'MOLD-DIE', name: '模具治具', category: 'MOLD', prefix: 'MOL', count: 21 },
]
const states: State[] = [
  { code: 1, name: '庫存', color: '#5b9b70' },
  { code: 2, name: '使用中', color: '#4e829e' },
  { code: 3, name: '維修中', color: '#c8953e' },
  { code: 4, name: '報廢', color: '#ae5650' },
  { code: 5, name: '待驗收', color: '#8b75a9' },
]
const operationName: Record<number, string> = { 1: 'Return', 2: 'Apply', 3: 'Repair', 4: 'Scrap', 5: '待驗收' }
const operationHelp: Record<number, string> = {
  1: '模擬配件歸還庫存流程。WinForms 另開 Return 對話窗；DEV 僅改本頁假資料。',
  2: '模擬配件領用流程。WinForms 另開 Apply 對話窗；DEV 僅改本頁假資料。',
  3: '模擬維修狀態說明。正式流程可能由狀態控制設定指定其他畫面。',
  4: '模擬報廢狀態說明。正式流程的權限與互斥規則須由 API 再核對。',
  5: '模擬一般狀態變更說明。',
}
const accessoryFixtures: Accessory[] = types.flatMap((type) => Array.from({ length: type.count }, (_, index) => {
  const sequence = String(index + 1).padStart(3, '0')
  const state = [1, 2, 1, 3, 5, 2, 1, 4][(index + type.count) % 8]
  return {
    no: type.prefix + '-' + sequence,
    category: type.category,
    type: type.code,
    state,
    version: '0' + (index % 3 + 1),
    location: state === 2 ? 'EQP-' + String(index % 7 + 1).padStart(3, '0') : 'ACC-STOCK',
    changedAt: '2026-11-' + String(index % 20 + 1).padStart(2, '0') + ' 08:30',
  }
}))
const currentState = reactive<Record<string, number>>(Object.fromEntries(accessoryFixtures.map((item) => [item.no, item.state])))
const selectedCategory = ref('')
const selectedType = ref('')
const loadedType = ref('')
const selectedAccessoryNo = ref('')
const currentPage = ref(1)
const refreshCount = ref(0)
const statusMessage = ref('請先選擇配件分類與類型')
const contextMenu = ref<{ x: number; y: number; accessoryNo: string } | null>(null)
const detailMode = ref<DetailMode>(null)
const detailAccessoryNo = ref('')
const pendingState = ref<number | null>(null)
const changeDescription = ref('')
const messageText = ref('')

const availableTypes = computed(() => types.filter((item) => item.category === selectedCategory.value))
const loadedTypeRow = computed(() => types.find((item) => item.code === loadedType.value) ?? null)
const loadedAccessories = computed(() => accessoryFixtures.filter((item) => item.type === loadedType.value))
const totalPages = computed(() => Math.max(1, Math.ceil(loadedAccessories.value.length / 20)))
const visibleAccessories = computed(() => loadedAccessories.value.slice((currentPage.value - 1) * 20, currentPage.value * 20))
const selectedAccessory = computed(() => accessoryFixtures.find((item) => item.no === selectedAccessoryNo.value) ?? null)
const detailAccessory = computed(() => accessoryFixtures.find((item) => item.no === detailAccessoryNo.value) ?? null)
const pageTitle = computed(() => ({
  'accessory-log': 'Accessory State Log',
  'equipment-log': 'Equipment Accessory State Log',
  'current-state': 'Accessory State',
  'equipment-state': 'Equipment Accessory State',
}[detailMode.value ?? 'accessory-log']))
const stateButtons = computed(() => states.map((state) => ({ ...state, action: operationName[state.code] })))

function stateFor(code: number): State {
  return states.find((item) => item.code === code) ?? { code, name: '未知狀態', color: '#98958a' }
}

function selectCategory(): void {
  selectedType.value = ''
  loadedType.value = ''
  selectedAccessoryNo.value = ''
  currentPage.value = 1
  contextMenu.value = null
  statusMessage.value = selectedCategory.value ? '已載入 ' + selectedCategory.value + ' 的狀態按鈕；請選擇配件類型。' : '請先選擇配件分類與類型'
}

function loadType(): void {
  if (!selectedCategory.value || !selectedType.value) {
    messageText.value = '請先選取配件分類與類型。'
    return
  }
  loadedType.value = selectedType.value
  selectedAccessoryNo.value = ''
  currentPage.value = 1
  contextMenu.value = null
  statusMessage.value = '已載入 ' + loadedType.value + ' 的配件狀態假資料'
}

function changePage(page: number): void {
  currentPage.value = Math.min(totalPages.value, Math.max(1, page))
  selectedAccessoryNo.value = ''
  contextMenu.value = null
  statusMessage.value = '第 ' + currentPage.value + ' 頁 / 共 ' + totalPages.value + ' 頁'
}

function onPageInput(event: Event): void {
  const target = event.target as HTMLInputElement | null
  if (target) changePage(Number(target.value))
}

function selectAccessory(accessory: Accessory): void {
  selectedAccessoryNo.value = accessory.no
  contextMenu.value = null
  statusMessage.value = '已選取配件 ' + accessory.no + ' · ' + stateFor(currentState[accessory.no]).name
}

function refreshStates(): void {
  if (!loadedType.value) return
  refreshCount.value += 1
  selectedAccessoryNo.value = ''
  contextMenu.value = null
  statusMessage.value = 'DEV 假資料已重新載入；頁面內狀態變更保留（Refresh ' + refreshCount.value + '）'
}

function forbiddenReason(accessory: Accessory, targetState: number): string {
  const current = currentState[accessory.no]
  if (current === targetState) return '目前已是此狀態'
  // DEV-only example pairs stand in for the category-specific violation matrix.
  if ((current === 2 && targetState === 4) || (current === 3 && targetState === 1)) return '配件分類互斥設定禁止此狀態轉換'
  return ''
}

function requestStateChange(targetState: number): void {
  if (!selectedAccessory.value) {
    messageText.value = '請先選取配件。'
    return
  }
  const reason = forbiddenReason(selectedAccessory.value, targetState)
  if (reason) {
    messageText.value = reason
    return
  }
  if (targetState === 5) {
    currentState[selectedAccessory.value.no] = targetState
    statusMessage.value = selectedAccessory.value.no + ' 已在本頁 DEV 假資料直接改為「' + stateFor(targetState).name + '」'
    return
  }
  pendingState.value = targetState
  changeDescription.value = ''
}

function applyStateChange(): void {
  if (!selectedAccessory.value || pendingState.value === null) return
  if (!changeDescription.value.trim()) {
    messageText.value = '請輸入 DEV 狀態變更說明。'
    return
  }
  const accessory = selectedAccessory.value
  currentState[accessory.no] = pendingState.value
  statusMessage.value = accessory.no + ' 已在本頁 DEV 假資料改為「' + stateFor(pendingState.value).name + '」'
  pendingState.value = null
  changeDescription.value = ''
}

function showContext(event: MouseEvent, accessory: Accessory): void {
  selectAccessory(accessory)
  contextMenu.value = { x: event.clientX, y: event.clientY, accessoryNo: accessory.no }
}

function openDetail(mode: Exclude<DetailMode, null>, accessoryNo: string): void {
  detailAccessoryNo.value = accessoryNo
  detailMode.value = mode
  contextMenu.value = null
}

function closeMonitor(): void {
  loadedType.value = ''
  selectedAccessoryNo.value = ''
  contextMenu.value = null
  pendingState.value = null
}
</script>

<template>
  <section v-if="dev" class="accessory-monitor" aria-label="配件狀態監控 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES304 · DEV fixtures</div><h1>配件狀態監控 <span>ACCESSORY STATE MONITOR</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>分類、配件、狀態色及互斥 pair 均為虛構；狀態變更僅存在本頁記憶體。</span></div>

    <section class="monitor-window" aria-label="Accessory State Monitor">
      <div class="toolbar">
        <label>Accessory Category
          <select v-model="selectedCategory" @change="selectCategory">
            <option value="">請選擇分類</option>
            <option v-for="category in categories" :key="category.code" :value="category.code">{{ category.name }} ({{ category.code }})</option>
          </select>
        </label>
        <label>Accessory Type
          <select v-model="selectedType" :disabled="!selectedCategory">
            <option value="">請選擇類型</option>
            <option v-for="type in availableTypes" :key="type.code" :value="type.code">{{ type.name }} ({{ type.code }})</option>
          </select>
        </label>
        <div class="page-controls">
          <span>Total Page <strong>{{ loadedType ? totalPages : 0 }}</strong></span>
          <button type="button" aria-label="上一頁" :disabled="!loadedType || currentPage <= 1" @click="changePage(currentPage - 1)">‹</button>
          <label>Page <input :value="loadedType ? currentPage : 0" type="number" :min="loadedType ? 1 : 0" :max="loadedType ? totalPages : 0" :disabled="!loadedType" aria-label="目前頁次" @change="onPageInput" /></label>
          <button type="button" aria-label="下一頁" :disabled="!loadedType || currentPage >= totalPages" @click="changePage(currentPage + 1)">›</button>
        </div>
        <button type="button" class="plain-button" :disabled="!loadedType" @click="refreshStates">Refresh</button>
        <button type="button" class="primary-button" @click="loadType">Search</button>
      </div>

      <div class="monitor-meta">
        <span>分類：{{ categories.find((item) => item.code === selectedCategory)?.name || '—' }}</span>
        <span>類型：{{ loadedTypeRow?.name || '—' }}</span>
        <span>配件 {{ loadedType ? loadedAccessories.length : 0 }} 筆 · 每頁 20 筆</span>
      </div>

      <section class="accessory-board" aria-label="配件現況清單">
        <button v-for="accessory in visibleAccessories" :key="accessory.no" type="button" class="accessory-card" :class="{ selected: selectedAccessoryNo === accessory.no }" @click="selectAccessory(accessory)" @dblclick="openDetail('equipment-state', accessory.no)" @contextmenu.prevent.stop="showContext($event, accessory)">
          <span class="accessory-glyph" aria-hidden="true">◈</span>
          <span class="accessory-name" :style="{ backgroundColor: stateFor(currentState[accessory.no]).color }">{{ accessory.no }}</span>
          <span class="accessory-state">{{ stateFor(currentState[accessory.no]).name }}</span>
          <span class="accessory-location">{{ accessory.location }}</span>
        </button>
        <div v-if="!loadedType" class="empty-board">請先選擇配件分類與類型，再按 Search 載入卡片。</div>
        <div v-else-if="visibleAccessories.length === 0" class="empty-board">此配件類型目前沒有假資料。</div>
      </section>

      <section class="action-panel" aria-label="Accessory Action">
        <div class="action-heading"><h2>Accessory Action</h2><span v-if="selectedAccessory">{{ selectedAccessory.no }} · 目前狀態：{{ stateFor(currentState[selectedAccessory.no]).name }}</span><span v-else>選取配件後，可執行狀態變更</span></div>
        <div class="state-buttons">
          <button v-for="state in stateButtons" :key="state.code" type="button" :disabled="!selectedAccessory || Boolean(forbiddenReason(selectedAccessory, state.code))" :title="selectedAccessory ? (forbiddenReason(selectedAccessory, state.code) || operationHelp[state.code]) : '請先選取配件'" @click="requestStateChange(state.code)">
            <i :style="{ backgroundColor: state.color }"></i><span>{{ state.action }}</span><small>{{ state.name }}</small>
          </button>
        </div>
      </section>
      <footer class="monitor-status"><span>{{ statusMessage }}</span><span>Refresh {{ refreshCount }}</span></footer>
      <button type="button" class="close-button" @click="closeMonitor(); emit('close')">Close</button>
    </section>

    <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @click.stop>
      <button type="button" @click="openDetail('accessory-log', contextMenu.accessoryNo)">顯示配件狀態歷史資料</button>
      <button type="button" @click="openDetail('equipment-log', contextMenu.accessoryNo)">顯示設備配件歷史資料</button>
      <button type="button" @click="openDetail('current-state', contextMenu.accessoryNo)">顯示配件現況</button>
    </div>

    <section v-if="detailMode && detailAccessory" class="dialog-backdrop" @click.self="detailMode = null">
      <article class="detail-dialog" role="dialog" aria-modal="true" :aria-label="pageTitle">
        <header><div><div class="eyebrow">{{ pageTitle }} · DEV preview</div><h2>{{ detailAccessory.no }}</h2></div><button type="button" class="close-icon" aria-label="關閉" @click="detailMode = null">×</button></header>
        <dl><dt>Accessory Category</dt><dd>{{ detailAccessory.category }}</dd><dt>Accessory Type</dt><dd>{{ detailAccessory.type }}</dd><dt>Accessory Version</dt><dd>{{ detailAccessory.version }}</dd><dt>Accessory State</dt><dd>{{ stateFor(currentState[detailAccessory.no]).name }}</dd><dt>Location</dt><dd>{{ detailAccessory.location }}</dd><dt>Last Change</dt><dd>{{ detailAccessory.changedAt }}</dd></dl>
        <p>此處模擬 WinForms 開啟的外部查詢視窗，資料僅為本頁 fixture。</p>
        <footer><button type="button" class="primary-button" @click="detailMode = null">Close</button></footer>
      </article>
    </section>

    <section v-if="pendingState !== null && selectedAccessory" class="dialog-backdrop" @click.self="pendingState = null">
      <article class="detail-dialog" role="dialog" aria-modal="true" aria-label="DEV 配件狀態變更">
        <header><div><div class="eyebrow">DEV · Accessory State Change</div><h2>{{ selectedAccessory.no }}</h2></div><button type="button" class="close-icon" aria-label="取消" @click="pendingState = null">×</button></header>
        <p>狀態：{{ stateFor(currentState[selectedAccessory.no]).name }} → {{ stateFor(pendingState).name }}</p>
        <p class="dialog-note">{{ operationHelp[pendingState] }}</p>
        <label class="reason-label">狀態變更說明<textarea v-model="changeDescription" rows="3" maxlength="255" placeholder="DEV 假資料：輸入變更說明"></textarea></label>
        <p class="dialog-warning">未執行正式配件上機／下機、壽命、版本、使用者權限及跨表交易。</p>
        <footer><button type="button" class="plain-button" @click="pendingState = null">Cancel</button><button type="button" class="primary-button" @click="applyStateChange">Apply to DEV fixture</button></footer>
      </article>
    </section>

    <div v-if="messageText" class="message-backdrop" @click.self="messageText = ''"><div class="message-box" role="alert"><p>{{ messageText }}</p><button type="button" class="primary-button" @click="messageText = ''">OK</button></div></div>
  </section>
</template>

<style scoped>
.accessory-monitor { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 14px; background: #f1f0eb; color: #24251f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin: 0 auto 12px; max-width: 1320px; }
.eyebrow { color: #777669; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }
.page-header h1 { margin: 1px 0 0; font-size: 22px; line-height: 1.25; }
.page-header h1 span { margin-left: 8px; color: #858376; font-size: 11px; font-weight: 500; }
.close-icon { border: 0; background: transparent; color: #605f56; font-size: 24px; line-height: 1; cursor: pointer; }
.fixture-banner { display: flex; gap: 10px; align-items: center; max-width: 1320px; box-sizing: border-box; margin: 0 auto 10px; padding: 8px 11px; border: 1px solid #e3d19c; border-radius: 4px; background: #fff8de; color: #685b2e; }
.fixture-banner strong { white-space: nowrap; }
.monitor-window { position: relative; max-width: 1320px; min-height: 430px; box-sizing: border-box; margin: auto; padding: 12px; border: 1px solid #b9b7ac; border-radius: 5px; background: #fbfaf6; box-shadow: 0 2px 9px #2221; }
.toolbar { display: flex; align-items: end; flex-wrap: wrap; gap: 10px; padding-bottom: 10px; border-bottom: 1px solid #deddd5; }
.toolbar > label { display: grid; gap: 3px; color: #66655c; font-size: 11px; }
.toolbar select { min-width: 205px; height: 31px; box-sizing: border-box; padding: 4px 8px; border: 1px solid #aaa89e; border-radius: 3px; background: white; color: #25251f; font: inherit; font-size: 12px; }
.toolbar select:disabled { background: #eeede8; color: #97958b; }
.page-controls { display: flex; align-items: center; gap: 6px; color: #6b6a61; font-size: 11px; }
.page-controls > span { white-space: nowrap; }
.page-controls strong { color: #282821; }
.page-controls button { width: 26px; height: 28px; border: 1px solid #bcbab0; border-radius: 3px; background: #f5f4ef; color: #333; font-size: 18px; cursor: pointer; }
.page-controls button:disabled { color: #aaa89e; cursor: default; }
.page-controls label { display: flex; align-items: center; gap: 4px; white-space: nowrap; }
.page-controls input { width: 48px; height: 27px; box-sizing: border-box; border: 1px solid #bcbab0; border-radius: 3px; text-align: center; font: inherit; }
.plain-button, .primary-button, .close-button { min-height: 29px; padding: 4px 11px; border: 1px solid #aaa89e; border-radius: 3px; background: #f7f6f1; color: #282821; font: inherit; cursor: pointer; }
.plain-button:disabled { opacity: .5; cursor: default; }
.primary-button { border-color: #4a4a40; background: #48483e; color: white; }
.monitor-meta { display: flex; flex-wrap: wrap; gap: 18px; padding: 8px 2px; color: #6d6b61; font-size: 11px; }
.accessory-board { display: grid; grid-template-columns: repeat(5, minmax(110px, 1fr)); gap: 8px; min-height: 214px; padding: 8px; border: 1px solid #d2d0c7; border-radius: 4px; background: #f2f1eb; }
.accessory-card { display: flex; min-width: 0; min-height: 79px; flex-direction: column; align-items: stretch; gap: 3px; padding: 6px; border: 1px solid #d0cec3; border-radius: 4px; background: #fff; color: #35352e; cursor: pointer; text-align: left; }
.accessory-card:hover { border-color: #8f8d80; }
.accessory-card.selected { outline: 2px solid #4d5f73; outline-offset: 1px; }
.accessory-glyph { align-self: center; color: #79786e; font-size: 20px; line-height: 1; }
.accessory-name { overflow: hidden; padding: 3px 4px; border-radius: 2px; color: #fff; font-weight: 600; text-align: center; text-overflow: ellipsis; white-space: nowrap; }
.accessory-state, .accessory-location { color: #66655c; font-size: 10px; text-align: center; }
.empty-board { grid-column: 1 / -1; display: grid; place-items: center; min-height: 170px; color: #848277; }
.action-panel { margin-top: 11px; padding: 9px 10px; border: 1px solid #d0cec5; border-radius: 4px; background: #f8f7f2; }
.action-heading { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; color: #77756b; font-size: 11px; }
.action-heading h2 { margin: 0; color: #33332d; font-size: 13px; }
.state-buttons { display: flex; flex-wrap: wrap; gap: 7px; margin-top: 7px; }
.state-buttons button { display: flex; min-width: 90px; align-items: center; gap: 6px; padding: 5px 8px; border: 1px solid #c4c2b8; border-radius: 3px; background: white; color: #33332d; cursor: pointer; text-align: left; }
.state-buttons button:disabled { opacity: .42; cursor: default; }
.state-buttons i { width: 10px; height: 10px; flex: 0 0 auto; border-radius: 50%; }
.state-buttons span { font-size: 11px; font-weight: 600; }
.state-buttons small { margin-left: auto; color: #77756b; font-size: 10px; }
.monitor-status { display: flex; justify-content: space-between; gap: 12px; min-height: 24px; padding: 7px 1px 0; color: #77756b; font-size: 11px; }
.close-button { display: block; min-width: 72px; margin: 5px 0 0 auto; }
.context-menu { position: fixed; z-index: 30; display: grid; min-width: 220px; padding: 4px; border: 1px solid #8c8a80; border-radius: 3px; background: #fffefb; box-shadow: 0 3px 12px #0003; }
.context-menu button { padding: 7px 9px; border: 0; background: transparent; color: #33332d; cursor: pointer; text-align: left; font: inherit; }
.context-menu button:hover { background: #eeede6; }
.dialog-backdrop, .message-backdrop { position: fixed; z-index: 40; inset: 0; display: grid; place-items: center; padding: 18px; background: #17171366; }
.detail-dialog, .message-box { width: min(520px, 100%); box-sizing: border-box; padding: 17px; border: 1px solid #aaa89e; border-radius: 5px; background: #fcfbf7; box-shadow: 0 8px 28px #0003; }
.detail-dialog > header { display: flex; align-items: start; justify-content: space-between; gap: 12px; padding-bottom: 10px; border-bottom: 1px solid #deddd5; }
.detail-dialog h2 { margin: 3px 0 0; font-size: 18px; }
.detail-dialog dl { display: grid; grid-template-columns: 150px 1fr; gap: 7px 12px; margin: 14px 0; }
.detail-dialog dt { color: #77756b; }
.detail-dialog dd { margin: 0; font-weight: 600; }
.detail-dialog p { color: #77756b; }
.detail-dialog footer { display: flex; justify-content: end; gap: 8px; margin-top: 12px; }
.dialog-note { padding: 8px; border-left: 3px solid #bba55d; background: #f7f3e4; }
.reason-label { display: grid; gap: 5px; font-size: 12px; }
.reason-label textarea { box-sizing: border-box; width: 100%; resize: vertical; padding: 7px; border: 1px solid #bcbab0; border-radius: 3px; font: inherit; }
.detail-dialog .dialog-warning { color: #8c5a3f; font-size: 11px; }
.message-box { width: min(360px, 100%); }
.message-box p { margin-top: 0; }
.message-box button { display: block; margin-left: auto; }
@media (max-width: 850px) { .accessory-board { grid-template-columns: repeat(4, minmax(90px, 1fr)); } .toolbar select { min-width: 160px; } }
@media (max-width: 600px) { .page-header h1 span { display: block; margin: 3px 0 0; } .toolbar { align-items: stretch; } .toolbar > label { width: 100%; } .toolbar select { width: 100%; } .accessory-board { grid-template-columns: repeat(2, minmax(90px, 1fr)); } .monitor-status { flex-direction: column; } }
</style>
