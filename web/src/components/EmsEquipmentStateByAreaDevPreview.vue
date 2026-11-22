<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

type NodeType = 0 | 1 | 2 | 3 | 4
type DetailMode = 'monitor' | 'log' | 'inventory' | 'area-state' | 'area-history' | null
interface EquipmentStateDefinition { code: number; name: string; color: string }
interface LayoutNode {
  objectNo: string; objectType: NodeType; objectText: string; objectX: number; objectY: number
  objectWidth: number; objectLength: number; objectColor: string; parentAreaNo?: string
  equipmentNo?: string; equipmentType?: string; equipmentGroup?: string; inventoryType?: string
}
interface BaseArea { areaNo: string; areaName: string; departmentNo: string; nodes: LayoutNode[] }
interface StateRow { equipmentNo: string; equipmentState: number | null; userNo: string; startTime: string; equipmentType: string }
type ContextAction = 'monitor' | 'log' | 'inventory' | 'area-state' | 'area-history'

const stateDefinitions: EquipmentStateDefinition[] = [
  { code: 0, name: '待機', color: '#8ba5b5' },
  { code: 1, name: '生產', color: '#4d9e68' },
  { code: 2, name: '異常', color: '#d45f57' },
  { code: 3, name: '換線', color: '#4d85b5' },
  { code: 4, name: '保養', color: '#d2a143' },
]

