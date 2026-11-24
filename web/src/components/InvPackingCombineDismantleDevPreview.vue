<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const emit = defineEmits<{ close: [] }>()
type PackType = -1 | 0 | 1
type DestinationPackType = 0 | 1
interface PackNode {
  packingNo: string
  moNo: string
  level: number
  levelName: string
  qty: number
  lotQty: number
  packType: PackType
  position: string
  nodeId: string
  closed: boolean
  fgdIn: boolean
  lotStatus: number
  fullBoxBoxQty?: number
  fullBoxNumberQty?: number
  children: PackNode[]
}
interface Operation {
  time: string
  type: '拆箱' | '並箱'
  source: string
  destination: string
  detail: string
}
interface DevState {
  packPool: PackNode[]
  operations: Operation[]
}
interface LoadedRoot {
  root: PackNode
}
interface DismantleRow {
  key: string
  rootNo: string
  packingNo: string
  level: number
  levelName: string
  qty: number
  lotQty: number
  packType: PackType
  position: string
  lotStatus: number
  children: PackNode[]
}

const storageKey = 'ymes.dev.iv310-packing-combine-dismantle.v1'
const labels: Record<number, string> = { '-1': 'Lot', 0: '線上包裝', 1: '線邊倉包裝' }
const modes = [
  { value: 0, label: '拆解單一包裝階層' },
  { value: 1, label: '零散拆箱（選取子包裝）' },
  { value: 2, label: '拆解到生產批' },
]
function lot(no: string, mo: string, qty: number, position: string, status = 1): PackNode {
  return { packingNo: no, moNo: mo, level: 0, levelName: 'Lot', qty: 1, lotQty: qty, packType: -1, position, nodeId: 'NODE-DEV-10', closed: false, fgdIn: false, lotStatus: status, children: [] }
}
function pack(no: string, mo: string, level: number, children: PackNode[], capacityBoxes: number, capacityQty: number, packType: DestinationPackType = 0, position = 'OP-DEV-10'): PackNode {
  const lotQty = children.reduce((sum, child) => sum + child.lotQty, 0)
  return { packingNo: no, moNo: mo, level, levelName: 'L' + level + ' 包裝', qty: children.length, lotQty, packType, position, nodeId: 'NODE-DEV-10', closed: false, fgdIn: false, lotStatus: 1, children, fullBoxBoxQty: capacityBoxes, fullBoxNumberQty: capacityQty }
}
function initialState(): DevState {
  const mo = 'MO-DEV-210'
  const unboxA = pack('BOX-DEV-210', mo, 2, [
    pack('PKG-DEV-210-A', mo, 1, [lot('LOT-DEV-210-01', mo, 2, 'OP-DEV-10'), lot('LOT-DEV-210-02', mo, 2, 'OP-DEV-10')], 4, 8),
    pack('PKG-DEV-210-B', mo, 1, [lot('LOT-DEV-210-03', mo, 3, 'OP-DEV-10'), lot('LOT-DEV-210-04', mo, 1, 'OP-DEV-10')], 4, 8),
  ], 4, 8)
  const target = pack('BOX-DEV-TGT-200', mo, 2, [
    pack('PKG-DEV-TGT-201', mo, 1, [lot('LOT-DEV-TGT-01', mo, 2, 'OP-DEV-10')], 4, 8),
    pack('PKG-DEV-TGT-202', mo, 1, [lot('LOT-DEV-TGT-02', mo, 2, 'OP-DEV-10')], 4, 8),
  ], 4, 8)
  const fromA = pack('PKG-DEV-SRC-301', mo, 1, [lot('LOT-DEV-SRC-01', mo, 1, 'OP-DEV-10'), lot('LOT-DEV-SRC-02', mo, 1, 'OP-DEV-10')], 4, 8)
  const fromB = pack('PKG-DEV-SRC-302', mo, 1, [lot('LOT-DEV-SRC-03', mo, 1, 'OP-DEV-10'), lot('LOT-DEV-SRC-04', mo, 1, 'OP-DEV-10')], 4, 8)
  const semi = pack('PKG-DEV-SEMI-401', mo, 1, [lot('LOT-DEV-SEMI-01', mo, 2, 'WIP-SEMI-01', 11)], 4, 8, 1, 'WIP-SEMI-01')
  const semiTarget = pack('BOX-DEV-SEMI-500', mo, 2, [pack('PKG-DEV-SEMI-501', mo, 1, [lot('LOT-DEV-SEMI-TGT-01', mo, 2, 'WIP-SEMI-01', 11)], 4, 8, 1, 'WIP-SEMI-01')], 4, 8, 1, 'WIP-SEMI-01')
  return { packPool: [unboxA, target, fromA, fromB, semiTarget, semi], operations: [] }
}
function readState(): DevState {
  try {
    const value = localStorage.getItem(storageKey)
    if (value) {
      const parsed = JSON.parse(value) as DevState
      if (Array.isArray(parsed.packPool) && Array.isArray(parsed.operations)) return parsed
    }
  } catch { /* Continue with the in-memory DEV fixtures. */ }
  return initialState()
}
function cloneNode(node: PackNode): PackNode { return JSON.parse(JSON.stringify(node)) as PackNode }
function findPool(no: string): PackNode | undefined { return state.value.packPool.find((node) => node.packingNo.toLowerCase() === no.trim().toLowerCase()) }
function leaves(node: PackNode): PackNode[] { return node.level === 0 ? [node] : node.children.flatMap(leaves) }
function nowText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }

