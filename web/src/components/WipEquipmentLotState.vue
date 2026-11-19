<script setup lang="ts">
import { computed, ref } from 'vue'

type EquipmentState = '待機' | '生產中' | '停機' | '維修'
type Action =
  | 'available-lots' | 'reservation' | 'loadport-state' | 'lot-info' | 'lot-log' | 'equipment-log'
  | 'hold-equipment' | 'check-in' | 'check-out' | 'carrier-in' | 'carrier-out' | 'hold-lots'
  | 'change-state' | 'qc-list' | 'general-check-in-out' | 'explain'

interface LoadPort { portNo: number; lotNo: string; held: boolean }
interface Equipment {
  equipmentNo: string
  equipmentType: string
  equipmentName: string
  equipmentGroupNo: string
  equipmentClass: string
  vendorNo: string
  modelNo: string
  assetNo: string
  pageNo: string
  state: EquipmentState
  stateColor: string
  autoFlag: 0 | 1 | null
  loadPortCount: number
  availableLotCount: number
  ports: LoadPort[]
}
interface BoardRecord { time: string; equipmentNo: string; lotNo: string; action: string; userNo: string }
interface Snapshot { version: 1; equipment: Equipment[]; records: BoardRecord[] }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp204-equipment-lot-state.v1'
const palette: Record<EquipmentState, string> = { 待機: '#b8d7eb', 生產中: '#b8dcb1', 停機: '#edb1a8', 維修: '#f2d997' }
const actionNames: Record<Action, string> = {
  'available-lots': 'Available Lot', reservation: 'LOTRESERVATION', 'loadport-state': 'LoadPort State',
  'lot-info': 'Lot Info', 'lot-log': 'Lot Log', 'equipment-log': 'EQP Log',
  'hold-equipment': 'Hold Lot by EQP', 'check-in': 'Check In', 'check-out': 'Check Out',
  'carrier-in': 'Carrier Check In', 'carrier-out': 'Carrier Check Out', 'hold-lots': 'Hold Lots',
  'change-state': 'Eqp Change State', 'qc-list': 'QC List',
  'general-check-in-out': 'General Check In/Out', explain: '設備批次執行說明',
}

function seedEquipment(): Equipment[] {
  const states: EquipmentState[] = ['待機', '生產中', '停機', '維修']
  return Array.from({ length: 30 }, (_, index) => {
    const number = index + 1
    const loadPortCount = 4 + (index % 5)
    const ports = Array.from({ length: loadPortCount }, (_, portIndex) => ({
      portNo: portIndex + 1,
      lotNo: (portIndex + index) % 4 === 0
        ? 'DEV-LOT-' + String(number).padStart(2, '0') + '-' + String(portIndex + 1).padStart(2, '0')
        : '',
      held: false,
    }))
    const state = states[index % states.length]
    return {
      equipmentNo: 'EQP-' + String(number).padStart(2, '0'),
      equipmentType: index % 2 ? 'DEV-TYPE-ASSY' : 'DEV-TYPE-PACK',
      equipmentName: (index % 2 ? '封裝設備 ' : '測試設備 ') + String(number).padStart(2, '0'),
      equipmentGroupNo: 'DEV-GRP-' + String(Math.ceil(number / 5)).padStart(2, '0'),
      equipmentClass: index % 3 ? '一般設備' : '自動設備',
      vendorNo: index % 2 ? 'DEV-VENDOR-A' : 'DEV-VENDOR-B',
      modelNo: 'MODEL-' + String(number).padStart(2, '0'),
      assetNo: 'ASSET-' + String(number).padStart(5, '0'),
      pageNo: String(Math.ceil(number / 15)),
      state,
      stateColor: palette[state],
      autoFlag: index % 7 === 0 ? null : index % 2 as 0 | 1,
      loadPortCount,
      availableLotCount: (index * 3 + 2) % 9,
      ports,
    }
  })
}

