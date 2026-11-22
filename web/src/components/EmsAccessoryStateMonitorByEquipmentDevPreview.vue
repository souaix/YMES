<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface EquipmentType { code: string; name: string }
interface Equipment { no: string; type: string; name: string }
interface MountedAccessory { no: string; equipmentNo: string; equipmentType: string; category: string; state: number }
interface State { code: number; name: string; color: string }
type DetailMode = 'accessory-log' | 'equipment-log' | 'current-state' | 'equipment-state' | null

const equipmentTypes: EquipmentType[] = [
  { code: 'PROBER', name: '晶圓測試機' },
  { code: 'ETCHER', name: '蝕刻設備' },
  { code: 'MOLDER', name: '封裝成型設備' },
]
const equipmentRows: Equipment[] = [
  { no: 'PRB-01', type: 'PROBER', name: '晶圓測試機 01' },
  { no: 'PRB-02', type: 'PROBER', name: '晶圓測試機 02' },
  { no: 'ETCH-01', type: 'ETCHER', name: '蝕刻機 01' },
  { no: 'ETCH-02', type: 'ETCHER', name: '蝕刻機 02' },
  { no: 'MOL-01', type: 'MOLDER', name: '封裝成型機 01' },
]
const states: State[] = [
  { code: 1, name: '庫存', color: '#5b9b70' },
  { code: 2, name: '使用中', color: '#4e829e' },
  { code: 3, name: '維修中', color: '#c8953e' },
  { code: 4, name: '報廢', color: '#ae5650' },
  { code: 5, name: '待驗收', color: '#8b75a9' },
]
const actionName: Record<number, string> = { 1: 'Return', 2: 'Apply', 3: 'Repair', 4: 'Scrap', 5: '待驗收' }
const actionHelp: Record<number, string> = {
  1: 'WinForms 會開啟 ES302 下機視窗；DEV 僅模擬狀態更新，不改設備掛載關聯。',
  2: 'WinForms 會開啟 ES301 上機視窗；DEV 僅模擬狀態更新，不改設備掛載關聯。',
  3: '依配件分類控制設定可能開啟狀態說明介面。',
  4: '依配件分類控制設定可能開啟狀態說明介面。',
  5: '一般狀態基礎按鈕；DEV 模擬直接變更狀態。',
}
const stateCodesByCategory: Record<string, number[]> = {
  PROBE: [1, 2, 3, 4, 5],
  MASK: [1, 2, 3, 5],
  MOLD: [1, 2, 3, 4],
}
const mountedAccessories: MountedAccessory[] = equipmentRows.flatMap((equipment, equipmentIndex) => {
  const count = equipment.no === 'PRB-01' ? 24 : equipment.no === 'ETCH-01' ? 22 : equipment.type === 'PROBER' ? 8 : 6
  const category = equipment.type === 'PROBER' ? 'PROBE' : equipment.type === 'ETCHER' ? 'MASK' : 'MOLD'
  const prefix = equipment.type === 'PROBER' ? 'PC' : equipment.type === 'ETCHER' ? 'RET' : 'JIG'
  return Array.from({ length: count }, (_, index) => {
    const sequence = String(index + 1).padStart(3, '0')
    const state = [2, 2, 3, 5, 2, 4, 2, 1][(index + equipmentIndex) % 8]
    return {
      no: prefix + '-' + equipment.no + '-' + sequence,
      equipmentNo: equipment.no,
      equipmentType: equipment.type,
      category,
      state,
    }
  })
})
const currentState = reactive<Record<string, number>>(Object.fromEntries(mountedAccessories.map((item) => [item.no, item.state])))
const selectedEquipmentType = ref('')
const selectedEquipmentNo = ref('')
const loaded = ref(false)
const selectedAccessoryNo = ref('')
const currentPage = ref(1)
const refreshCount = ref(0)
const statusMessage = ref('請選擇設備類別或設備編號')
const contextMenu = ref<{ x: number; y: number; accessoryNo: string } | null>(null)
const detailMode = ref<DetailMode>(null)
const detailAccessoryNo = ref('')
const pendingState = ref<number | null>(null)
const description = ref('')
const messageText = ref('')