const state = ref<DevState>(readState())
const activeTab = ref<'unboxing' | 'combine'>('unboxing')
const unboxingMethod = ref(0)
const unboxInput = ref('')
const loadedRoots = ref<LoadedRoot[]>([])
const selectedRows = ref<string[]>([])
const combineToInput = ref('')
const combineFromInput = ref('')
const combineTargetNo = ref('')
const combineFrom = ref<PackNode[]>([])
const autoCloseFull = ref(false)
const labelPreview = ref(false)
const dialog = ref<{ title: string; body: string; action: 'dismantle' | 'combine' } | null>(null)
const notice = ref('此頁只使用 DEV 假資料；不連 Oracle、PostgreSQL 或正式 API。')
const poolOptions = computed(() => state.value.packPool.map((item) => item.packingNo))
const combineTarget = computed(() => findPool(combineTargetNo.value))
const targetBoxes = computed(() => combineTarget.value?.children.length ?? 0)
const targetLotQty = computed(() => combineTarget.value?.children.reduce((sum, row) => sum + row.lotQty, 0) ?? 0)
const sourceLotQty = computed(() => combineFrom.value.reduce((sum, row) => sum + row.lotQty, 0))
const fullBoxQty = computed(() => Number(combineTarget.value?.fullBoxBoxQty ?? 0))
const fullLotQty = computed(() => Number(combineTarget.value?.fullBoxNumberQty ?? 0))
const unboxRows = computed<DismantleRow[]>(() => loadedRoots.value.flatMap(({ root }) => {
  const targets = unboxingMethod.value === 2 ? leaves(root) : root.children
  return targets.map((row) => ({
    key: root.packingNo + '|' + row.packingNo,
    rootNo: root.packingNo,
    packingNo: row.packingNo,
    level: row.level,
    levelName: row.levelName,
    qty: row.qty,
    lotQty: row.lotQty,
    packType: row.packType,
    position: row.position,
    lotStatus: row.lotStatus,
    children: row.children,
  }))
}))
const checkedCount = computed(() => unboxRows.value.filter((row) => selectedRows.value.includes(row.key)).length)

watch(unboxingMethod, () => {
  loadedRoots.value = []
  selectedRows.value = []
  notice.value = '拆箱方式已切換；來源畫面會清空目前已載入的包裝。'
})

