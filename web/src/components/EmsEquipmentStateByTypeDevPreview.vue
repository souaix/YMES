<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface EquipmentTypeOption { code: string; name: string; prefix: string; count: number }
interface EquipmentRow { equipmentNo: string; equipmentType: string; equipmentName: string; isChamber: boolean; belongEquipmentNo: string }
interface StateDefinition { code: number; name: string; color: string }
type DetailMode = 'state-log' | 'production' | 'lot-info' | null

const equipmentTypes: EquipmentTypeOption[] = [
  { code: 'ASSEMBLY', name: '組裝設備', prefix: 'ASM', count: 12 },
  { code: 'ETCH', name: '蝕刻設備', prefix: 'ETCH', count: 12 },
  { code: 'PACKER', name: '包裝設備', prefix: 'PACK', count: 5 },
  { code: 'TESTER', name: '測試設備', prefix: 'TEST', count: 8 },
].sort((left, right) => left.code.localeCompare(right.code))

const stateDefinitions: StateDefinition[] = [
  { code: 1, name: '生產', color: '#4d9e68' },
  { code: 2, name: '待機', color: '#8ba5b5' },
  { code: 3, name: '異常', color: '#d45f57' },
  { code: 4, name: '保養', color: '#d2a143' },
]
const equipmentRowsFixture: EquipmentRow[] = equipmentTypes.flatMap((type) => Array.from({ length: type.count }, (_, index) => {
  const sequence = String(index + 1).padStart(3, '0')
  return {
    equipmentNo: type.prefix + '-' + sequence,
    equipmentType: type.code,
    equipmentName: type.name + ' ' + String(index + 1).padStart(2, '0'),
    isChamber: false,
    belongEquipmentNo: '',
  }
}))
const chamberRowsFixture: EquipmentRow[] = [
  { equipmentNo: 'ETCH-001-C1', equipmentType: 'ETCH', equipmentName: '蝕刻腔體 1', isChamber: true, belongEquipmentNo: 'ETCH-001' },
  { equipmentNo: 'ETCH-001-C2', equipmentType: 'ETCH', equipmentName: '蝕刻腔體 2', isChamber: true, belongEquipmentNo: 'ETCH-001' },
]
const equipmentState = reactive<Record<string, number>>({})
equipmentRowsFixture.forEach((row, index) => { equipmentState[row.equipmentNo] = stateDefinitions[index % stateDefinitions.length].code })
chamberRowsFixture.forEach((row, index) => { equipmentState[row.equipmentNo] = index === 0 ? 1 : 2 })

const selectedType = ref('')
const monitorOpen = ref(false)
const currentPage = ref(1)
const selectedEquipmentNo = ref('')
const showChamberManage = ref(true) // DEV fixture for SysInfo.ChamberManage.
const snapshotIndex = ref(0)
const statusMessage = ref('請先選擇設備類別')
const contextMenu = ref<{ x: number; y: number; row: EquipmentRow } | null>(null)
const detailMode = ref<DetailMode>(null)
const detailEquipmentNo = ref('')
const pendingState = ref<number | null>(null)
const changeReason = ref('')
const messageText = ref('')

const selectedTypeLabel = computed(() => equipmentTypes.find((item) => item.code === selectedType.value)?.name ?? '')
const currentTypeRows = computed(() => equipmentRowsFixture.filter((row) => row.equipmentType === selectedType.value))
const totalPages = computed(() => Math.max(1, Math.ceil(currentTypeRows.value.length / 10)))
const visibleEquipmentRows = computed(() => currentTypeRows.value.slice((currentPage.value - 1) * 10, currentPage.value * 10))
const selectedEquipment = computed(() => [...equipmentRowsFixture, ...chamberRowsFixture].find((row) => row.equipmentNo === selectedEquipmentNo.value) ?? null)
const selectedEquipmentState = computed(() => selectedEquipment.value ? equipmentState[selectedEquipment.value.equipmentNo] : null)
const visibleChamberRows = computed(() => showChamberManage.value && selectedType.value === 'ETCH' && selectedEquipmentNo.value === 'ETCH-001'
  ? chamberRowsFixture.filter((row) => row.belongEquipmentNo === selectedEquipmentNo.value)
  : [])