const equipmentOptions = computed(() => equipmentRows.filter((item) => !selectedEquipmentType.value || item.type === selectedEquipmentType.value))
const visibleEquipment = computed(() => equipmentRows.find((item) => item.no === selectedEquipmentNo.value) ?? null)
const currentAccessories = computed(() => mountedAccessories
  .filter((item) => (!selectedEquipmentType.value || item.equipmentType === selectedEquipmentType.value)
    && (!selectedEquipmentNo.value || item.equipmentNo === selectedEquipmentNo.value))
  .sort((left, right) => left.category.localeCompare(right.category) || left.no.localeCompare(right.no)))
const totalPages = computed(() => Math.max(1, Math.ceil(currentAccessories.value.length / 20)))
const visibleAccessories = computed(() => currentAccessories.value.slice((currentPage.value - 1) * 20, currentPage.value * 20))
const selectedAccessory = computed(() => mountedAccessories.find((item) => item.no === selectedAccessoryNo.value) ?? null)
const detailAccessory = computed(() => mountedAccessories.find((item) => item.no === detailAccessoryNo.value) ?? null)
const detailTitle = computed(() => ({
  'accessory-log': 'Accessory State Log',
  'equipment-log': 'Equipment Accessory State Log',
  'current-state': 'Accessory State',
  'equipment-state': 'Equipment Accessory State',
}[detailMode.value ?? 'accessory-log']))
const stateButtons = computed(() => {
  const category = selectedAccessory.value?.category
  return states.filter((item) => category ? stateCodesByCategory[category]?.includes(item.code) : false)
    .map((item) => ({ ...item, action: actionName[item.code] }))
})

function stateFor(code: number): State {
  return states.find((item) => item.code === code) ?? { code, name: '未知狀態', color: '#98958a' }
}

function loadMonitor(): void {
  loaded.value = true
  currentPage.value = 1
  selectedAccessoryNo.value = ''
  contextMenu.value = null
  const filter = selectedEquipmentNo.value || selectedEquipmentType.value || '全部設備（DEV）'
  statusMessage.value = '已載入 ' + filter + ' 的掛載配件假資料'
}

function onEquipmentTypeChange(): void {
  selectedEquipmentNo.value = ''
  loadMonitor()
}