function persist(message: string): void {
  try { localStorage.setItem(storageKey, JSON.stringify(state.value)) } catch { /* This page remains usable for the current session. */ }
  notice.value = message
}
function scanUnboxing(): void {
  const node = findPool(unboxInput.value)
  if (!unboxInput.value.trim()) return
  if (!node || node.level < 1) { notice.value = '找不到可拆的頂層包裝；請輸入示範包裝編號。'; return }
  if (node.closed || node.fgdIn) { notice.value = '已封箱或已開立成品入庫單的包裝不可拆箱。'; return }
  if (loadedRoots.value.some(({ root }) => root.packingNo === node.packingNo)) { notice.value = '此包裝已載入。'; return }
  const first = loadedRoots.value[0]?.root
  if (first && (first.moNo !== node.moNo || first.level !== node.level)) { notice.value = '同一批拆箱資料必須來自相同工單與包裝階層。'; return }
  loadedRoots.value.push({ root: cloneNode(node) })
  selectedRows.value = []
  notice.value = '已載入 ' + node.packingNo + '；可展開子層檢視。'
  unboxInput.value = ''
}
function deleteLoadedRoot(no: string): void {
  loadedRoots.value = loadedRoots.value.filter(({ root }) => root.packingNo !== no)
  selectedRows.value = selectedRows.value.filter((key) => !key.startsWith(no + '|'))
  notice.value = '已從本次拆箱清單移除 ' + no + '；DEV 中尚未變更包裝資料。'
}
function clearUnboxing(): void {
  loadedRoots.value = []
  selectedRows.value = []
  notice.value = '已清除本次拆箱清單。'
}
function openDismantleConfirm(): void {
  if (!loadedRoots.value.length) { notice.value = '請先載入包裝。'; return }
  const count = unboxingMethod.value === 1 ? checkedCount.value : unboxRows.value.length
  if (!count) { notice.value = '請至少勾選一個子包裝。'; return }
  dialog.value = {
    title: '確認拆箱',
    body: modes[unboxingMethod.value].label + '，將處理 ' + count + ' 個子項目。只會更新本頁 DEV 假資料。',
    action: 'dismantle',
  }
}
function executeDismantle(): void {
  const picksByRoot = new Map<string, string[]>()
  for (const row of unboxRows.value) {
    if (unboxingMethod.value === 1 && !selectedRows.value.includes(row.key)) continue
    const picks = picksByRoot.get(row.rootNo) ?? []
    picks.push(row.packingNo)
    picksByRoot.set(row.rootNo, picks)
  }
  for (const { root } of loadedRoots.value) {
    const picks = picksByRoot.get(root.packingNo) ?? []
    if (!picks.length) continue
    const pickedNodes = unboxingMethod.value === 2
      ? leaves(root).filter((row) => picks.includes(row.packingNo))
      : root.children.filter((row) => picks.includes(row.packingNo))
    if (root.packType === 0) {
      const blocked = pickedNodes.flatMap(leaves).find((row) => ![0, 1, 11].includes(row.lotStatus))
      if (blocked) {
        notice.value = '生產批 ' + blocked.packingNo + ' 狀態不允許拆箱。'
        dialog.value = null
        return
      }
    }
  }
  for (const { root } of loadedRoots.value) {
    const picks = picksByRoot.get(root.packingNo) ?? []
    if (!picks.length) continue
    const current = findPool(root.packingNo)
    if (!current) continue
    if (unboxingMethod.value === 1) {
      const picked = current.children.filter((child) => picks.includes(child.packingNo))
      current.children = current.children.filter((child) => !picks.includes(child.packingNo))
      current.qty = current.children.length
      current.lotQty = current.children.reduce((sum, child) => sum + child.lotQty, 0)
      state.value.packPool.push(...picked.map(cloneNode))
      if (!current.children.length) state.value.packPool = state.value.packPool.filter((row) => row.packingNo !== root.packingNo)
    } else {
      const released = unboxingMethod.value === 2
        ? leaves(current).filter((row) => picks.includes(row.packingNo))
        : current.children.map(cloneNode)
      state.value.packPool = state.value.packPool.filter((row) => row.packingNo !== root.packingNo)
      state.value.packPool.push(...released.map(cloneNode))
    }
    state.value.operations.unshift({ time: nowText(), type: '拆箱', source: root.packingNo, destination: 'L' + (unboxingMethod.value === 2 ? '0 生產批' : root.level - 1), detail: modes[unboxingMethod.value].label + ' / ' + picks.length + ' 項' })
  }
  loadedRoots.value = []
  selectedRows.value = []
  dialog.value = null
  persist('拆箱完成：DEV 包裝池、拆並箱歷程及來源狀態示意已更新。')
}
function scanCombineTarget(): void {
  const node = findPool(combineToInput.value)
  if (!combineToInput.value.trim()) return
  if (!node || node.level < 1) { notice.value = '找不到可並入的目標包裝。'; return }
  if (node.closed || node.fgdIn) { notice.value = '目標包裝已封箱或已開立成品入庫單。'; return }
  if (node.fullBoxBoxQty && node.children.length >= node.fullBoxBoxQty) { notice.value = '目標包裝已達滿箱數量，請先處理封箱。'; return }
  combineTargetNo.value = node.packingNo
  combineFrom.value = []
  combineToInput.value = ''
  notice.value = '已載入目標 ' + node.packingNo + '；請逐筆輸入來源包裝。'
}
function scanCombineSource(): void {
  const no = combineFromInput.value.trim()
  const target = combineTarget.value
  const node = findPool(no)
  if (!target) { notice.value = '請先載入 Combine To 目標包裝。'; return }
  if (!no) return
  if (!node || node.level !== target.level - 1) { notice.value = '來源包裝階層必須剛好比目標低一層。'; return }
  if (node.moNo !== target.moNo) { notice.value = '來源與目標工單不同。'; return }
  if (node.packingNo === target.packingNo || combineFrom.value.some((row) => row.packingNo === node.packingNo)) { notice.value = '此來源包裝已存在於清單。'; return }
  if (node.closed || node.fgdIn) { notice.value = '已封箱或已開立成品入庫單的來源包裝不可並入。'; return }
  const sourceLots = leaves(node)
  if (!sourceLots.length) { notice.value = '來源包裝沒有可併入的生產批。'; return }
  for (const sourceLot of sourceLots) {
    if (sourceLot.lotStatus === 1) {
      if (target.packType !== 0 || sourceLot.position !== target.position) { notice.value = '線上 Lot 的作業站須符合目標線上包裝位置。'; return }
    } else if (sourceLot.lotStatus === 11) {
      if (target.packType !== 1 || sourceLot.position !== target.position) { notice.value = '線邊倉 Lot 的 InventoryNo 須符合目標線邊倉位置。'; return }
    } else {
      notice.value = '來源 Lot 狀態不允許並箱。'
      return
    }
  }
  const nextBoxes = target.children.length + combineFrom.value.length + 1
  const nextQty = targetLotQty.value + sourceLotQty.value + node.lotQty
  if ((fullBoxQty.value && nextBoxes > fullBoxQty.value) || (fullLotQty.value && nextQty > fullLotQty.value)) {
    notice.value = '超過包裝規則設定的箱數或數量；已拒絕此來源。'
    return
  }
  combineFrom.value.push(cloneNode(node))
  combineFromInput.value = ''
  notice.value = '已加入來源 ' + node.packingNo + '。'
  const totalBoxes = target.children.length + combineFrom.value.length
  const totalQty = targetLotQty.value + sourceLotQty.value
  const fullByBoxes = fullBoxQty.value > 0 && totalBoxes === fullBoxQty.value
  const fullByQty = fullLotQty.value > 0 && totalQty === fullLotQty.value
  if (fullByBoxes && fullByQty && autoCloseFull.value) {
    notice.value = '箱數與 LotQty 同時達滿箱；依 Auto Closed 設定自動執行並箱。'
    executeCombine()
  } else if (fullByBoxes || fullByQty) {
    dialog.value = {
      title: fullByBoxes && fullByQty ? 'Combine To 已滿箱' : 'Combine To 已達容量條件',
      body: fullByBoxes && fullByQty ? '箱數與 LotQty 已同時滿箱，是否執行並箱？' : '箱數或 LotQty 已達滿箱條件，是否繼續並箱？',
      action: 'combine',
    }
  }
}
function removeCombineSource(no: string): void {
  combineFrom.value = combineFrom.value.filter((row) => row.packingNo !== no)
}
function clearCombineTarget(): void {
  combineTargetNo.value = ''
  combineFrom.value = []
  notice.value = '已清除目標與來源清單。'
}
function clearCombineSources(): void {
  combineFrom.value = []
  notice.value = '已清除來源清單。'
}
function openCombineConfirm(): void {
  const target = combineTarget.value
  if (!target || !combineFrom.value.length) { notice.value = '請先載入目標與至少一個來源包裝。'; return }
  const boxes = target.children.length + combineFrom.value.length
  const qty = targetLotQty.value + sourceLotQty.value
  const fullByBoxes = fullBoxQty.value > 0 && boxes === fullBoxQty.value
  const fullByQty = fullLotQty.value > 0 && qty === fullLotQty.value
  if (boxes > fullBoxQty.value && fullBoxQty.value > 0 || qty > fullLotQty.value && fullLotQty.value > 0) { notice.value = '已超過包裝規則容量。'; return }
  const fullMessage = fullByBoxes && fullByQty
    ? (autoCloseFull.value ? '箱數與數量都達滿箱，將依設定自動處理。' : '箱數與數量都達滿箱；舊 Form 會先詢問是否執行自動封箱。')
    : fullByBoxes || fullByQty ? '箱數或數量達到滿箱條件；舊 Form 仍會詢問是否繼續。' : '尚未達滿箱條件。'
  dialog.value = { title: '確認並箱', body: 'Combine To ' + target.packingNo + '；來源 ' + combineFrom.value.map((row) => row.packingNo).join('、') + '。' + fullMessage + '此動作只更新本頁 DEV 假資料。', action: 'combine' }
}
function executeCombine(): void {
  const target = combineTarget.value
  if (!target) { dialog.value = null; return }
  const sources = combineFrom.value.map(cloneNode)
  const sourceNos = new Set(sources.map((row) => row.packingNo))
  target.children.push(...sources)
  target.qty = target.children.length
  target.lotQty = target.children.reduce((sum, row) => sum + row.lotQty, 0)
  state.value.packPool = state.value.packPool.filter((row) => !sourceNos.has(row.packingNo))
  for (const source of sources) {
    state.value.operations.unshift({ time: nowText(), type: '並箱', source: source.packingNo, destination: target.packingNo, detail: 'Action=1 / ' + source.lotQty + ' LotQty' })
  }
  const finalLevel = target.level >= 6
  if (target.packType === 0 && finalLevel) {
    for (const row of leaves(target)) if (row.lotStatus === 1) row.lotStatus = 4
  }
  combineTargetNo.value = ''
  combineFrom.value = []
  dialog.value = null
  labelPreview.value = true
  persist('並箱 DEV 模擬完成；已記錄 Action=1。線上最終階層可能觸發 lot transfer／自動 BR。')
}
function resetDev(): void {
  state.value = initialState()
  loadedRoots.value = []
  selectedRows.value = []
  combineTargetNo.value = ''
  combineFrom.value = []
  try { localStorage.removeItem(storageKey) } catch { /* In-memory reset still completed. */ }
  activeTab.value = 'unboxing'
  unboxingMethod.value = 0
  unboxInput.value = ''
  combineToInput.value = ''
  combineFromInput.value = ''
  autoCloseFull.value = false
  dialog.value = null
  labelPreview.value = false
  notice.value = 'DEV 假資料已重設。'
}
</script>