const detailTitle = computed(() => ({
  'state-log': 'Equipment State Log', production: 'Production Data', 'lot-info': 'Lot Info',
}[detailMode.value ?? 'state-log']))
const detailRow = computed(() => [...equipmentRowsFixture, ...chamberRowsFixture].find((row) => row.equipmentNo === detailEquipmentNo.value) ?? null)

function stateFor(code: number | null | undefined): StateDefinition {
  return stateDefinitions.find((item) => item.code === code) ?? { code: 0, name: '未知狀態', color: '#d8d6cf' }
}

function openMonitor(): void {
  if (!selectedType.value) return
  currentPage.value = 1
  selectedEquipmentNo.value = ''
  contextMenu.value = null
  statusMessage.value = '已載入 ' + selectedType.value + ' 設備狀態假資料'
  monitorOpen.value = true
}

function changePage(page: number): void {
  currentPage.value = Math.min(totalPages.value, Math.max(1, page))
  selectedEquipmentNo.value = ''
  statusMessage.value = '第 ' + currentPage.value + ' 頁 / 共 ' + totalPages.value + ' 頁'
}

function onPageInput(event: Event): void {
  const target = event.target as HTMLInputElement | null
  if (target) changePage(Number(target.value))
}

function selectEquipment(row: EquipmentRow): void {
  selectedEquipmentNo.value = row.equipmentNo
  contextMenu.value = null
  statusMessage.value = 'Selected : Equipment ' + row.equipmentNo
}

function refreshStates(): void {
  snapshotIndex.value += 1
  ;[...currentTypeRows.value, ...chamberRowsFixture.filter((row) => row.equipmentType === selectedType.value)].forEach((row) => {
    equipmentState[row.equipmentNo] = (equipmentState[row.equipmentNo] % stateDefinitions.length) + 1
  })
  statusMessage.value = 'DEV 狀態資料已重新載入（Fixture ' + (snapshotIndex.value + 1) + '）'
}

function showEquipmentContext(event: MouseEvent, row: EquipmentRow): void {
  selectEquipment(row)
  contextMenu.value = { x: event.clientX, y: event.clientY, row }
}

function openDetail(mode: Exclude<DetailMode, null>, row: EquipmentRow): void {
  detailEquipmentNo.value = row.equipmentNo
  detailMode.value = mode
  contextMenu.value = null
}

function requestStateChange(code: number): void {
  if (!selectedEquipment.value) {
    messageText.value = '請先選取設備。'
    return
  }
  if (equipmentState[selectedEquipment.value.equipmentNo] === code) {
    messageText.value = '設備目前已是這個狀態。'
    return
  }
  pendingState.value = code
  changeReason.value = ''
}

function applyStateChange(): void {
  if (!selectedEquipment.value || pendingState.value === null) return
  if (!changeReason.value.trim()) {
    messageText.value = 'DEV 預覽請填寫狀態變更說明。'
    return
  }
  equipmentState[selectedEquipment.value.equipmentNo] = pendingState.value
  statusMessage.value = selectedEquipment.value.equipmentNo + ' 已在本頁 DEV 假資料改為「' + stateFor(pendingState.value).name + '」'
  pendingState.value = null
  changeReason.value = ''
}

function closeMonitor(): void {
  monitorOpen.value = false
  contextMenu.value = null
  pendingState.value = null
}
</script>