function onEquipmentNoChange(): void {
  const equipment = equipmentRows.find((item) => item.no === selectedEquipmentNo.value)
  if (equipment) selectedEquipmentType.value = equipment.type
  loadMonitor()
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

function selectAccessory(accessory: MountedAccessory): void {
  selectedAccessoryNo.value = accessory.no
  contextMenu.value = null
  statusMessage.value = '已選取 ' + accessory.no + ' · ' + accessory.category + ' · ' + stateFor(currentState[accessory.no]).name
}

function refresh(): void {
  if (!loaded.value) return
  refreshCount.value += 1
  selectedAccessoryNo.value = ''
  contextMenu.value = null
  statusMessage.value = 'DEV 掛載資料已重新載入；頁面內狀態變更保留（Refresh ' + refreshCount.value + '）'
}

function forbiddenReason(accessory: MountedAccessory, targetState: number): string {
  const current = currentState[accessory.no]
  if (current === targetState) return '目前已是此狀態'
  // Mock category-specific forbidden pairs for the DEV preview only.
  if ((accessory.category === 'PROBE' && current === 2 && targetState === 4)
    || (accessory.category === 'MASK' && current === 3 && targetState === 1)
    || (accessory.category === 'MOLD' && current === 2 && targetState === 5)) return '配件分類互斥設定禁止此狀態轉換'
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
  description.value = ''
}

function applyStateChange(): void {
  if (!selectedAccessory.value || pendingState.value === null) return
  if (!description.value.trim()) {
    messageText.value = '請輸入 DEV 狀態變更說明。'
    return
  }
  const accessory = selectedAccessory.value
  currentState[accessory.no] = pendingState.value
  statusMessage.value = accessory.no + ' 已在本頁 DEV 假資料改為「' + stateFor(pendingState.value).name + '」；設備掛載假資料不變'
  pendingState.value = null
  description.value = ''
}

function showContext(event: MouseEvent, accessory: MountedAccessory): void {
  selectAccessory(accessory)
  contextMenu.value = { x: event.clientX, y: event.clientY, accessoryNo: accessory.no }
}

function openDetail(mode: Exclude<DetailMode, null>, accessoryNo: string): void {
  detailAccessoryNo.value = accessoryNo
  detailMode.value = mode
  contextMenu.value = null
}

function close(): void {
  contextMenu.value = null
  pendingState.value = null
  emit('close')
}
</script>

<template>
  <section v-if="dev" class="accessory-equipment-monitor" aria-label="配件狀態監控_設備 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES305 · DEV fixtures</div><h1>配件狀態監控_設備 <span>ACCESSORY MONITOR BY EQUIPMENT</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="close">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>設備、掛載關聯、配件狀態及互斥 pair 均為虛構；狀態更新不改掛載紀錄。</span></div>

    <section class="monitor-window" aria-label="Accessory State Monitor By Equipment">
      <div class="toolbar">
        <label>Equipment Type
          <select v-model="selectedEquipmentType" @change="onEquipmentTypeChange">
            <option value="">選擇設備類別</option>
            <option v-for="type in equipmentTypes" :key="type.code" :value="type.code">{{ type.name }} ({{ type.code }})</option>
          </select>
        </label>
        <label>Equipment No
          <select v-model="selectedEquipmentNo" @change="onEquipmentNoChange">
            <option value="">同類別全部設備</option>
            <option v-for="equipment in equipmentOptions" :key="equipment.no" :value="equipment.no">{{ equipment.no }} · {{ equipment.name }}</option>
          </select>
        </label>
        <div class="page-controls">
          <span>Total Page <strong>{{ loaded ? totalPages : 0 }}</strong></span>
          <button type="button" aria-label="上一頁" :disabled="!loaded || currentPage <= 1" @click="changePage(currentPage - 1)">‹</button>
          <label>Page <input :value="loaded ? currentPage : 0" type="number" :min="loaded ? 1 : 0" :max="loaded ? totalPages : 0" :disabled="!loaded" aria-label="目前頁次" @change="onPageInput" /></label>
          <button type="button" aria-label="下一頁" :disabled="!loaded || currentPage >= totalPages" @click="changePage(currentPage + 1)">›</button>
        </div>
        <button type="button" class="plain-button" :disabled="!loaded" @click="refresh">Refresh</button>
      </div>

      <div class="monitor-meta">
        <span>設備類別：{{ equipmentTypes.find((item) => item.code === selectedEquipmentType)?.name || '全部' }}</span>
        <span>設備編號：{{ visibleEquipment?.no || (selectedEquipmentType ? '同類別全部設備' : '全部') }}</span>
        <span>掛載配件 {{ loaded ? currentAccessories.length : 0 }} 筆 · 每頁 20 筆</span>
      </div>

      <section class="accessory-board" aria-label="設備掛載配件清單">
        <button v-for="accessory in visibleAccessories" :key="accessory.no" type="button" class="accessory-card" :class="{ selected: selectedAccessoryNo === accessory.no }" :title="stateFor(currentState[accessory.no]).name + ' · ' + accessory.equipmentNo" @click="selectAccessory(accessory)" @dblclick="openDetail('equipment-state', accessory.no)" @contextmenu.prevent.stop="showContext($event, accessory)">
          <span class="card-topline"><i :style="{ backgroundColor: stateFor(currentState[accessory.no]).color }"></i><strong>{{ accessory.no }}</strong></span>
          <span class="state-name">{{ stateFor(currentState[accessory.no]).name }}</span>
        </button>
        <div v-if="!loaded" class="empty-board">請選取設備類別或設備編號，載入其掛載配件。</div>
        <div v-else-if="visibleAccessories.length === 0" class="empty-board">目前篩選條件沒有掛載配件假資料。</div>
      </section>

      <section class="action-panel" aria-label="Accessory Action">
        <div class="action-heading"><h2>Accessory Action</h2><span v-if="selectedAccessory">{{ selectedAccessory.no }} · {{ selectedAccessory.category }} · 目前 {{ stateFor(currentState[selectedAccessory.no]).name }}</span><span v-else>選取配件後，依所屬分類執行狀態操作</span></div>
        <div class="state-buttons">
          <button v-for="state in stateButtons" :key="state.code" type="button" :disabled="!selectedAccessory || Boolean(forbiddenReason(selectedAccessory, state.code))" :title="selectedAccessory ? (forbiddenReason(selectedAccessory, state.code) || actionHelp[state.code]) : '請先選取配件'" @click="requestStateChange(state.code)">
            <i :style="{ backgroundColor: state.color }"></i><span>{{ state.action }}</span><small>{{ state.name }}</small>
          </button>
        </div>
      </section>
      <footer class="monitor-status"><span>{{ statusMessage }}</span><span>Refresh {{ refreshCount }}</span></footer>
      <button type="button" class="close-button" @click="close">Close</button>
    </section>

    <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @click.stop>
      <button type="button" @click="openDetail('accessory-log', contextMenu.accessoryNo)">Accessory State Log</button>
      <button type="button" @click="openDetail('equipment-log', contextMenu.accessoryNo)">Equipment ACC State Log</button>
      <button type="button" @click="openDetail('current-state', contextMenu.accessoryNo)">Accessory State</button>
    </div>

    <section v-if="detailMode && detailAccessory" class="dialog-backdrop" @click.self="detailMode = null">
      <article class="detail-dialog" role="dialog" aria-modal="true" :aria-label="detailTitle">
        <header><div><div class="eyebrow">{{ detailTitle }} · DEV preview</div><h2>{{ detailAccessory.no }}</h2></div><button type="button" class="close-icon" aria-label="關閉" @click="detailMode = null">×</button></header>
        <dl><dt>Equipment Type</dt><dd>{{ detailAccessory.equipmentType }}</dd><dt>Equipment No</dt><dd>{{ detailAccessory.equipmentNo }}</dd><dt>Accessory Category</dt><dd>{{ detailAccessory.category }}</dd><dt>Accessory State</dt><dd>{{ stateFor(currentState[detailAccessory.no]).name }}</dd></dl>
        <p>來源右鍵／雙擊會開啟外部查詢 Form；此處只顯示本頁假資料。</p>
        <footer><button type="button" class="primary-button" @click="detailMode = null">Close</button></footer>
      </article>
    </section>

    <section v-if="pendingState !== null && selectedAccessory" class="dialog-backdrop" @click.self="pendingState = null">
      <article class="detail-dialog" role="dialog" aria-modal="true" aria-label="DEV 配件狀態變更">
        <header><div><div class="eyebrow">DEV · Accessory State Change</div><h2>{{ selectedAccessory.no }}</h2></div><button type="button" class="close-icon" aria-label="取消" @click="pendingState = null">×</button></header>
        <p>狀態：{{ stateFor(currentState[selectedAccessory.no]).name }} → {{ stateFor(pendingState).name }}</p>
        <p class="dialog-note">{{ actionHelp[pendingState] }}</p>
        <label class="reason-label">狀態變更說明<textarea v-model="description" rows="3" maxlength="255" placeholder="DEV 假資料：輸入變更說明"></textarea></label>
        <p class="dialog-warning">不執行 ES301／ES302 實際掛載交易、壽命計算、權限及正式互斥查核。</p>
        <footer><button type="button" class="plain-button" @click="pendingState = null">Cancel</button><button type="button" class="primary-button" @click="applyStateChange">Apply to DEV fixture</button></footer>
      </article>
    </section>

    <div v-if="messageText" class="message-backdrop" @click.self="messageText = ''"><div class="message-box" role="alert"><p>{{ messageText }}</p><button type="button" class="primary-button" @click="messageText = ''">OK</button></div></div>
  </section>
</template>

<style scoped>
.accessory-equipment-monitor { height: 100%; min-height: 0; overflow: auto; box-sizing: border-box; padding: 14px; background: #f1f0eb; color: #24251f; font: 13px/1.45 "Segoe UI", "Microsoft JhengHei", sans-serif; }
.page-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; max-width: 1320px; margin: 0 auto 12px; }
.eyebrow { color: #777669; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }
.page-header h1 { margin: 1px 0 0; font-size: 22px; line-height: 1.25; }
.page-header h1 span { margin-left: 8px; color: #858376; font-size: 11px; font-weight: 500; }
.close-icon { border: 0; background: transparent; color: #605f56; font-size: 24px; line-height: 1; cursor: pointer; }
.fixture-banner { display: flex; gap: 10px; align-items: center; max-width: 1320px; box-sizing: border-box; margin: 0 auto 10px; padding: 8px 11px; border: 1px solid #e3d19c; border-radius: 4px; background: #fff8de; color: #685b2e; }
.fixture-banner strong { white-space: nowrap; }
.monitor-window { position: relative; max-width: 1320px; min-height: 430px; box-sizing: border-box; margin: auto; padding: 12px; border: 1px solid #b9b7ac; border-radius: 5px; background: #fbfaf6; box-shadow: 0 2px 9px #2221; }
.toolbar { display: flex; align-items: end; flex-wrap: wrap; gap: 10px; padding-bottom: 10px; border-bottom: 1px solid #deddd5; }
.toolbar > label { display: grid; gap: 3px; color: #66655c; font-size: 11px; }
.toolbar select { min-width: 205px; height: 31px; box-sizing: border-box; padding: 4px 8px; border: 1px solid #aaa89e; border-radius: 3px; background: #fff; color: #25251f; font: inherit; font-size: 12px; }
.page-controls { display: flex; align-items: center; gap: 6px; color: #6b6a61; font-size: 11px; }
.page-controls > span, .page-controls label { white-space: nowrap; }
.page-controls strong { color: #282821; }
.page-controls button { width: 26px; height: 28px; border: 1px solid #bcbab0; border-radius: 3px; background: #f5f4ef; color: #333; font-size: 18px; cursor: pointer; }
.page-controls button:disabled { color: #aaa89e; cursor: default; }
.page-controls label { display: flex; align-items: center; gap: 4px; }
.page-controls input { width: 48px; height: 27px; box-sizing: border-box; border: 1px solid #bcbab0; border-radius: 3px; text-align: center; font: inherit; }
.plain-button, .primary-button, .close-button { min-height: 29px; padding: 4px 11px; border: 1px solid #aaa89e; border-radius: 3px; background: #f7f6f1; color: #282821; font: inherit; cursor: pointer; }
.plain-button:disabled { opacity: .5; cursor: default; }
.primary-button { border-color: #4a4a40; background: #48483e; color: white; }
.monitor-meta { display: flex; flex-wrap: wrap; gap: 18px; padding: 8px 2px; color: #6d6b61; font-size: 11px; }
.accessory-board { display: grid; grid-template-columns: repeat(5, minmax(125px, 1fr)); gap: 8px; min-height: 214px; padding: 8px; border: 1px solid #d2d0c7; border-radius: 4px; background: #f2f1eb; }
.accessory-card { display: flex; min-width: 0; min-height: 70px; flex-direction: column; gap: 3px; padding: 7px; border: 1px solid #d0cec3; border-radius: 4px; background: white; color: #35352e; cursor: pointer; text-align: left; }
.accessory-card:hover { border-color: #8f8d80; }
.accessory-card.selected { outline: 2px solid #4d5f73; outline-offset: 1px; }
.card-topline { display: flex; align-items: center; gap: 6px; min-width: 0; }
.card-topline i { width: 10px; height: 10px; flex: 0 0 auto; border-radius: 50%; }
.card-topline strong { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.state-name { color: #55544b; font-size: 11px; }
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
@media (max-width: 1050px) { .accessory-board { grid-template-columns: repeat(4, minmax(135px, 1fr)); } .toolbar select { min-width: 165px; } }
@media (max-width: 800px) { .accessory-board { grid-template-columns: repeat(3, minmax(135px, 1fr)); } }
@media (max-width: 650px) { .page-header h1 span { display: block; margin: 3px 0 0; } .toolbar { align-items: stretch; } .toolbar > label { width: 100%; } .toolbar select { width: 100%; } .accessory-board { grid-template-columns: repeat(2, minmax(140px, 1fr)); } .monitor-status { flex-direction: column; } }
</style>