const baseAreas: BaseArea[] = [
  {
    areaNo: 'DEV-BASE-01', areaName: '開發用組裝區', departmentNo: 'DEV-DEPT', nodes: [
      { objectNo: 'DEV-AREA-A', objectType: 0, objectText: '組裝區 A', objectX: 35, objectY: 32, objectWidth: 380, objectLength: 204, objectColor: '#f6e7b0' },
      { objectNo: 'DEV-EQP-101', objectType: 2, objectText: '貼片機 101', objectX: 80, objectY: 92, objectWidth: 118, objectLength: 62, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-A', equipmentNo: 'DEV-EQP-101', equipmentType: 'DEV-MOUNTER' },
      { objectNo: 'DEV-EQP-102', objectType: 2, objectText: '貼片機 102', objectX: 245, objectY: 92, objectWidth: 118, objectLength: 62, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-A', equipmentNo: 'DEV-EQP-102', equipmentType: 'DEV-MOUNTER' },
      { objectNo: 'DEV-STOCK-01', objectType: 1, objectText: '元件庫', objectX: 478, objectY: 46, objectWidth: 142, objectLength: 72, objectColor: '#dbe8fa', inventoryType: '6' },
      { objectNo: 'DEV-TESTER', objectType: 3, objectText: '測試設備類別', objectX: 690, objectY: 47, objectWidth: 190, objectLength: 68, objectColor: '#e4e0f5', equipmentType: 'DEV-TESTER' },
      { objectNo: 'DEV-GROUP-01', objectType: 4, objectText: '組裝設備群組', objectX: 483, objectY: 198, objectWidth: 176, objectLength: 68, objectColor: '#e5e1d6', equipmentGroup: 'DEV-GROUP-01' },
      { objectNo: 'DEV-AREA-B', objectType: 0, objectText: '檢測區 B', objectX: 690, objectY: 182, objectWidth: 245, objectLength: 215, objectColor: '#d8e8ee' },
      { objectNo: 'DEV-EQP-103', objectType: 2, objectText: '測試機 103', objectX: 708, objectY: 235, objectWidth: 96, objectLength: 60, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-B', equipmentNo: 'DEV-EQP-103', equipmentType: 'DEV-TESTER' },
      { objectNo: 'DEV-EQP-104', objectType: 2, objectText: '測試機 104', objectX: 821, objectY: 235, objectWidth: 96, objectLength: 60, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-B', equipmentNo: 'DEV-EQP-104', equipmentType: 'DEV-TESTER' },
    ],
  },
  {
    areaNo: 'DEV-BASE-02', areaName: '開發用包裝區', departmentNo: 'DEV-DEPT', nodes: [
      { objectNo: 'DEV-AREA-PACK', objectType: 0, objectText: '包裝線', objectX: 48, objectY: 45, objectWidth: 390, objectLength: 235, objectColor: '#fae5a9' },
      { objectNo: 'DEV-EQP-201', objectType: 2, objectText: '封裝機 201', objectX: 92, objectY: 112, objectWidth: 130, objectLength: 66, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-PACK', equipmentNo: 'DEV-EQP-201', equipmentType: 'DEV-PACKER' },
      { objectNo: 'DEV-EQP-202', objectType: 2, objectText: '封裝機 202', objectX: 260, objectY: 112, objectWidth: 130, objectLength: 66, objectColor: '#d8e5d5', parentAreaNo: 'DEV-AREA-PACK', equipmentNo: 'DEV-EQP-202', equipmentType: 'DEV-PACKER' },
      { objectNo: 'DEV-STOCK-02', objectType: 1, objectText: '包材庫', objectX: 530, objectY: 70, objectWidth: 150, objectLength: 78, objectColor: '#dbe8fa', inventoryType: '6' },
      { objectNo: 'DEV-GROUP-02', objectType: 4, objectText: '包裝設備群組', objectX: 520, objectY: 215, objectWidth: 192, objectLength: 72, objectColor: '#e5e1d6', equipmentGroup: 'DEV-GROUP-02' },
    ],
  },
]

const snapshots: Record<string, number | null>[] = [
  { 'DEV-EQP-101': 1, 'DEV-EQP-102': 2, 'DEV-EQP-103': 4, 'DEV-EQP-104': null, 'DEV-EQP-201': 1, 'DEV-EQP-202': 0 },
  { 'DEV-EQP-101': 2, 'DEV-EQP-102': 1, 'DEV-EQP-103': 0, 'DEV-EQP-104': 3, 'DEV-EQP-201': 4, 'DEV-EQP-202': 1 },
]
const equipmentUsers: Record<string, string> = {
  'DEV-EQP-101': 'DEV-OP-01', 'DEV-EQP-102': 'DEV-OP-02', 'DEV-EQP-103': 'DEV-OP-03',
  'DEV-EQP-104': 'DEV-OP-04', 'DEV-EQP-201': 'DEV-OP-02', 'DEV-EQP-202': 'DEV-OP-05',
}
const groupMembers: Record<string, string[]> = { 'DEV-GROUP-01': ['DEV-EQP-101', 'DEV-EQP-102'], 'DEV-GROUP-02': ['DEV-EQP-201', 'DEV-EQP-202'] }
const areaChildren: Record<string, string[]> = { 'DEV-AREA-A': ['DEV-EQP-101', 'DEV-EQP-102'], 'DEV-AREA-B': ['DEV-EQP-103', 'DEV-EQP-104'], 'DEV-AREA-PACK': ['DEV-EQP-201', 'DEV-EQP-202'] }

const selectedAreaNo = ref('')
const colorMode = ref<'area' | 'state'>('area')
const zoom = ref(100)
const gridVisible = ref(true)
const snapshotIndex = ref(0)
const selectedNodeNo = ref('')
const statusMessage = ref('Nothing be selected')
const contextMenu = ref<{ x: number; y: number; node: LayoutNode } | null>(null)
const detailMode = ref<DetailMode>(null)
const detailNode = ref<LayoutNode | null>(null)
const mockEnterpriseNo = 'IMESTECH'

const activeArea = computed(() => baseAreas.find((area) => area.areaNo === selectedAreaNo.value) ?? null)
const layoutNodes = computed(() => activeArea.value?.nodes ?? [])
const stateRows = computed<StateRow[]>(() => {
  const current = snapshots[snapshotIndex.value]
  return layoutNodes.value.filter((node) => node.objectType === 2 && node.equipmentNo).map((node) => ({
    equipmentNo: node.equipmentNo ?? '',
    equipmentState: current[node.equipmentNo ?? ''] ?? null,
    userNo: equipmentUsers[node.equipmentNo ?? ''] ?? 'DEV-OP-00',
    startTime: snapshotIndex.value === 0 ? '2026-11-23 08:00:00' : '2026-11-23 09:15:00',
    equipmentType: node.equipmentType ?? '',
  }))
})
const selectedNode = computed(() => layoutNodes.value.find((node) => node.objectNo === selectedNodeNo.value) ?? null)
const detailTitle = computed(() => ({
  monitor: 'Equipment State Monitor', log: 'Show Equipment State Log', inventory: 'Inventory',
  'area-state': 'Area Operator State', 'area-history': 'Area Operator History',
}[detailMode.value ?? 'monitor']))

const contextActions = computed((): { action: ContextAction; label: string }[] => {
  const node = contextMenu.value?.node
  if (!node) return []
  if (node.objectType === 0) return [
    { action: 'monitor', label: 'Equipment State Monitor' },
    { action: 'log', label: 'Show Equipment State Log' },
    { action: 'area-state', label: 'State' },
    { action: 'area-history', label: 'History' },
  ]
  if (node.objectType === 1) return [{ action: 'inventory', label: 'Inventory' }]
  if (node.objectType === 2) return [
    { action: 'monitor', label: 'Equipment State Monitor' },
    { action: 'log', label: 'Show Equipment State Log' },
  ]
  return []
})

function stateFor(equipmentNo: string | undefined): StateRow | null {
  return equipmentNo ? stateRows.value.find((row) => row.equipmentNo === equipmentNo) ?? null : null
}

function nodeFill(node: LayoutNode): string {
  if (colorMode.value === 'area') return node.objectColor
  if (node.objectType !== 2) return '#ffffff'
  const current = stateFor(node.equipmentNo)
  return stateDefinitions.find((state) => state.code === current?.equipmentState)?.color ?? '#ffffff'
}

function nodeShape(node: LayoutNode): 'rect' | 'round' | 'ellipse' | 'hexagon' {
  if (node.objectType === 0) return 'rect'
  if (node.objectType === 2) return 'ellipse'
  if (node.objectType === 1) return 'hexagon'
  return 'round'
}

function selectNode(node: LayoutNode): void {
  selectedNodeNo.value = node.objectNo
  statusMessage.value = `Selected : Node ${node.objectText}`
  contextMenu.value = null
}

function changeBaseArea(): void {
  selectedNodeNo.value = ''
  statusMessage.value = activeArea.value ? `${activeArea.value.areaNo} · ${activeArea.value.areaName}` : 'Nothing be selected'
  contextMenu.value = null
  detailMode.value = null
}

function refreshEquipmentStates(): void {
  if (colorMode.value !== 'state' || !activeArea.value) return
  snapshotIndex.value = (snapshotIndex.value + 1) % snapshots.length
  statusMessage.value = `DEV 狀態資料已重新載入（Fixture ${snapshotIndex.value + 1}）`
}

function showContext(event: MouseEvent, node: LayoutNode): void {
  selectNode(node)
  if (mockEnterpriseNo !== 'IMESTECH' || ![0, 1, 2].includes(node.objectType)) return
  contextMenu.value = { x: event.clientX, y: event.clientY, node }
}

function equipmentRowsFor(node: LayoutNode | null): StateRow[] {
  if (!node) return stateRows.value
  if (node.objectType === 2) return stateRows.value.filter((row) => row.equipmentNo === node.equipmentNo)
  if (node.objectType === 3) return stateRows.value.filter((row) => row.equipmentType === node.equipmentType)
  if (node.objectType === 4) return stateRows.value.filter((row) => (groupMembers[node.objectNo] ?? []).includes(row.equipmentNo))
  if (node.objectType === 0) return stateRows.value.filter((row) => (areaChildren[node.objectNo] ?? []).includes(row.equipmentNo))
  return []
}

const detailEquipmentRows = computed(() => equipmentRowsFor(detailNode.value))
const detailOperatorRows = computed(() => detailMode.value === 'area-history'
  ? [
      { userNo: 'DEV-OP-01', shiftNo: 'DAY', loginTime: '2026-11-22 08:00', logoutTime: '2026-11-22 17:00', comment: '交班完成' },
      { userNo: 'DEV-OP-02', shiftNo: 'NIGHT', loginTime: '2026-11-22 20:00', logoutTime: '2026-11-23 05:00', comment: '例行巡檢' },
    ]
  : [{ userNo: 'DEV-OP-03', shiftNo: 'DAY', loginTime: '2026-11-23 08:00', logoutTime: '—', comment: '目前在區' }])

function openDetail(mode: DetailMode, node: LayoutNode | null): void {
  detailNode.value = node
  detailMode.value = mode
  contextMenu.value = null
}

function runContextAction(action: ContextAction): void {
  const node = contextMenu.value?.node ?? null
  openDetail(action, node)
}

function openNode(node: LayoutNode): void {
  selectNode(node)
  if (node.objectType === 1) openDetail('inventory', node)
  else openDetail('monitor', node)
}

function closeDetail(): void { detailMode.value = null; detailNode.value = null }
</script>

<template>
  <section v-if="dev" class="equipment-state-by-area" aria-label="設備狀態監控_區域 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES203 · DEV fixtures</div><h1>設備狀態監控_區域 <span>EQUIPMENT STATE BY AREA</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>區域配置、設備狀態與 IMESTECH 右鍵選單都是虛構資料；Refresh 只切換本頁狀態快照。</span></div>

    <section class="monitor-shell" @click="contextMenu = null">
      <div class="toolbar">
        <label class="base-area"><span>Base Area</span>
          <select v-model="selectedAreaNo" @change="changeBaseArea">
            <option value="" disabled>請選擇 Base Area</option>
            <option v-for="area in baseAreas" :key="area.areaNo" :value="area.areaNo">{{ area.areaNo }} · {{ area.areaName }}</option>
          </select>
        </label>
        <fieldset class="color-mode">
          <legend>Color</legend>
          <label><input v-model="colorMode" type="radio" value="area" />Show Area Color</label>
          <label><input v-model="colorMode" type="radio" value="state" />Show EQP State Color</label>
        </fieldset>
        <label class="zoom-control">Zoom
          <select v-model.number="zoom"><option v-for="level in [100, 150, 75, 50]" :key="level" :value="level">{{ level }}%</option></select>
        </label>
        <button type="button" class="toolbar-button" :class="{ toggled: gridVisible }" @click="gridVisible = !gridVisible">Grid</button>
        <button type="button" class="toolbar-button" :disabled="colorMode !== 'state' || !activeArea" @click="refreshEquipmentStates">Refresh</button>
        <button type="button" class="toolbar-button close-button" @click="emit('close')">Close</button>
      </div>

      <div v-if="colorMode === 'state' && activeArea" class="state-legend" aria-label="設備狀態顏色">
        <span v-for="state in stateDefinitions" :key="state.code"><i :style="{ backgroundColor: state.color }"></i>{{ state.name }} ({{ state.code }})</span>
        <span><i class="unknown-color"></i>未取得狀態</span>
      </div>

      <main class="canvas-frame" :class="{ 'with-grid': gridVisible }" @click.self="contextMenu = null" @dblclick.self="activeArea && openDetail('monitor', null)">
        <div v-if="!activeArea" class="canvas-empty">請選擇 Base Area 載入區域配置</div>
        <svg v-else :width="984 * zoom / 100" :height="469 * zoom / 100" viewBox="0 0 984 469" class="layout-canvas" role="img" aria-label="設備與區域配置圖">
          <g v-for="node in layoutNodes" :key="node.objectType + ':' + node.objectNo" class="layout-node" :class="{ selected: selectedNodeNo === node.objectNo }" role="button" tabindex="0" @click.stop="selectNode(node)" @contextmenu.prevent.stop="showContext($event, node)" @dblclick.stop="openNode(node)" @keydown.enter.stop="openNode(node)">
            <rect v-if="nodeShape(node) === 'rect'" :x="node.objectX" :y="node.objectY" :width="node.objectWidth" :height="node.objectLength" :fill="nodeFill(node)" stroke="#555" />
            <rect v-else-if="nodeShape(node) === 'round'" :x="node.objectX" :y="node.objectY" :width="node.objectWidth" :height="node.objectLength" rx="11" :fill="nodeFill(node)" stroke="#555" />
            <ellipse v-else-if="nodeShape(node) === 'ellipse'" :cx="node.objectX + node.objectWidth / 2" :cy="node.objectY + node.objectLength / 2" :rx="node.objectWidth / 2" :ry="node.objectLength / 2" :fill="nodeFill(node)" stroke="#555" />
            <polygon v-else :points="`${node.objectX + 13},${node.objectY} ${node.objectX + node.objectWidth - 13},${node.objectY} ${node.objectX + node.objectWidth},${node.objectY + node.objectLength / 2} ${node.objectX + node.objectWidth - 13},${node.objectY + node.objectLength} ${node.objectX + 13},${node.objectY + node.objectLength} ${node.objectX},${node.objectY + node.objectLength / 2}`" :fill="nodeFill(node)" stroke="#555" />
            <text :x="node.objectX + node.objectWidth / 2" :y="node.objectType === 0 ? node.objectY + 22 : node.objectY + node.objectLength / 2" text-anchor="middle" dominant-baseline="middle">{{ node.objectText }}</text>
            <title>{{ node.objectText }} · {{ node.objectNo }}</title>
          </g>
          <text v-if="layoutNodes.length === 0" x="492" y="235" text-anchor="middle" class="canvas-placeholder">目前沒有配置節點</text>
        </svg>
        <div v-if="contextMenu" class="context-menu" :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }" @click.stop>
          <button v-for="entry in contextActions" :key="entry.action" type="button" @click="runContextAction(entry.action)">{{ entry.label }}</button>
        </div>
      </main>

      <footer class="status-line"><span>{{ activeArea ? `${activeArea.areaNo} · ${activeArea.areaName}` : '未選擇區域' }}</span><span>{{ statusMessage }}</span><span>節點數：{{ layoutNodes.length }} · Zoom {{ zoom }}%</span></footer>
    </section>

    <section v-if="detailMode" class="dialog-backdrop" @click.self="closeDetail">
      <article class="detail-dialog" role="dialog" aria-modal="true" :aria-label="detailTitle">
        <header><div><div class="eyebrow">{{ detailTitle }}</div><h2>{{ detailNode ? `${detailNode.objectText} · ${detailNode.objectNo}` : activeArea?.areaName }}</h2></div><button type="button" aria-label="關閉視窗" @click="closeDetail">×</button></header>
        <template v-if="detailMode === 'monitor' || detailMode === 'log'">
          <p class="detail-note">{{ detailMode === 'monitor' ? 'DEV 模擬設備現況；來源會另開 ES_EquipmentStateMonitor。' : 'DEV 模擬設備狀態紀錄；來源會另開 ES_EquipmentStateLog。' }}</p>
          <div class="table-scroll"><table><thead><tr><th>Equipment No</th><th>Equipment Type</th><th>Equipment State</th><th>User No</th><th>Start Time</th></tr></thead><tbody>
            <tr v-for="row in detailEquipmentRows" :key="row.equipmentNo"><td>{{ row.equipmentNo }}</td><td>{{ row.equipmentType }}</td><td><span class="state-swatch" :style="{ backgroundColor: stateDefinitions.find((state) => state.code === row.equipmentState)?.color ?? '#fff' }"></span>{{ stateDefinitions.find((state) => state.code === row.equipmentState)?.name ?? '未取得狀態' }}</td><td>{{ row.userNo }}</td><td>{{ row.startTime }}</td></tr>
            <tr v-if="detailEquipmentRows.length === 0"><td colspan="5" class="empty-row">此節點沒有符合的設備狀態假資料。</td></tr>
          </tbody></table></div>
        </template>
        <template v-else-if="detailMode === 'inventory'">
          <dl class="inventory-details"><dt>Inventory No</dt><dd>{{ detailNode?.objectNo }}</dd><dt>Inventory Name</dt><dd>{{ detailNode?.objectText }}</dd><dt>Inventory Type</dt><dd>{{ detailNode?.inventoryType }}</dd><dt>Factory</dt><dd>DEV-FACTORY</dd></dl>
          <p class="detail-note">來源雙擊／右鍵會開啟庫房庫存功能；本預覽只顯示虛構節點資料。</p>
        </template>
        <template v-else>
          <p class="detail-note">DEV 模擬此區域節點所開啟的作業人員 {{ detailMode === 'area-history' ? '歷程' : '目前狀態' }}資料。</p>
          <div class="table-scroll"><table><thead><tr><th>User No</th><th>Shift No</th><th>Login Time</th><th>{{ detailMode === 'area-history' ? 'Logout Time' : '狀態' }}</th><th>Comment</th></tr></thead><tbody>
            <tr v-for="row in detailOperatorRows" :key="row.userNo"><td>{{ row.userNo }}</td><td>{{ row.shiftNo }}</td><td>{{ row.loginTime }}</td><td>{{ detailMode === 'area-history' ? row.logoutTime : '已登入' }}</td><td>{{ row.comment }}</td></tr>
          </tbody></table></div>
        </template>
        <footer><button type="button" class="toolbar-button" @click="closeDetail">Close</button></footer>
      </article>
    </section>
  </section>
  <section v-else class="empty-state" aria-label="DEV 假資料未啟用">DEV 預覽未啟用。</section>
</template>

<style scoped>
.equipment-state-by-area { height: 100%; min-height: 0; overflow: auto; padding: 14px; box-sizing: border-box; background: #f3f2ed; color: #26251f; font: inherit; }
.page-header,.toolbar,.status-line,.detail-dialog header,.detail-dialog footer { display: flex; align-items: center; gap: 10px; }
.page-header { justify-content: space-between; max-width: 1240px; margin: 0 auto 9px; }
.eyebrow { color: #777366; font-size: 12px; } h1 { margin: 3px 0 0; font-size: 21px; } h1 span { margin-left: 7px; color: #777366; font-size: 12px; font-weight: 500; }
.close-icon,.detail-dialog header button { width: 32px; height: 32px; border: 1px solid #aaa69a; border-radius: 3px; background: #f8f7f2; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner { display: flex; align-items: center; gap: 9px; max-width: 1240px; margin: 0 auto 9px; padding: 7px 10px; border: 1px solid #e4d69d; border-radius: 3px; background: #fff8dc; color: #62551c; font-size: 12px; }
.fixture-banner strong { padding: 2px 6px; border-radius: 8px; background: #efda83; font-size: 11px; }
.monitor-shell { max-width: 1240px; margin: 0 auto; padding: 7px; border: 1px solid #aaa69a; background: #ededea; }
.toolbar { min-height: 40px; flex-wrap: wrap; padding: 1px 2px 7px; }
.base-area { display: flex; align-items: center; gap: 6px; font-size: 12px; }
.base-area select,.zoom-control select { min-height: 27px; min-width: 205px; border: 1px solid #aaa69a; border-radius: 2px; padding: 3px 6px; background: white; color: #282720; font: inherit; }
.color-mode { display: flex; align-items: center; gap: 12px; margin: 0 4px; padding: 4px 8px 5px; border: 1px solid #c7c3b8; }
.color-mode legend { padding: 0 4px; color: #6c695f; font-size: 11px; }.color-mode label,.zoom-control { display: flex; align-items: center; gap: 5px; font-size: 12px; white-space: nowrap; }
.zoom-control select { min-width: 72px; }.toolbar-button { min-height: 27px; padding: 3px 11px; border: 1px solid #aaa69a; border-radius: 2px; background: #f8f7f2; color: #292821; font: inherit; cursor: pointer; }
.toolbar-button:hover:not(:disabled) { background: #fff4cd; }.toolbar-button:disabled { color: #999; background: #e7e6e1; cursor: default; }.toolbar-button.toggled { border-color: #786526; background: #fff0b7; }
.close-button { margin-left: auto; }
.state-legend { display: flex; flex-wrap: wrap; gap: 8px 14px; padding: 6px 8px; border: 1px solid #d0ccc1; border-bottom: 0; background: #faf9f5; font-size: 11px; }
.state-legend span { display: flex; align-items: center; gap: 5px; }.state-legend i,.state-swatch { display: inline-block; width: 14px; height: 12px; border: 1px solid #78766f; vertical-align: -2px; }.state-legend i.unknown-color { background: white; }
.canvas-frame { position: relative; min-height: 380px; overflow: auto; border: 1px solid #77766e; background-color: #fff; }
.canvas-frame.with-grid { background-image: linear-gradient(#e9e7e2 1px, transparent 1px), linear-gradient(90deg, #e9e7e2 1px, transparent 1px); background-size: 20px 20px; }
.canvas-empty { display: grid; min-height: 420px; place-items: center; color: #777366; font-size: 13px; }.layout-canvas { display: block; background: transparent; }
.layout-node { cursor: pointer; }.layout-node text { fill: #26251f; font: 12px Arial, sans-serif; pointer-events: none; }.layout-node.selected > rect,.layout-node.selected > ellipse,.layout-node.selected > polygon { stroke: #20201d; stroke-width: 3; }
.canvas-placeholder { fill: #777366; font: 13px Arial, sans-serif; }
.status-line { min-height: 29px; justify-content: space-between; padding: 3px 5px 0; color: #555349; font-size: 11px; }
.context-menu { position: fixed; z-index: 30; display: grid; min-width: 205px; padding: 3px; border: 1px solid #6d6b64; background: #f9f8f4; box-shadow: 2px 3px 8px rgb(0 0 0 / 20%); }
.context-menu button { padding: 7px 9px; border: 0; background: transparent; color: #292821; text-align: left; font: inherit; cursor: pointer; }.context-menu button:hover { background: #ffe9a5; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 40; display: grid; place-items: center; padding: 14px; background: rgb(20 19 16 / 40%); }
.detail-dialog { display: flex; flex-direction: column; gap: 8px; width: min(900px, 96vw); max-height: 86vh; padding: 0 12px 10px; border: 1px solid #77746b; background: #f5f4ef; box-shadow: 0 14px 40px rgb(0 0 0 / 22%); }
.detail-dialog header { justify-content: space-between; margin: 0 -12px; padding: 8px 12px; border-bottom: 1px solid #c8c5bb; background: #e8e6de; }.detail-dialog h2 { margin: 2px 0 0; font-size: 16px; }.detail-note { margin: 0; color: #68655b; font-size: 12px; }.table-scroll { overflow: auto; border: 1px solid #c5c2b8; background: white; }
table { width: 100%; border-collapse: collapse; font-size: 12px; white-space: nowrap; }th,td { padding: 7px 9px; border-bottom: 1px solid #e4e1d8; text-align: left; }th { background: #eceae2; color: #4b4941; font-weight: 600; }.empty-row { padding: 18px; color: #777366; text-align: center; }
.inventory-details { display: grid; grid-template-columns: 160px 1fr; gap: 7px; margin: 6px 0; font-size: 13px; }.inventory-details dt { color: #68655b; }.inventory-details dd { margin: 0; }.detail-dialog footer { justify-content: flex-end; margin-top: auto; }
.empty-state { padding: 16px; color: #777366; }button:focus-visible,input:focus-visible,select:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) { .equipment-state-by-area { padding: 8px; } h1 span { display: block; margin: 2px 0; }.color-mode { order: 1; width: 100%; }.close-button { margin-left: 0; }.status-line { flex-wrap: wrap; }.detail-dialog { width: 96vw; } }
</style>