<template>
  <section v-if="dev" class="equipment-state-by-type" aria-label="設備狀態監控_設備類別 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES202 · DEV fixtures</div><h1>設備狀態監控_設備類別 <span>EQUIPMENT STATE BY TYPE</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>設備類別、現況、腔體及狀態色皆為虛構；狀態變更只存在本頁記憶體，不會寫入正式系統。</span></div>

    <section class="launcher" aria-label="設備類別選擇">
      <div class="launcher-fields">
        <label for="equipment-type-select">Equipment Type</label>
        <select id="equipment-type-select" v-model="selectedType">
          <option value="" disabled>請選擇設備類別</option>
          <option v-for="option in equipmentTypes" :key="option.code" :value="option.code">{{ option.code }}</option>
        </select>
      </div>
      <div class="launcher-hint">選取設備類別後按 Confirm，載入該類別的設備狀態監控。</div>
      <footer class="launcher-footer">
        <button type="button" class="primary-button" @click="openMonitor">Confirm</button>
        <button type="button" class="plain-button" @click="emit('close')">Close</button>
      </footer>
    </section>

    <section v-if="monitorOpen" class="dialog-backdrop">
      <article class="monitor-dialog" role="dialog" aria-modal="true" aria-label="Equipment State Monitor" @click="contextMenu = null">
        <header class="monitor-header">
          <div><div class="eyebrow">Equipment State Monitor</div><h2>Equipment Type : {{ selectedType }}</h2></div>
          <button type="button" class="close-icon" aria-label="關閉監控視窗" @click="closeMonitor">×</button>
        </header>
        <div class="monitor-toolbar">
          <div><strong>Query By</strong><span>{{ selectedTypeLabel }} · {{ selectedType }}</span></div>
          <label class="chamber-toggle"><input v-model="showChamberManage" type="checkbox" /> ChamberManage (DEV)</label>
          <div class="page-controls">
            <span>Total Page</span><strong>{{ totalPages }}</strong>
            <button type="button" aria-label="上一頁" :disabled="currentPage <= 1" @click="changePage(currentPage - 1)">‹</button>
            <span>Page</span><input :value="currentPage" type="number" min="1" :max="totalPages" aria-label="目前頁次" @change="onPageInput" />
            <button type="button" aria-label="下一頁" :disabled="currentPage >= totalPages" @click="changePage(currentPage + 1)">›</button>
          </div>
          <button type="button" class="plain-button" @click="refreshStates">Refresh</button>
        </div>
        <div class="monitor-body" :class="{ 'with-chambers': showChamberManage && selectedType === 'ETCH' }">
          <section class="equipment-board" aria-label="設備清單">
            <button v-for="row in visibleEquipmentRows" :key="row.equipmentNo" type="button" class="equipment-card" :class="{ selected: selectedEquipmentNo === row.equipmentNo }" @click="selectEquipment(row)" @contextmenu.prevent.stop="showEquipmentContext($event, row)">
              <span class="equipment-picture" aria-hidden="true">⚙</span>
              <span class="equipment-name" :style="{ backgroundColor: stateFor(equipmentState[row.equipmentNo]).color }">{{ row.equipmentNo }}</span>
              <span class="equipment-caption">{{ row.equipmentName }}</span>
              <span class="equipment-state">{{ stateFor(equipmentState[row.equipmentNo]).name }}</span>
            </button>
            <div v-if="visibleEquipmentRows.length === 0" class="empty-board">此設備類別目前沒有設備假資料。</div>
          </section>
          <aside v-if="showChamberManage && selectedType === 'ETCH'" class="chamber-board" aria-label="Chamber">
            <h3>Chamber</h3>
            <p v-if="selectedEquipmentNo !== 'ETCH-001'">選取 ETCH-001 查看其腔體。</p>
            <button v-for="row in visibleChamberRows" :key="row.equipmentNo" type="button" class="chamber-card" :class="{ selected: selectedEquipmentNo === row.equipmentNo }" @click="selectEquipment(row)" @contextmenu.prevent.stop="showEquipmentContext($event, row)">
              <strong>{{ row.equipmentNo }}</strong><span :style="{ backgroundColor: stateFor(equipmentState[row.equipmentNo]).color }">{{ stateFor(equipmentState[row.equipmentNo]).name }}</span>
            </button>
          </aside>
        </div>
        <section class="state-panel" aria-label="Equipment State">
          <div class="state-panel-heading"><h3>Equipment State</h3><span v-if="selectedEquipment">已選取 {{ selectedEquipment.equipmentNo }} · 目前 {{ stateFor(selectedEquipmentState).name }}</span><span v-else>先選取設備，再選擇狀態按鈕</span></div>
          <div class="state-buttons">
            <button v-for="state in stateDefinitions" :key="state.code" type="button" :disabled="!selectedEquipment || selectedEquipmentState === state.code" @click="requestStateChange(state.code)"><i :style="{ backgroundColor: state.color }"></i>{{ state.name }}</button>
          </div>
        </section>
        <footer class="monitor-status"><span>{{ statusMessage }}</span><span>設備 {{ currentTypeRows.length }} 筆 · 每頁 10 筆 · Fixture {{ snapshotIndex + 1 }}</span></footer>
        <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @click.stop>
          <button type="button" @click="openDetail('state-log', contextMenu.row)">Equipment State Log</button>
          <button type="button" @click="openDetail('production', contextMenu.row)">Production Data</button>
          <button type="button" @click="openDetail('lot-info', contextMenu.row)">Lot Info</button>
        </div>
      </article>
    </section>

    <section v-if="detailMode && detailRow" class="detail-backdrop" @click.self="detailMode = null">
      <article class="detail-dialog" role="dialog" aria-modal="true" :aria-label="detailTitle">
        <header><div><div class="eyebrow">{{ detailTitle }} · DEV preview</div><h2>{{ detailRow.equipmentNo }}</h2></div><button type="button" class="close-icon" aria-label="關閉" @click="detailMode = null">×</button></header>
        <dl><dt>Equipment Type</dt><dd>{{ detailRow.equipmentType }}</dd><dt>Equipment State</dt><dd>{{ stateFor(equipmentState[detailRow.equipmentNo]).name }}</dd><dt v-if="detailMode === 'production'">Lot No</dt><dd v-if="detailMode === 'production'">DEV-LOT-20261123-01</dd><dt v-if="detailMode === 'lot-info'">Current Lot</dt><dd v-if="detailMode === 'lot-info'">DEV-LOT-20261123-01</dd><dt>Operator</dt><dd>DEV-OP-01</dd><dt>Start Time</dt><dd>2026-11-23 08:00:00</dd></dl>
        <p>來源右鍵會開啟對應 MES 視窗；此處只展示本頁假資料。</p>
        <footer><button type="button" class="primary-button" @click="detailMode = null">Close</button></footer>
      </article>
    </section>

    <section v-if="pendingState !== null && selectedEquipment" class="detail-backdrop" @click.self="pendingState = null">
      <article class="detail-dialog state-change-dialog" role="dialog" aria-modal="true" aria-label="DEV 設備狀態變更">
        <header><div><div class="eyebrow">DEV · Equipment State Change</div><h2>{{ selectedEquipment.equipmentNo }}</h2></div><button type="button" class="close-icon" aria-label="取消" @click="pendingState = null">×</button></header>
        <p>狀態：{{ stateFor(selectedEquipmentState).name }} → {{ stateFor(pendingState).name }}</p>
        <label>狀態變更說明<textarea v-model="changeReason" rows="3" maxlength="255" placeholder="DEV 假資料：輸入變更說明"></textarea></label>
        <p class="detail-warning">WinForms 另有版本、保養工單、互斥狀態、腔體同步、批次與權限檢查；本預覽不執行這些正式規則。</p>
        <footer><button type="button" class="plain-button" @click="pendingState = null">Cancel</button><button type="button" class="primary-button" @click="applyStateChange">Apply to DEV fixture</button></footer>
      </article>
    </section>

    <div v-if="messageText" class="message-backdrop" @click.self="messageText = ''"><div class="message-box" role="alert"><p>{{ messageText }}</p><button type="button" class="primary-button" @click="messageText = ''">OK</button></div></div>
  </section>