<template>
  <main class="iv310-page" aria-label="包裝拆並箱作業 DEV">
    <header class="page-header">
      <div>
        <p class="eyebrow">IV310 · 在製品庫</p>
        <h1>包裝拆並箱作業</h1>
        <p class="hint">依 MES 包裝層級與線上／線邊倉位置模擬拆箱、並箱及歷程。DEV 假資料。</p>
      </div>
      <div class="header-actions">
        <span class="dev-badge">DEV PREVIEW</span>
        <button type="button" class="button secondary" @click="resetDev">Reset DEV</button>
        <button type="button" class="button secondary" @click="emit('close')">Close (X)</button>
      </div>
    </header>

    <div class="notice" role="status">{{ notice }}</div>

    <nav class="tabs" aria-label="包裝作業模式">
      <button type="button" :class="{ active: activeTab === 'unboxing' }" @click="activeTab = 'unboxing'">拆箱</button>
      <button type="button" :class="{ active: activeTab === 'combine' }" @click="activeTab = 'combine'">並箱</button>
    </nav>

    <section v-if="activeTab === 'unboxing'" class="panel">
      <div class="toolbar">
        <label>Unboxing Method
          <select v-model.number="unboxingMethod">
            <option v-for="mode in modes" :key="mode.value" :value="mode.value">{{ mode.label }}</option>
          </select>
        </label>
        <label class="scan-field">Packing No
          <input v-model="unboxInput" list="iv310-pool-options" placeholder="輸入或選取包裝編號" @keydown.enter.prevent="scanUnboxing">
        </label>
        <datalist id="iv310-pool-options"><option v-for="no in poolOptions" :key="no" :value="no" /></datalist>
        <button type="button" class="button" @click="scanUnboxing">載入包裝</button>
        <span class="spacer"></span>
        <button type="button" class="button danger" :disabled="!loadedRoots.length" @click="clearUnboxing">Clear</button>
      </div>

      <div class="content-grid">
        <section class="subpanel">
          <div class="section-head">
            <h2>包裝階層</h2>
            <span>{{ loadedRoots.length }} 個來源</span>
          </div>
          <div v-if="!loadedRoots.length" class="empty">輸入頂層未封箱包裝編號並按 Enter 載入。</div>
          <article v-for="{ root } in loadedRoots" :key="root.packingNo" class="tree-card">
            <div class="tree-root">
              <strong>{{ root.packingNo }}</strong>
              <span>MO {{ root.moNo }} · L{{ root.level }} · {{ labels[root.packType] }} · {{ root.position }}</span>
              <button type="button" class="link-button" @click="deleteLoadedRoot(root.packingNo)">移除</button>
            </div>
            <div v-for="row in (unboxingMethod === 2 ? leaves(root) : root.children)" :key="root.packingNo + '|' + row.packingNo" class="tree-child">
              <input v-if="unboxingMethod === 1" v-model="selectedRows" type="checkbox" :value="root.packingNo + '|' + row.packingNo" :aria-label="'選取 ' + row.packingNo">
              <span class="level">{{ row.level === 0 ? 'Lot' : 'L' + row.level }}</span>
              <strong>{{ row.packingNo }}</strong>
              <span>{{ row.levelName }}</span>
              <span>箱數 {{ row.qty }}</span>
              <span>LotQty {{ row.lotQty }}</span>
              <span>{{ labels[row.packType] }}</span>
              <span>{{ row.position }}</span>
              <small v-if="row.level === 0">Status {{ row.lotStatus }}</small>
              <div v-for="lotRow in row.children.flatMap(leaves)" :key="lotRow.packingNo" class="tree-grandchild">
                <span>Lot</span><strong>{{ lotRow.packingNo }}</strong><span>LotQty {{ lotRow.lotQty }}</span><small>Status {{ lotRow.lotStatus }}</small>
              </div>
            </div>
          </article>
        </section>
        <aside class="subpanel summary">
          <h2>操作規則</h2>
          <p><b>單一階層：</b>拆出目前包裝的下一層。</p>
          <p><b>零散拆箱：</b>只處理勾選的下一層包裝，未選項目留在原包裝。</p>
          <p><b>拆到生產批：</b>展開並拆出所有 L0 Lot。</p>
          <p>線上包裝會檢查 Lot 狀態；線邊倉包裝維持倉儲位置，不模擬過站。</p>
          <p class="small-note">原 Form 的 Delete 只移除本次載入資料列；真正資料變更發生於 Confirm。</p>
        </aside>
      </div>
      <footer class="action-row">
        <span>{{ unboxingMethod === 1 ? '已勾選 ' + checkedCount + ' 項' : '待處理 ' + unboxRows.length + ' 項' }}</span>
        <button type="button" class="button secondary" :disabled="!loadedRoots.length" @click="clearUnboxing">Delete / Clear</button>
        <button type="button" class="button" :disabled="!loadedRoots.length" @click="openDismantleConfirm">Confirm Dismantle</button>
      </footer>
    </section>

    <section v-else class="panel">
      <div class="toolbar">
        <label>Combine To PackNo
          <input v-model="combineToInput" list="iv310-pool-options" placeholder="先輸入目標包裝" @keydown.enter.prevent="scanCombineTarget">
        </label>
        <button type="button" class="button" @click="scanCombineTarget">載入目標</button>
        <label class="scan-field">Combine From PackNo
          <input v-model="combineFromInput" list="iv310-pool-options" placeholder="逐筆輸入來源包裝" @keydown.enter.prevent="scanCombineSource">
        </label>
        <button type="button" class="button secondary" :disabled="!combineTarget" @click="scanCombineSource">加入來源</button>
      </div>

      <div class="content-grid combine-grid">
        <section class="subpanel">
          <div class="section-head">
            <h2>目標包裝</h2>
            <button v-if="combineTarget" type="button" class="link-button" @click="clearCombineTarget">清除目標</button>
          </div>
          <div v-if="!combineTarget" class="empty">先載入未封箱、未滿箱且未開立成品入庫單的目標包裝。</div>
          <template v-else>
            <div class="target-card">
              <strong>{{ combineTarget.packingNo }}</strong>
              <span>MO {{ combineTarget.moNo }} · L{{ combineTarget.level }}</span>
              <span>{{ labels[combineTarget.packType] }} · {{ combineTarget.position }}</span>
              <span>現有 {{ targetBoxes }} 箱 / {{ targetLotQty }} LotQty</span>
              <span>規則 {{ fullBoxQty || '—' }} 箱 / {{ fullLotQty || '—' }} LotQty</span>
            </div>
            <div v-for="row in combineTarget.children" :key="row.packingNo" class="tree-child">
              <strong>{{ row.packingNo }}</strong><span>L{{ row.level }}</span><span>{{ row.lotQty }} LotQty</span>
            </div>
          </template>
        </section>
        <section class="subpanel">
          <div class="section-head">
            <h2>來源包裝</h2>
            <span>{{ combineFrom.length }} 箱 · {{ sourceLotQty }} LotQty</span>
          </div>
          <div v-if="!combineFrom.length" class="empty">來源須與目標同工單、同位置，且恰好低一層。</div>
          <div v-for="row in combineFrom" :key="row.packingNo" class="tree-child">
            <strong>{{ row.packingNo }}</strong><span>MO {{ row.moNo }}</span><span>L{{ row.level }}</span><span>{{ row.lotQty }} LotQty</span>
            <button type="button" class="link-button" @click="removeCombineSource(row.packingNo)">移除</button>
          </div>
          <label class="checkline"><input v-model="autoCloseFull" type="checkbox"> Full Boxe Auto Closed</label>
        </section>
      </div>
      <footer class="action-row">
        <span>來源與目標驗證：工單／階層／位置／容量</span>
        <button type="button" class="button secondary" :disabled="!combineTarget" @click="clearCombineSources">Clear From</button>
        <button type="button" class="button" :disabled="!combineTarget || !combineFrom.length" @click="openCombineConfirm">Pack Close / Combine</button>
      </footer>
      <p class="small-note label-note">成功後舊 Form 會建立標籤資料並呼叫本機列印元件；DEV 只顯示標籤工作預覽，不呼叫 CodeSoft、BarTender 或印表機。</p>
    </section>

    <section class="history-panel">
      <div class="section-head"><h2>DEV 拆並箱歷程</h2><span>本機 localStorage 假資料</span></div>
      <div v-if="!state.operations.length" class="empty compact">尚無本機模擬紀錄。</div>
      <div v-for="(row, index) in state.operations.slice(0, 8)" :key="row.time + row.source + index" class="history-row">
        <time>{{ row.time }}</time><b>{{ row.type }}</b><span>{{ row.source }} → {{ row.destination }}</span><small>{{ row.detail }}</small>
      </div>
    </section>

    <div v-if="dialog" class="modal-backdrop" role="presentation">
      <section class="dialog" role="dialog" aria-modal="true" :aria-label="dialog.title">
        <h2>{{ dialog.title }}</h2>
        <p>{{ dialog.body }}</p>
        <div class="dialog-actions">
          <button type="button" class="button secondary" @click="dialog = null">取消</button>
          <button type="button" class="button" @click="dialog.action === 'dismantle' ? executeDismantle() : executeCombine()">確認</button>
        </div>
      </section>
    </div>
    <div v-if="labelPreview" class="modal-backdrop" role="presentation">
      <section class="dialog label-preview" role="dialog" aria-modal="true" aria-label="標籤工作預覽">
        <h2>DEV 標籤工作預覽</h2>
        <p>並箱後的標籤範本／印表機來自產品包裝規則。此預覽不送出列印工作。</p>
        <div class="label-ticket"><small>MES PACKING LABEL · DEV</small><strong>{{ state.operations[0]?.destination }}</strong><span>MO {{ findPool(state.operations[0]?.destination ?? '')?.moNo ?? 'MO-DEV-210' }}</span><span>假資料預覽</span></div>
        <div class="dialog-actions"><button type="button" class="button" @click="labelPreview = false">關閉</button></div>
      </section>
    </div>
  </main>