function readSnapshot(): Snapshot {
  const initial: Snapshot = { version: 1, equipment: seedEquipment(), records: [] }
  if (!dev.value) return initial
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) || 'null') as Partial<Snapshot> | null
    if (stored?.version === 1 && Array.isArray(stored.equipment) && Array.isArray(stored.records))
      return { version: 1, equipment: stored.equipment as Equipment[], records: stored.records as BoardRecord[] }
  } catch { /* invalid DEV snapshot uses the seed data */ }
  return initial
}

const initial = readSnapshot()
const equipmentRows = ref(initial.equipment)
const records = ref(initial.records)
const currentPageNo = ref('1')
const selectedEquipmentNo = ref('')
const loadAvailableLots = ref(false)
const recommendation = ref(false)
const actionError = ref('')
const actionMessage = ref('')
const dialogKind = ref<Action | null>(null)
const lotNoInput = ref('')
const nextState = ref<EquipmentState>('待機')
const contextMenu = ref<{ x: number; y: number } | null>(null)
const dialogTitle = computed(() => dialogKind.value ? actionNames[dialogKind.value] : '')
const pages = computed(() => [...new Set(equipmentRows.value.map(row => row.pageNo))].sort((a, b) => Number(a) - Number(b)))
const pageEquipment = computed(() => equipmentRows.value.filter(row => row.pageNo === currentPageNo.value).slice(0, 15))
const selectedEquipment = computed(() => equipmentRows.value.find(row => row.equipmentNo === selectedEquipmentNo.value) ?? null)
const occupiedPorts = computed(() => selectedEquipment.value?.ports.filter(port => port.lotNo) ?? [])
const allLots = computed(() => [
  { lotNo: 'DEV-WIP-LOT-001', opNo: 'DEV-OP-01', productNo: 'DEV-PRD-001', status: 'Queue' },
  { lotNo: 'DEV-WIP-LOT-002', opNo: 'DEV-OP-02', productNo: 'DEV-PRD-002', status: 'Queue' },
  { lotNo: 'DEV-WIP-LOT-003', opNo: 'DEV-OP-03', productNo: 'DEV-PRD-003', status: 'Running' },
  { lotNo: 'DEV-WIP-LOT-004', opNo: 'DEV-OP-01', productNo: 'DEV-PRD-001', status: 'Queue' },
  { lotNo: 'DEV-WIP-LOT-005', opNo: 'DEV-OP-04', productNo: 'DEV-PRD-004', status: 'Queue' },
])
const lotLogRows = computed(() => records.value.filter(record => record.equipmentNo === selectedEquipmentNo.value))
const equipmentLogRows = computed(() => records.value.filter(record => record.equipmentNo === selectedEquipmentNo.value))