</template>

<style scoped>
.equipment-state-by-type { min-height: 100%; padding: 12px; box-sizing: border-box; background: #f7f6f1; color: #292821; font: 13px Arial, sans-serif; }
.page-header,.monitor-header,.detail-dialog header { display: flex; align-items: center; justify-content: space-between; }
.page-header { max-width: 1240px; margin: 0 auto 9px; }.eyebrow { color: #777366; font-size: 11px; }.page-header h1 { margin: 3px 0 0; font-size: 21px; }.page-header h1 span { margin-left: 7px; color: #777366; font-size: 12px; font-weight: 500; }
.close-icon { width: 32px; height: 32px; border: 1px solid #aaa69a; border-radius: 3px; background: #f8f7f2; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner { display: flex; align-items: center; gap: 9px; max-width: 1240px; margin: 0 auto 9px; padding: 7px 10px; border: 1px solid #e4d69d; border-radius: 3px; background: #fff8dc; color: #62551c; font-size: 12px; }.fixture-banner strong { padding: 2px 6px; border-radius: 8px; background: #efda83; font-size: 11px; }
.launcher { position: relative; max-width: 1240px; min-height: min(64vh, 550px); margin: 0 auto; border: 1px solid #aaa69a; background: #faf9f5; }.launcher-fields { display: flex; align-items: center; gap: 8px; padding: 15px 16px; }.launcher-fields label { width: 90px; text-align: right; }.launcher-fields select { width: min(320px, 68vw); min-height: 28px; padding: 3px 7px; border: 1px solid #aaa69a; border-radius: 2px; background: white; color: #282720; font: inherit; }.launcher-hint { position: absolute; inset: 50% 10px auto; color: #888579; text-align: center; font-size: 13px; }.launcher-footer { position: absolute; right: 12px; bottom: 12px; display: flex; gap: 7px; }
.primary-button,.plain-button { min-height: 29px; padding: 4px 12px; border: 1px solid #77746b; border-radius: 3px; background: #f8f7f2; color: #292821; font: inherit; cursor: pointer; }.primary-button { border-color: #49463d; background: #49463d; color: white; }.primary-button:hover { background: #302f29; }.plain-button:hover { background: #fff2bf; }
.dialog-backdrop,.detail-backdrop,.message-backdrop { position: fixed; inset: 0; z-index: 50; display: grid; place-items: center; padding: 14px; background: rgb(20 19 16 / 42%); }.monitor-dialog { position: relative; display: flex; flex-direction: column; width: min(1120px, 96vw); height: min(690px, 92vh); padding: 9px; border: 1px solid #77746b; background: #f5f4ef; box-shadow: 0 14px 40px rgb(0 0 0 / 24%); }.monitor-header { padding: 2px 1px 7px; border-bottom: 1px solid #d0ccc1; }.monitor-header h2 { margin: 3px 0 0; font-size: 16px; }
.monitor-toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; min-height: 39px; padding: 4px 2px; border-bottom: 1px solid #d0ccc1; }.monitor-toolbar > div:first-child { display: flex; gap: 7px; }.monitor-toolbar > div:first-child strong { font-weight: 600; }.chamber-toggle { display: flex; align-items: center; gap: 4px; margin-left: auto; color: #706d62; font-size: 11px; }.page-controls { display: flex; align-items: center; gap: 5px; font-size: 11px; }.page-controls strong { min-width: 22px; text-align: center; }.page-controls button { width: 24px; height: 24px; border: 1px solid #aaa69a; border-radius: 2px; background: #faf9f5; font-size: 18px; }.page-controls button:disabled { color: #aaa69a; }.page-controls input { width: 42px; min-height: 23px; border: 1px solid #aaa69a; text-align: center; }
.monitor-body { display: grid; grid-template-columns: 1fr; gap: 8px; flex: 1; min-height: 0; padding: 8px 1px; }.monitor-body.with-chambers { grid-template-columns: minmax(0, 1fr) 205px; }.equipment-board { display: grid; grid-template-columns: repeat(5, minmax(105px, 1fr)); grid-template-rows: repeat(2, minmax(110px, 1fr)); gap: 8px; min-height: 250px; }.equipment-card { display: flex; flex-direction: column; align-items: stretch; justify-content: center; min-width: 0; padding: 5px; border: 1px solid #aaa69a; border-radius: 2px; background: #fbfaf6; cursor: pointer; }.equipment-card:hover { border-color: #70622d; background: #fffbea; }.equipment-card.selected,.chamber-card.selected { outline: 2px solid #292821; outline-offset: 1px; }.equipment-picture { align-self: center; display: grid; width: 54px; height: 48px; margin-bottom: 5px; place-items: center; border: 1px solid #9a978d; border-radius: 5px; background: linear-gradient(145deg, #e9e7df, #cbc8bd); color: #5b5951; font-size: 30px; }.equipment-name { overflow: hidden; padding: 3px 4px; color: #171713; text-align: center; text-overflow: ellipsis; white-space: nowrap; font-size: 11px; }.equipment-caption,.equipment-state { overflow: hidden; margin-top: 3px; color: #666359; text-align: center; text-overflow: ellipsis; white-space: nowrap; font-size: 10px; }.equipment-state { font-weight: 600; }.empty-board { grid-column: 1 / -1; display: grid; min-height: 220px; place-items: center; color: #777366; }.chamber-board { overflow: auto; padding: 8px; border: 1px solid #c6c3ba; background: #f9f8f3; }.chamber-board h3,.state-panel h3 { margin: 0; font-size: 13px; }.chamber-board p { color: #777366; font-size: 11px; }.chamber-card { display: flex; flex-direction: column; gap: 5px; width: 100%; margin: 7px 0; padding: 8px; border: 1px solid #aaa69a; background: #fff; text-align: left; cursor: pointer; }.chamber-card span { padding: 3px; text-align: center; font-size: 11px; }
.state-panel { min-height: 82px; padding: 7px 9px; border: 1px solid #c6c3ba; background: #f9f8f3; }.state-panel-heading { display: flex; align-items: center; justify-content: space-between; gap: 8px; }.state-panel-heading span { color: #777366; font-size: 11px; }.state-buttons { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 7px; }.state-buttons button { display: inline-flex; align-items: center; gap: 5px; min-width: 92px; min-height: 29px; padding: 4px 8px; border: 1px solid #aaa69a; border-radius: 2px; background: #fff; color: #292821; font: inherit; cursor: pointer; }.state-buttons button:disabled { color: #aaa69a; cursor: default; }.state-buttons i { width: 12px; height: 10px; border: 1px solid #777; }.monitor-status { display: flex; justify-content: space-between; gap: 12px; min-height: 22px; padding: 5px 2px 0; border-top: 1px solid #d0ccc1; color: #5e5b52; font-size: 10px; }
.context-menu { position: fixed; z-index: 70; display: grid; min-width: 190px; padding: 3px; border: 1px solid #6d6b64; background: #f9f8f4; box-shadow: 2px 3px 8px rgb(0 0 0 / 20%); }.context-menu button { padding: 7px 9px; border: 0; background: transparent; color: #292821; text-align: left; font: inherit; cursor: pointer; }.context-menu button:hover { background: #ffe9a5; }
.detail-backdrop { z-index: 80; }.detail-dialog { display: flex; flex-direction: column; gap: 10px; width: min(560px, 94vw); max-height: 85vh; padding: 10px 13px 12px; border: 1px solid #77746b; background: #f5f4ef; box-shadow: 0 14px 40px rgb(0 0 0 / 22%); }.detail-dialog header { margin: -10px -13px 0; padding: 8px 13px; border-bottom: 1px solid #c8c5bb; background: #e8e6de; }.detail-dialog h2 { margin: 2px 0 0; font-size: 16px; }.detail-dialog dl { display: grid; grid-template-columns: 150px 1fr; gap: 7px; margin: 5px 0; }.detail-dialog dt { color: #68655b; }.detail-dialog dd { margin: 0; }.detail-dialog p { margin: 0; color: #68655b; font-size: 12px; }.detail-dialog footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: auto; }.state-change-dialog label { display: grid; gap: 5px; }.state-change-dialog textarea { padding: 7px; border: 1px solid #aaa69a; border-radius: 2px; font: inherit; resize: vertical; }.detail-dialog p.detail-warning { padding: 7px; border: 1px solid #e1d292; background: #fff8dc; color: #62551c; }.message-backdrop { z-index: 90; }.message-box { width: min(360px, 90vw); padding: 14px; border: 1px solid #77746b; background: #f9f8f4; box-shadow: 0 14px 40px rgb(0 0 0 / 22%); }.message-box p { margin: 0 0 14px; }
button:focus-visible,input:focus-visible,select:focus-visible,textarea:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) { .equipment-state-by-type { padding: 8px; }.page-header h1 span { display: block; margin: 2px 0; }.launcher { min-height: 66vh; }.monitor-toolbar { align-items: flex-start; }.chamber-toggle { margin-left: 0; }.monitor-body.with-chambers { grid-template-columns: minmax(0, 1fr); grid-template-rows: minmax(0, 1fr) auto; }.equipment-board { grid-template-columns: repeat(2, minmax(100px, 1fr)); grid-template-rows: auto; }.monitor-dialog { height: 94vh; }.monitor-status { flex-direction: column; }.state-panel-heading { align-items: flex-start; flex-direction: column; } }
</style>