</template>

<style scoped>
.iv310-page { min-height: 100%; padding: 16px; box-sizing: border-box; background: #f5f4ee; color: #282720; font: inherit; overflow: auto; }
.page-header,.section-head,.toolbar,.action-row,.header-actions,.tree-root,.tree-child,.history-row,.dialog-actions { display: flex; align-items: center; gap: 10px; }
.page-header { justify-content: space-between; margin-bottom: 12px; }
.page-header h1 { margin: 2px 0 4px; font-size: 23px; }
.eyebrow { margin: 0; color: #777466; font-size: 11px; letter-spacing: .08em; text-transform: uppercase; }
.hint,.small-note { margin: 0; color: #6d6a5e; font-size: 12px; }
.dev-badge { padding: 4px 7px; border: 1px solid #986b1c; border-radius: 3px; color: #77520f; background: #fff3ce; font-size: 10px; font-weight: 700; }
.notice { margin: 10px 0; padding: 8px 10px; border-left: 3px solid #748c62; background: #e9eddf; font-size: 12px; }
.tabs { display: flex; gap: 3px; border-bottom: 1px solid #bdbbad; }
.tabs button { padding: 8px 20px; border: 1px solid transparent; background: transparent; color: #666256; font: inherit; cursor: pointer; }
.tabs button.active { border: 1px solid #bdbbad; border-bottom-color: #fff; border-radius: 4px 4px 0 0; background: #fff; color: #27261f; font-weight: 700; }
.panel,.history-panel { padding: 12px; border: 1px solid #bdbbad; border-top: 0; background: #fff; }
.history-panel { margin-top: 12px; border-top: 1px solid #bdbbad; }
.toolbar { flex-wrap: wrap; padding-bottom: 11px; border-bottom: 1px solid #e2e0d6; }
.toolbar label { display: grid; gap: 4px; min-width: 180px; color: #5a584e; font-size: 11px; }
.toolbar input,.toolbar select { min-height: 30px; padding: 3px 7px; border: 1px solid #bab8ab; border-radius: 3px; background: white; color: inherit; font: inherit; }
.toolbar .scan-field { min-width: 230px; }
.spacer { flex: 1; }
.button { min-height: 30px; padding: 4px 12px; border: 1px solid #46583b; border-radius: 3px; background: #596f4a; color: #fff; font: inherit; cursor: pointer; }
.button.secondary { border-color: #aaa797; background: #f2f1eb; color: #343229; }
.button.danger { border-color: #b8a7a0; color: #673a2c; background: #fff8f5; }
.button:disabled { opacity: .48; cursor: not-allowed; }
.content-grid { display: grid; grid-template-columns: minmax(0, 1.8fr) minmax(240px, .8fr); gap: 12px; padding-top: 12px; }
.combine-grid { grid-template-columns: 1fr 1fr; }
.subpanel { min-width: 0; padding: 10px; border: 1px solid #dedcd1; background: #fbfaf6; }
.section-head { justify-content: space-between; margin-bottom: 8px; color: #6c695c; font-size: 11px; }
.section-head h2,.summary h2,.dialog h2 { margin: 0; color: #302f28; font-size: 14px; }
.tree-card { margin: 8px 0; border: 1px solid #d6d4c8; background: white; }
.tree-root { padding: 8px; border-bottom: 1px solid #e2e0d6; }
.tree-root span { flex: 1; color: #666358; font-size: 11px; }
.tree-child { min-height: 31px; padding: 4px 8px 4px 20px; border-bottom: 1px solid #ecebe4; font-size: 11px; }
.tree-child:last-child { border-bottom: 0; }
.tree-child span { color: #69665a; }
.tree-child strong,.tree-root strong { color: #343228; }
.tree-child small { color: #7a6330; }
.tree-grandchild { display: flex; align-items: center; gap: 9px; width: 100%; padding: 4px 0 0 32px; color: #777366; font-size: 10px; }
.tree-grandchild strong { color: #565348; font-weight: 500; }
.level { min-width: 28px; color: #7b6940 !important; }
.link-button { padding: 3px 5px; border: 0; background: transparent; color: #655e42; text-decoration: underline; cursor: pointer; font: inherit; font-size: 11px; }
.summary p { margin: 10px 0; color: #5e5b50; font-size: 12px; line-height: 1.5; }
.small-note { padding-top: 7px; }
.action-row { justify-content: flex-end; padding-top: 12px; }
.action-row span { margin-right: auto; color: #777366; font-size: 11px; }
.empty { padding: 20px 10px; color: #858174; text-align: center; font-size: 12px; }
.empty.compact { padding: 10px; }
.target-card { display: grid; gap: 5px; padding: 10px; border: 1px solid #d4d1c3; background: #fff; font-size: 11px; }
.target-card strong { font-size: 15px; }
.target-card span { color: #615e52; }
.checkline { display: flex; align-items: center; gap: 7px; margin-top: 12px; color: #5e5b50; font-size: 11px; }
.label-note { padding-top: 10px; }
.history-row { padding: 7px 0; border-top: 1px solid #ecebe4; font-size: 11px; }
.history-row time,.history-row small { color: #777366; }
.history-row span { flex: 1; }
.modal-backdrop { position: fixed; z-index: 1000; inset: 0; display: grid; place-items: center; padding: 16px; background: #24231f7a; }
.dialog { width: min(480px, 95vw); padding: 18px; border: 1px solid #aaa797; border-radius: 5px; background: #fff; box-shadow: 0 12px 35px #0003; }
.dialog p { margin: 12px 0; color: #5d5a50; font-size: 13px; line-height: 1.55; }
.dialog-actions { justify-content: flex-end; margin-top: 14px; }
.label-ticket { display: grid; gap: 8px; width: min(260px, 100%); margin: 12px auto; padding: 16px; border: 1px dashed #898579; background: #fbfaf6; text-align: center; }
.label-ticket small,.label-ticket span { color: #6f6b5e; font-size: 11px; }
.label-ticket strong { font-size: 18px; letter-spacing: .08em; }
@media (max-width: 900px) { .content-grid,.combine-grid { grid-template-columns: 1fr; } .header-actions { flex-wrap: wrap; justify-content: flex-end; } .history-row { align-items: flex-start; flex-wrap: wrap; } }
</style>