function persist(): void {
  if (dev.value) localStorage.setItem(storageKey, JSON.stringify({ version: 1, equipment: equipmentRows.value, records: records.value } satisfies Snapshot))
}
function selectEquipment(row: Equipment): void {
  selectedEquipmentNo.value = row.equipmentNo
  actionError.value = ''
  actionMessage.value = ''
  contextMenu.value = null
}
function showContext(event: MouseEvent, row: Equipment): void {
  selectEquipment(row)
  contextMenu.value = { x: Math.min(event.clientX, window.innerWidth - 210), y: Math.min(event.clientY, window.innerHeight - 150) }
}
function openAction(action: Action, equipmentNo?: string): void {
  contextMenu.value = null
  actionError.value = ''
  actionMessage.value = ''
  if (equipmentNo) selectedEquipmentNo.value = equipmentNo
  if (action === 'general-check-in-out' && !selectedEquipment.value)
    selectedEquipmentNo.value = equipmentRows.value[0]?.equipmentNo ?? ''
  const needsEquipment = !['hold-lots', 'general-check-in-out', 'explain'].includes(action)
  if (needsEquipment && !selectedEquipment.value) { actionError.value = '請先選取一台設備。'; return }
  dialogKind.value = action
  lotNoInput.value = action === 'check-out' || action === 'carrier-out'
    ? occupiedPorts.value[0]?.lotNo ?? ''
    : action === 'check-in' || action === 'carrier-in' || action === 'general-check-in-out'
      ? allLots.value[0].lotNo
      : ''
  if (selectedEquipment.value) nextState.value = selectedEquipment.value.state
}
function log(action: string, lotNo = '', equipmentNo = selectedEquipment.value?.equipmentNo ?? 'ALL'): void {
  records.value.unshift({
    time: new Date().toISOString(),
    equipmentNo,
    lotNo,
    action,
    userNo: props.currentUserNo || 'DEV-USER',
  })
  records.value = records.value.slice(0, 100)
}
function executeDialog(): void {
  const row = selectedEquipment.value
  const action = dialogKind.value
  if (!action) return
  if (action === 'check-in' || action === 'carrier-in' || action === 'general-check-in-out') {
    if (!row) { actionError.value = '請先選取設備。'; return }
    const lotNo = lotNoInput.value.trim().toUpperCase()
    if (!lotNo) { actionError.value = 'Lot No 不可空白。'; return }
    const emptyPort = row.ports.find(port => !port.lotNo)
    if (!emptyPort) { actionError.value = row.equipmentNo + ' 沒有空的 LoadPort。'; return }
    emptyPort.lotNo = lotNo
    emptyPort.held = false
    row.state = '生產中'
    row.stateColor = palette[row.state]
    row.availableLotCount = Math.max(0, row.availableLotCount - 1)
    log(actionNames[action], lotNo)
    actionMessage.value = 'DEV 已將 ' + lotNo + ' 配置至 ' + row.equipmentNo + ' LoadPort ' + emptyPort.portNo + '。'
  } else if (action === 'check-out' || action === 'carrier-out') {
    if (!row) { actionError.value = '請先選取設備。'; return }
    const lotNo = lotNoInput.value.trim().toUpperCase()
    const port = row.ports.find(item => item.lotNo === lotNo)
    if (!port) { actionError.value = '此設備找不到指定 Lot No。'; return }
    port.lotNo = ''
    port.held = false
    row.state = row.ports.some(item => item.lotNo) ? '生產中' : '待機'
    row.stateColor = palette[row.state]
    row.availableLotCount += 1
    log(actionNames[action], lotNo)
    actionMessage.value = 'DEV 已將 ' + lotNo + ' 從 ' + row.equipmentNo + ' 執行 Check Out。'
  } else if (action === 'hold-equipment' || action === 'hold-lots') {
    const targets = action === 'hold-lots' ? equipmentRows.value : row ? [row] : []
    const held = targets.flatMap(item => item.ports.filter(port => port.lotNo && !port.held))
    held.forEach(port => { port.held = true })
    targets.forEach(item => log(actionNames[action], item.ports.find(port => port.held)?.lotNo ?? '', item.equipmentNo))
    actionMessage.value = held.length ? 'DEV 已標記 ' + held.length + ' 筆設備批次為 Hold。' : '目前沒有可設定 Hold 的設備批次。'
  } else if (action === 'change-state') {
    if (!row) { actionError.value = '請先選取設備。'; return }
    row.state = nextState.value
    row.stateColor = palette[nextState.value]
    log(actionNames[action])
    actionMessage.value = row.equipmentNo + ' 狀態已在 DEV 假資料更新為「' + nextState.value + '」。'
  } else {
    dialogKind.value = null
    return
  }
  persist()
  if (!actionError.value) dialogKind.value = null
}
function closeDialog(): void { dialogKind.value = null; actionError.value = '' }
function cardClass(row: Equipment): string[] {
  return [row.equipmentNo === selectedEquipmentNo.value ? 'selected' : '', row.autoFlag === 1 ? 'auto' : row.autoFlag === 0 ? 'manual' : 'unspecified'].filter(Boolean)
}
function stateStyle(row: Equipment): Record<string, string> { return { backgroundColor: row.stateColor } }
function formattedTime(value: string): string { return new Date(value).toLocaleString() }
function announceRefresh(): void { actionError.value = ''; actionMessage.value = '已重新整理目前 DEV 設備頁面。' }
</script>

<template>
  <section v-if="dev" class="equipment-lot-state-page">
    <header class="page-title">
      <div><small>在製品管理 / 在製品 / 生產報工</small><h2>生產批執行_設備 <span>DEV 假資料</span></h2></div>
      <div class="user-stamp">使用者：{{ currentUserNo || 'DEV-USER' }}</div>
    </header>
    <div class="work-area">
      <aside class="favorite-pages" aria-label="設備最愛頁面">
        <header>設備最愛</header>
        <button v-for="page in pages" :key="page" type="button" :class="{ active: currentPageNo === page }" @click="currentPageNo = page; selectedEquipmentNo = ''; actionError = ''">Page {{ page }} <small>{{ equipmentRows.filter(row => row.pageNo === page).length }} 台</small></button>
        <p>每頁最多顯示 15 台設備</p>
      </aside>
      <main class="board-content">
        <div class="toolbar">
          <div class="toolbar-actions">
            <button type="button" @click="openAction(recommendation ? 'reservation' : 'available-lots')">{{ recommendation ? 'LOTRESERVATION' : 'Availd Lot' }}</button>
            <button type="button" @click="openAction('loadport-state')">LoadPort State</button>
            <button type="button" @click="openAction('lot-info')">Lot Info</button>
            <button type="button" @click="openAction('lot-log')">Lot Log</button>
            <button type="button" @click="openAction('equipment-log')">EQP Log</button>
            <label class="toolbar-check"><input v-model="recommendation" type="checkbox"> Recommand</label>
            <label class="toolbar-check"><input v-model="loadAvailableLots" type="checkbox"> Load Available Lots</label>
            <button type="button" @click="openAction('hold-equipment')">Hold Lot by EQP</button>
            <button class="help-button" type="button" aria-label="設備批次執行說明" @click="openAction('explain')">?</button>
            <button type="button" @click="announceRefresh">Refresh</button>
          </div>
          <div class="page-pager"><label>Page <select v-model="currentPageNo" @change="selectedEquipmentNo = ''"><option v-for="page in pages" :key="page" :value="page">{{ page }}</option></select></label><span>/ {{ pages.length }}</span></div>
        </div>
        <section class="equipment-board" aria-label="設備看板">
          <button v-for="row in pageEquipment" :key="row.equipmentNo" type="button" class="equipment-card" :class="cardClass(row)" @click="selectEquipment(row)" @contextmenu.prevent="showContext($event, row)">
            <header :style="stateStyle(row)"><b>{{ row.equipmentNo }}</b><span>{{ row.state }}</span></header>
            <div class="equipment-name">{{ row.equipmentName }}</div>
            <div class="equipment-meta"><span>可用批次</span><b>{{ loadAvailableLots ? row.availableLotCount : 'N/A' }}</b></div>
            <div class="equipment-meta"><span>LoadPort</span><b>{{ row.ports.filter(port => port.lotNo).length }} / {{ row.loadPortCount }}</b></div>
            <div class="port-chips"><span v-for="port in row.ports.slice(0, 8)" :key="port.portNo" :class="{ occupied: port.lotNo, held: port.held }" :title="port.lotNo || '空 LoadPort'">{{ port.portNo }}</span></div>
          </button>
          <div v-if="!pageEquipment.length" class="empty-board">此最愛頁面沒有設備。</div>
        </section>
        <div class="board-footer"><span>設備 {{ pageEquipment.length }} 台 · 選取：{{ selectedEquipmentNo || '未選取' }}</span><span class="legend"><i class="auto-key"></i>Auto <i class="manual-key"></i>Manual <i class="port-key"></i>LoadPort 有批</span></div>
        <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p><p v-else-if="actionMessage" class="feedback" role="status">{{ actionMessage }}</p>
        <div class="bottom-actions">
          <button class="primary" type="button" @click="openAction('check-in')">Check In</button><button type="button" @click="openAction('check-out')">Check Out</button>
          <button type="button" @click="openAction('hold-lots')">Hold Lots</button><button type="button" @click="openAction('change-state')">Eqp Change State</button>
          <button type="button" @click="openAction('carrier-in')">Carrier Check In</button><button type="button" @click="openAction('carrier-out')">Carrier Check Out</button>
          <button type="button" @click="openAction('qc-list')">QC List</button><button type="button" @click="openAction('general-check-in-out')">General Check In/Out</button>
        </div>
      </main>
    </div>
    <footer class="page-footer"><span>資料來源：本機 DEV 假資料</span><button type="button" @click="emit('close')">Close</button></footer>

    <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @mouseleave="contextMenu = null">
      <button type="button" @click="openAction('loadport-state')">LoadPort State</button><button type="button" @click="openAction(recommendation ? 'reservation' : 'available-lots')">Available Lot</button><button type="button" @click="openAction('equipment-log')">Equipment State Log</button>
    </div>

    <div v-if="dialogKind" class="dialog-overlay" @click.self="closeDialog">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="dialogTitle">
        <header><h3>{{ dialogTitle }}</h3><button type="button" aria-label="關閉" @click="closeDialog">×</button></header>
        <template v-if="['check-in','check-out','carrier-in','carrier-out','general-check-in-out'].includes(dialogKind)">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo || '通用報工' }} · {{ actionNames[dialogKind] }}。此畫面只模擬本機 DEV 狀態。</p>
          <label v-if="dialogKind === 'general-check-in-out'" class="dialog-field">設備編號<select v-model="selectedEquipmentNo"><option v-for="equipment in equipmentRows" :key="equipment.equipmentNo" :value="equipment.equipmentNo">{{ equipment.equipmentNo }} · {{ equipment.equipmentName }}</option></select></label>
          <label class="dialog-field">Lot No<input v-model.trim="lotNoInput" list="dev-wip-lots" @keydown.enter.prevent="executeDialog"><datalist id="dev-wip-lots"><option v-for="lot in allLots" :key="lot.lotNo" :value="lot.lotNo"></option></datalist></label>
          <div v-if="dialogKind === 'check-out' || dialogKind === 'carrier-out'" class="selection-list"><button v-for="port in occupiedPorts" :key="port.portNo" type="button" @click="lotNoInput = port.lotNo">Port {{ port.portNo }} · {{ port.lotNo }}</button><p v-if="!occupiedPorts.length">此設備目前沒有批次。</p></div>
        </template>
        <template v-else-if="dialogKind === 'change-state'">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo }} · 狀態變更會即時套用到 DEV 看板。</p><label class="dialog-field">設備狀態<select v-model="nextState"><option>待機</option><option>生產中</option><option>停機</option><option>維修</option></select></label>
        </template>
        <template v-else-if="dialogKind === 'loadport-state'">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo }} · {{ selectedEquipment?.equipmentName }}</p>
          <div class="dialog-table"><table><thead><tr><th>LoadPort</th><th>狀態</th><th>Lot No</th></tr></thead><tbody><tr v-for="port in selectedEquipment?.ports" :key="port.portNo"><td>{{ port.portNo }}</td><td>{{ port.held ? 'Hold' : port.lotNo ? 'Occupied' : 'Idle' }}</td><td>{{ port.lotNo || '—' }}</td></tr></tbody></table></div>
        </template>
        <template v-else-if="dialogKind === 'available-lots' || dialogKind === 'reservation'">
          <p class="dialog-description">{{ recommendation ? '依建議設備與目前製程顯示候選批次。' : '可派至選取設備的 DEV 假資料。' }} {{ selectedEquipment?.equipmentNo }}</p>
          <div class="dialog-table"><table><thead><tr><th>Lot No</th><th>OP No</th><th>Product No</th><th>狀態</th></tr></thead><tbody><tr v-for="lot in allLots" :key="lot.lotNo"><td>{{ lot.lotNo }}</td><td>{{ lot.opNo }}</td><td>{{ lot.productNo }}</td><td>{{ lot.status }}</td></tr></tbody></table></div>
        </template>
        <template v-else-if="dialogKind === 'lot-info'">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo }} 上的批次資料</p><div class="dialog-table"><table><thead><tr><th>LoadPort</th><th>Lot No</th><th>狀態</th><th>Hold</th></tr></thead><tbody><tr v-for="port in occupiedPorts" :key="port.portNo"><td>{{ port.portNo }}</td><td>{{ port.lotNo }}</td><td>Running</td><td>{{ port.held ? '是' : '否' }}</td></tr><tr v-if="!occupiedPorts.length"><td colspan="4">目前沒有批次。</td></tr></tbody></table></div>
        </template>
        <template v-else-if="dialogKind === 'lot-log' || dialogKind === 'equipment-log'">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo }} 操作紀錄（DEV）</p><div class="dialog-table"><table><thead><tr><th>時間</th><th>設備</th><th>Lot No</th><th>動作</th><th>使用者</th></tr></thead><tbody><tr v-for="(record,index) in lotLogRows" :key="record.time + index"><td>{{ formattedTime(record.time) }}</td><td>{{ record.equipmentNo }}</td><td>{{ record.lotNo || '—' }}</td><td>{{ record.action }}</td><td>{{ record.userNo }}</td></tr><tr v-if="!equipmentLogRows.length"><td colspan="5">目前沒有 DEV 紀錄。</td></tr></tbody></table></div>
        </template>
        <template v-else-if="dialogKind === 'hold-equipment' || dialogKind === 'hold-lots'">
          <p class="dialog-description">{{ dialogKind === 'hold-lots' ? '將目前設備看板上有批次的 LoadPort 標記為 Hold。' : '將 ' + selectedEquipment?.equipmentNo + ' 的在機批次標記為 Hold。' }} 此動作只改變 DEV 假資料。</p>
          <div class="selection-list"><p v-if="dialogKind === 'hold-lots'">看板上有批次的設備：{{ equipmentRows.filter(row => row.ports.some(port => port.lotNo)).length }} 台</p><p v-else-if="!occupiedPorts.length">此設備沒有可 Hold 的批次。</p><p v-for="port in occupiedPorts" :key="port.portNo">Port {{ port.portNo }} · {{ port.lotNo }}<span v-if="port.held">（已 Hold）</span></p></div>
        </template>
        <template v-else-if="dialogKind === 'qc-list'">
          <p class="dialog-description">{{ selectedEquipment?.equipmentNo }} · 設備點檢表</p><div class="dialog-table"><table><thead><tr><th>項目</th><th>規格</th><th>最近結果</th></tr></thead><tbody><tr><td>安全門</td><td>正常</td><td>Pass</td></tr><tr><td>氣壓</td><td>0.40–0.60 MPa</td><td>0.52 MPa</td></tr><tr><td>溫度</td><td>20–30 °C</td><td>25.4 °C</td></tr></tbody></table></div>
        </template>
        <template v-else><p class="dialog-description">WinForms 設備批次執行包含設備最愛頁、LoadPort、批次推薦、Check In/Out、Carrier、Hold、設備狀態、QC 及操作紀錄。DEV 頁只示範畫面與基本狀態變化；正式執行仍需後端重驗 Lot、Equipment、LoadPort、權限、Business Rule 及 WIP 交易。</p></template>
        <p v-if="actionError" class="feedback error" role="alert">{{ actionError }}</p>
        <footer><button v-if="['check-in','check-out','carrier-in','carrier-out','general-check-in-out','change-state','hold-equipment','hold-lots'].includes(dialogKind)" class="primary" type="button" @click="executeDialog">Execute</button><button type="button" @click="closeDialog">Close</button></footer>
      </section>
    </div>
  </section>
  <p v-else role="status">此功能目前僅提供 DEV 假資料預覽。</p>
</template>

<style scoped>
.equipment-lot-state-page{height:100%;min-height:540px;display:flex;flex-direction:column;gap:7px;padding:9px;color:#292929;background:#fff;font:12px "Microsoft JhengHei",Arial,sans-serif;box-sizing:border-box}.page-title{display:flex;align-items:center;justify-content:space-between;min-height:43px;padding:3px 9px;border-bottom:1px solid #d8d4cc;background:#f7f5ef}.page-title small{color:#777;font-size:11px}.page-title h2{margin:2px 0 0;font-size:19px;font-weight:500}.page-title h2 span{margin-left:6px;padding:2px 7px;background:#fff0b2;color:#785d00;font-size:11px;vertical-align:middle}.user-stamp{color:#686868}.work-area{flex:1;min-height:0;display:grid;grid-template-columns:154px minmax(0,1fr);gap:8px}.favorite-pages{display:flex;flex-direction:column;gap:5px;min-height:0;padding:7px;border:1px solid #c8c8c8;background:#f7f7f7}.favorite-pages header{padding:4px 5px;border-bottom:1px solid #ccc;font-size:14px;font-weight:600}.favorite-pages button{display:flex;justify-content:space-between;align-items:center;min-height:33px;text-align:left}.favorite-pages button.active{border-color:#aaa;background:#dedede;font-weight:600}.favorite-pages button small{color:#777;font-size:10px}.favorite-pages p{margin:auto 4px 0;color:#777;font-size:11px}.board-content{display:flex;min-width:0;min-height:0;flex-direction:column;gap:7px}.toolbar{display:flex;justify-content:space-between;align-items:center;gap:8px;min-height:48px;padding:5px 7px;border:1px solid #ccc;background:#fbfbfb}.toolbar-actions{display:flex;align-items:center;flex-wrap:wrap;gap:4px}.toolbar button,.bottom-actions button,.page-footer button{min-height:27px;padding:3px 8px;border:1px solid #b8b8b8;border-radius:3px;background:#f6f6f6;color:inherit;font:inherit;white-space:nowrap;cursor:pointer}.toolbar button:hover,.bottom-actions button:hover,.page-footer button:hover{background:#fff1c7}.toolbar-check{display:inline-flex;align-items:center;gap:3px;margin:0 2px;white-space:nowrap}.toolbar-check input{accent-color:#356fa8}.help-button{width:27px;padding:0!important;font-weight:700!important}.page-pager{display:flex;align-items:center;gap:4px;white-space:nowrap}.page-pager select{height:26px;border:1px solid #aaa;background:#fff;padding:2px 5px}.equipment-board{flex:1;min-height:0;display:grid;grid-template-columns:repeat(5,minmax(125px,1fr));grid-template-rows:repeat(3,minmax(105px,1fr));gap:7px;overflow:auto;padding:7px;border:1px solid #c9c9c9;background:#fff}.equipment-card{display:flex;min-width:0;flex-direction:column;gap:4px;padding:5px;border:2px solid #999;background:#fafafa;color:#282828;text-align:left;cursor:pointer}.equipment-card.auto{border-color:#3973b2}.equipment-card.manual{border-color:#c4564c}.equipment-card.unspecified{border-style:dotted;border-color:#777}.equipment-card.selected{outline:2px solid #e9a323;outline-offset:1px;background:#fff8df}.equipment-card:hover{filter:brightness(.98)}.equipment-card>header{display:flex;justify-content:space-between;gap:5px;margin:-1px -1px 0;padding:4px 5px;border:1px solid #aaa;font-size:12px}.equipment-card>header span{font-weight:600}.equipment-name{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:11px}.equipment-meta{display:flex;justify-content:space-between;color:#666;font-size:10px}.equipment-meta b{color:#333;font-size:11px}.port-chips{display:flex;flex-wrap:wrap;gap:3px;margin-top:auto}.port-chips span{display:grid;min-width:21px;height:18px;place-items:center;border:1px solid #888;background:#ddd;color:#555;font-size:10px}.port-chips span.occupied{border-color:#af4a43;background:#d96b60;color:#fff}.port-chips span.held{outline:2px solid #e6b12d;outline-offset:0}.empty-board{grid-column:1/-1;align-self:center;justify-self:center;color:#777}.board-footer{display:flex;justify-content:space-between;align-items:center;min-height:20px;padding:0 4px;color:#666}.legend{display:flex;align-items:center;gap:4px}.legend i{display:inline-block;width:11px;height:11px;border:2px solid #3973b2}.legend .manual-key{border-color:#c4564c}.legend .port-key{border:1px solid #af4a43;background:#d96b60}.feedback{margin:0;padding:5px 8px;border:1px solid #b4c6aa;background:#eff6eb;color:#3b5934}.feedback.error{border-color:#cf958b;background:#fff0ed;color:#982d21}.bottom-actions{display:flex;justify-content:flex-end;flex-wrap:wrap;gap:5px;padding:5px 2px;border-top:1px solid #ccc}.primary{border-color:#eb9b24!important;background:#ffad32!important;color:#fff!important;font-weight:600!important}.page-footer{display:flex;justify-content:space-between;align-items:center;min-height:30px;padding:0 3px;color:#777}.context-menu{position:fixed;z-index:170;display:grid;min-width:190px;padding:4px;border:1px solid #999;background:#fff;box-shadow:0 3px 12px #0003}.context-menu button{padding:7px 9px;border:0;background:#fff;text-align:left;font:inherit;cursor:pointer}.context-menu button:hover{background:#fff0bd}.dialog-overlay{position:fixed;inset:0;z-index:180;display:grid;place-items:center;padding:16px;background:#26334266}.dialog{display:flex;width:min(760px,96vw);max-height:88vh;flex-direction:column;gap:12px;overflow:auto;padding:14px;border:1px solid #89939c;background:#fff;box-shadow:0 8px 28px #1113}.dialog>header{display:flex;justify-content:space-between;align-items:center;padding-bottom:7px;border-bottom:1px solid #ccc}.dialog h3{margin:0;font-size:17px;font-weight:500}.dialog>header button{border:0;background:none;font-size:22px;cursor:pointer}.dialog-description{margin:0;color:#555}.dialog-field{display:grid;grid-template-columns:100px minmax(0,1fr);align-items:center;gap:9px}.dialog-field input,.dialog-field select{min-height:29px;padding:4px 7px;border:1px solid #999;background:#ffffc0;font:inherit}.dialog-table{max-height:48vh;overflow:auto;border:1px solid #c9c9c9}.dialog-table table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:12px}.dialog-table th,.dialog-table td{padding:6px 8px;border:1px solid #ccc;text-align:left}.dialog-table th{position:sticky;top:0;background:#e8e6df;font-weight:500}.selection-list{display:grid;gap:4px;max-height:35vh;overflow:auto;padding:7px;border:1px solid #ddd}.selection-list p{margin:2px 0}.selection-list button{padding:5px;border:1px solid #ccc;background:#f7f7f7;text-align:left;font:inherit;cursor:pointer}.dialog>footer{display:flex;justify-content:flex-end;gap:7px;margin-top:3px}.dialog>footer button{min-height:29px;padding:4px 12px;border:1px solid #b8b8b8;border-radius:3px;background:#f6f6f6;font:inherit;cursor:pointer}.dialog>footer .primary{background:#ffad32}@media(max-width:760px){.work-area{grid-template-columns:125px minmax(0,1fr)}.toolbar{align-items:flex-start;flex-direction:column}.equipment-board{grid-template-columns:repeat(3,minmax(125px,1fr));grid-template-rows:repeat(5,minmax(98px,1fr))}}@media(max-width:680px){.equipment-lot-state-page{overflow:auto}.work-area{grid-template-columns:1fr}.favorite-pages{flex-direction:row;align-items:center;flex-wrap:wrap}.favorite-pages header{width:100%}.favorite-pages p{margin:0}.equipment-board{grid-template-columns:repeat(2,minmax(120px,1fr));grid-template-rows:none;grid-auto-rows:110px}.board-footer{align-items:flex-start;flex-direction:column}.page-title{align-items:flex-start;flex-direction:column;gap:3px}}
</style>
