<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type ResourceRow = Record<string, any>
type AreaRow = { areaNo: string; areaName: string; factoryNo: string; defaultArea?: number }
type ProcessRow = { processNo: string; processVersion: string; psNo: string; psOrder: number; moTypeNo: string | number }
type OperationRow = { opNo: string; opName: string; processNo: string; processVersion: string; opOrder: number; areas: AreaRow[] }

const browserWindow = globalThis.window
const props = defineProps<{
  product: ResourceRow
  processes: ResourceRow[]
  opAreas: ResourceRow[] | null
  developmentPreview: boolean
  canEdit?: boolean
}>()
const emit = defineEmits<{ save: [rows: ResourceRow[]]; close: [] }>()

const working = ref<ResourceRow[]>([])
const activeOpNo = ref('')
const selectedOpNos = ref<string[]>([])
const definitionOpen = ref(false)
const editMode = ref(false)
const lookupMode = ref<'process' | 'operation' | null>(null)
const draftProcessNo = ref('')
const draftProcessVersion = ref('')
const draftOpNo = ref('')
const draftOpName = ref('')
const draftAreas = ref<AreaRow[]>([])
const leftSelected = ref<string[]>([])
const rightSelected = ref<string[]>([])
const leftSearch = ref('')
const rightSearch = ref('')
const error = ref('')
const notice = ref('')

const readOnly = computed(() => !props.developmentPreview || props.canEdit === false)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))

const devProcesses: ProcessRow[] = [
  { processNo: 'DEV-PROC-01', processVersion: '01', psNo: 'DEV-PS-10', psOrder: 10, moTypeNo: 1 },
]
const devOperations: OperationRow[] = [
  {
    opNo: 'DEV-OP-10', opName: '點膠作業', processNo: 'DEV-PROC-01', processVersion: '01', opOrder: 10,
    areas: [{ areaNo: 'DEV-AREA-01', areaName: '點膠區', factoryNo: 'DEV-FAB-01', defaultArea: 1 }],
  },
  {
    opNo: 'DEV-OP-20', opName: '組裝作業', processNo: 'DEV-PROC-01', processVersion: '01', opOrder: 20,
    areas: [{ areaNo: 'DEV-AREA-02', areaName: '組裝區', factoryNo: 'DEV-FAB-01', defaultArea: 1 }],
  },
  {
    opNo: 'DEV-OP-30', opName: '終檢作業', processNo: 'DEV-PROC-01', processVersion: '01', opOrder: 30,
    areas: [{ areaNo: 'DEV-AREA-03', areaName: '終檢區', factoryNo: 'DEV-FAB-02', defaultArea: 1 }],
  },
]
const devAreas: AreaRow[] = [
  { areaNo: 'DEV-AREA-01', areaName: '點膠區', factoryNo: 'DEV-FAB-01' },
  { areaNo: 'DEV-AREA-02', areaName: '組裝區', factoryNo: 'DEV-FAB-01' },
  { areaNo: 'DEV-AREA-03', areaName: '終檢區', factoryNo: 'DEV-FAB-02' },
  { areaNo: 'DEV-AREA-04', areaName: '待驗區', factoryNo: 'DEV-FAB-02' },
  { areaNo: 'DEV-AREA-05', areaName: '包裝區', factoryNo: 'DEV-FAB-02' },
]

const productProcesses = computed(() => {
  const source = props.processes.length ? props.processes : (props.developmentPreview ? devProcesses : [])
  return source.map(row => ({
    ...row,
    processNo: String(row.processNo ?? row.ProcessNo ?? ''),
    processVersion: String(row.processVersion ?? row.ProcessVersion ?? ''),
    psNo: String(row.psNo ?? row.PSNo ?? ''),
    psOrder: Number(row.psOrder ?? row.PSOrder ?? 0),
    moTypeNo: row.moTypeNo ?? row.MOTypeNo ?? '',
  })).filter(row => row.processNo)
})
const operationOptions = computed<OperationRow[]>(() => {
  if (!draftProcessNo.value) return []
  const source = props.developmentPreview ? devOperations : []
  return source.filter(row => row.processNo === draftProcessNo.value)
    .map(row => ({ ...row, processVersion: draftProcessVersion.value || row.processVersion }))
    .sort((a, b) => a.opOrder - b.opOrder)
})
const areaMaster = computed<AreaRow[]>(() => {
  const result = new Map<string, AreaRow>()
  for (const row of [...devAreas, ...working.value, ...draftAreas.value]) {
    const areaNo = String(row.areaNo ?? row.AreaNo ?? '')
    if (!areaNo) continue
    result.set(areaNo, {
      areaNo,
      areaName: String(row.areaName ?? row.AreaName ?? ''),
      factoryNo: String(row.factoryNo ?? row.FactoryNo ?? ''),
    })
  }
  return [...result.values()].sort((a, b) => a.areaNo.localeCompare(b.areaNo))
})
const mainRows = computed(() => {
  const operations = new Map<string, ResourceRow>()
  for (const row of working.value) {
    const opNo = String(row.opNo ?? row.OPNo ?? '')
    if (!opNo) continue
    if (!operations.has(opNo)) {
      operations.set(opNo, {
        opNo,
        opName: String(row.opName ?? row.OPName ?? operationName(opNo)),
        defaultArea: '',
      })
    }
    if (isDefault(row.defaultArea ?? row.DefaultArea)) {
      operations.get(opNo)!.defaultArea = String(row.areaNo ?? row.AreaNo ?? '')
    }
  }
  return [...operations.values()].sort((a, b) => a.opNo.localeCompare(b.opNo))
})
const activeSummary = computed(() => mainRows.value.find(row => row.opNo === activeOpNo.value) ?? null)
const detailRows = computed(() => working.value
  .filter(row => String(row.opNo ?? row.OPNo ?? '') === activeOpNo.value)
  .map(row => ({
    areaNo: String(row.areaNo ?? row.AreaNo ?? ''),
    areaName: String(row.areaName ?? row.AreaName ?? ''),
    factoryNo: String(row.factoryNo ?? row.FactoryNo ?? ''),
    defaultArea: isDefault(row.defaultArea ?? row.DefaultArea) ? 1 : 0,
  }))
  .sort((a, b) => b.areaNo.localeCompare(a.areaNo)))
const availableAreas = computed(() => areaMaster.value.filter(row => !draftAreas.value.some(item => item.areaNo === row.areaNo)))
const defaultCount = computed(() => draftAreas.value.filter(row => isDefault(row.defaultArea)).length)
const canConfirm = computed(() => !readOnly.value && Boolean(draftOpNo.value) && draftAreas.value.length > 0 && defaultCount.value === 1)

watch(() => props.opAreas, rows => {
  working.value = clone(Array.isArray(rows) ? rows : [])
  if (!mainRows.value.some(row => row.opNo === activeOpNo.value)) {
    activeOpNo.value = mainRows.value[0]?.opNo ?? ''
  }
  selectedOpNos.value = selectedOpNos.value.filter(opNo => mainRows.value.some(row => row.opNo === opNo))
}, { immediate: true, deep: true })

function clone<T>(value: T): T { return JSON.parse(JSON.stringify(value)) as T }
function isDefault(value: unknown): boolean { return value === true || Number(value) === 1 }
function operationName(opNo: string): string {
  return devOperations.find(row => row.opNo === opNo)?.opName ?? ''
}
function processName(row: ResourceRow): string {
  return String(row.processNo ?? '') + ' / ' + String(row.processVersion ?? '')
}
function selectOperationRow(row: ResourceRow, event: MouseEvent): void {
  activeOpNo.value = row.opNo
  if (event.ctrlKey || event.metaKey) {
    selectedOpNos.value = selectedOpNos.value.includes(row.opNo)
      ? selectedOpNos.value.filter(opNo => opNo !== row.opNo) : [...selectedOpNos.value, row.opNo]
  } else if (event.shiftKey && selectedOpNos.value.length) {
    const keys = mainRows.value.map(item => String(item.opNo))
    const from = keys.indexOf(selectedOpNos.value[selectedOpNos.value.length - 1])
    const to = keys.indexOf(row.opNo)
    selectedOpNos.value = keys.slice(Math.min(from, to), Math.max(from, to) + 1)
  } else selectedOpNos.value = [row.opNo]
}
function openDefinition(row?: ResourceRow): void {
  if (readOnly.value) return
  error.value = ''
  notice.value = ''
  editMode.value = Boolean(row)
  draftOpNo.value = row ? String(row.opNo ?? '') : ''
  draftOpName.value = row ? String(row.opName ?? operationName(String(row.opNo ?? ''))) : ''
  const operation = devOperations.find(item => item.opNo === draftOpNo.value)
  const process = productProcesses.value.find(item => item.processNo === operation?.processNo)
  draftProcessNo.value = process?.processNo ?? operation?.processNo ?? ''
  draftProcessVersion.value = process?.processVersion ?? operation?.processVersion ?? ''
  draftAreas.value = row
    ? clone(working.value.filter(item => String(item.opNo ?? item.OPNo ?? '') === String(row.opNo)))
        .map(item => ({
          areaNo: String(item.areaNo ?? item.AreaNo ?? ''),
          areaName: String(item.areaName ?? item.AreaName ?? ''),
          factoryNo: String(item.factoryNo ?? item.FactoryNo ?? ''),
          defaultArea: isDefault(item.defaultArea ?? item.DefaultArea) ? 1 : 0,
        }))
    : []
  leftSelected.value = []
  rightSelected.value = []
  leftSearch.value = ''
  rightSearch.value = ''
  lookupMode.value = null
  definitionOpen.value = true
}
function chooseProcess(row: ResourceRow): void {
  draftProcessNo.value = String(row.processNo ?? '')
  draftProcessVersion.value = String(row.processVersion ?? '')
  draftOpNo.value = ''
  draftOpName.value = ''
  draftAreas.value = []
  leftSelected.value = []
  rightSelected.value = []
  lookupMode.value = null
  error.value = ''
}
function chooseOperation(row: OperationRow): void {
  draftOpNo.value = row.opNo
  draftOpName.value = row.opName
  const existing = working.value.filter(item => String(item.opNo ?? item.OPNo ?? '') === row.opNo)
  if (existing.length) {
    draftAreas.value = existing.map(item => ({
      areaNo: String(item.areaNo ?? item.AreaNo ?? ''),
      areaName: String(item.areaName ?? item.AreaName ?? ''),
      factoryNo: String(item.factoryNo ?? item.FactoryNo ?? ''),
      defaultArea: isDefault(item.defaultArea ?? item.DefaultArea) ? 1 : 0,
    }))
  } else {
    draftAreas.value = clone(row.areas)
  }
  leftSelected.value = []
  rightSelected.value = []
  lookupMode.value = null
  error.value = ''
}
function openOperationLookup(): void {
  if (readOnly.value || editMode.value) return
  if (!productProcesses.value.length) {
    error.value = '此產品版本沒有可使用的流程，或目前登入者沒有流程權限。'
    return
  }
  lookupMode.value = 'process'
}
function moveToSelected(areaNos: string[] = leftSelected.value): void {
  if (readOnly.value) return
  const moving = availableAreas.value.filter(row => areaNos.includes(row.areaNo))
  draftAreas.value = [...draftAreas.value, ...moving.map(row => ({ ...row, defaultArea: 0 }))]
  leftSelected.value = []
}
function moveToAvailable(areaNos: string[] = rightSelected.value): void {
  if (readOnly.value) return
  draftAreas.value = draftAreas.value.filter(row => !areaNos.includes(row.areaNo))
  rightSelected.value = []
}
function moveExact(side: 'available' | 'selected'): void {
  const query = (side === 'available' ? leftSearch.value : rightSearch.value).trim().toLocaleLowerCase()
  if (!query) return
  if (side === 'available') {
    const row = availableAreas.value.find(item => item.areaNo.toLocaleLowerCase() === query)
    if (row) { moveToSelected([row.areaNo]); leftSearch.value = '' }
  } else {
    const row = draftAreas.value.find(item => item.areaNo.toLocaleLowerCase() === query)
    if (row) { moveToAvailable([row.areaNo]); rightSearch.value = '' }
  }
}
function toggleDefault(areaNo: string, event: Event): void {
  if (readOnly.value) return
  const checked = (event.target as HTMLInputElement).checked
  const row = draftAreas.value.find(item => item.areaNo === areaNo)
  if (row) row.defaultArea = checked ? 1 : 0
}
function confirmDefinition(): void {
  if (readOnly.value) return
  if (!draftOpNo.value) { error.value = '請選擇作業站。'; return }
  if (!draftAreas.value.length) { error.value = '請至少選擇一個作業區域。'; return }
  if (defaultCount.value === 0) { error.value = '請選擇一個預設區域。'; return }
  if (defaultCount.value > 1) { error.value = '只能設定一個預設區域。'; return }

  const retained = working.value.filter(item => String(item.opNo ?? item.OPNo ?? '') !== draftOpNo.value)
  const assigned = draftAreas.value.map(area => ({
    productNo: productNo.value,
    productVersion: productVersion.value,
    opNo: draftOpNo.value,
    areaNo: area.areaNo,
    defaultArea: isDefault(area.defaultArea) ? 1 : 0,
    areaName: area.areaName,
    factoryNo: area.factoryNo,
  }))
  working.value = [...retained, ...assigned]
  emit('save', clone(working.value))
  activeOpNo.value = draftOpNo.value
  selectedOpNos.value = [draftOpNo.value]
  definitionOpen.value = false
  notice.value = 'DEV 產品作業區域假資料已儲存。'
  error.value = ''
}
function deleteSelected(): void {
  if (readOnly.value) return
  const opNos = selectedOpNos.value.length ? selectedOpNos.value : activeOpNo.value ? [activeOpNo.value] : []
  if (!opNos.length) { error.value = '請先選取作業站資料。'; return }
  if (!browserWindow.confirm('確定刪除選取作業站的全部產品區域設定？')) return
  working.value = working.value.filter(row => !opNos.includes(String(row.opNo ?? row.OPNo ?? '')))
  emit('save', clone(working.value))
  selectedOpNos.value = []
  activeOpNo.value = mainRows.value[0]?.opNo ?? ''
  error.value = ''
  notice.value = '已刪除選取作業站的產品區域假資料。'
}
</script>

<template>
  <section class="prd-resource-window" role="dialog" aria-modal="true" aria-label="Product OP Area">
    <header class="prd-resource-title"><span>Product OP Area</span><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <p v-if="!developmentPreview" class="prd-resource-message">Production API 尚未接線；正式資料唯讀。</p>
    <p v-else-if="canEdit === false" class="prd-resource-message">產品狀態不是未核准；依 WinForms 規則唯讀。</p>
    <p v-if="notice" class="prd-resource-message">{{ notice }}</p>
    <p v-if="error && !definitionOpen" class="prd-resource-message is-error">{{ error }}</p>

    <main class="prd-resource-main">
      <section class="prd-resource-panel prd-resource-op-panel">
        <legend>Product OP</legend>
        <div class="prd-resource-panel-content">
          <div class="prd-resource-grid-wrap">
            <table class="prd-resource-grid">
              <thead><tr><th>OPNo</th></tr></thead>
              <tbody>
                <tr v-for="row in mainRows" :key="row.opNo" :class="{ selected: selectedOpNos.includes(row.opNo), active: activeOpNo === row.opNo }" @click="selectOperationRow(row, $event)">
                  <td>{{ row.opNo }}</td>
                </tr>
                <tr v-if="!mainRows.length"><td class="prd-resource-empty">此產品版本尚未設定作業區域。</td></tr>
              </tbody>
            </table>
          </div>
          <aside class="prd-resource-actions">
            <button class="prd-resource-button confirm" type="button" :disabled="readOnly" @click="openDefinition()">ADD (&amp;A)</button>
            <button class="prd-resource-button" type="button" :disabled="readOnly || !activeSummary" @click="openDefinition(activeSummary ?? undefined)">EDIT (&amp;E)</button>
            <button class="prd-resource-button" type="button" :disabled="readOnly || (!selectedOpNos.length && !activeOpNo)" @click="deleteSelected">DEL (&amp;D)</button>
          </aside>
        </div>
      </section>

      <section class="prd-resource-panel prd-resource-area-panel">
        <legend>Area</legend>
        <div class="prd-resource-panel-content">
          <div class="prd-resource-grid-wrap">
            <table class="prd-resource-grid">
              <thead><tr><th>AreaNo</th><th>DefaultArea</th></tr></thead>
              <tbody>
                <tr v-for="row in detailRows" :key="row.areaNo"><td>{{ row.areaNo }}</td><td>{{ row.defaultArea ? 'Yes' : 'No' }}</td></tr>
                <tr v-if="activeSummary && !detailRows.length"><td colspan="2" class="prd-resource-empty">沒有區域明細。</td></tr>
                <tr v-if="!activeSummary"><td colspan="2" class="prd-resource-empty">請先選取上方作業站。</td></tr>
              </tbody>
            </table>
          </div>
          <aside class="prd-resource-actions prd-resource-bottom-actions">
            <button class="prd-resource-button" type="button" @click="emit('close')">Close (&amp;X)</button>
          </aside>
        </div>
      </section>
    </main>

    <div v-if="definitionOpen" class="prd-resource-modal-backdrop">
      <section class="prd-resource-definition" role="dialog" aria-modal="true" aria-label="Product OP Area 定義">
        <header class="prd-resource-title"><span>{{ editMode ? 'Product OP Area Edit' : 'Product OP Area Add' }}</span><button type="button" aria-label="關閉" @click="definitionOpen = false">×</button></header>
        <div class="prd-resource-fields">
          <template v-if="!editMode">
            <label for="prd-resource-process">Process No</label>
            <input id="prd-resource-process" class="prd-resource-input" :value="draftProcessNo" readonly>
            <input class="prd-resource-input prd-resource-version" aria-label="Process Version" :value="draftProcessVersion" readonly>
            <button class="prd-resource-lookup" type="button" @click="openOperationLookup">...</button>
          </template>
          <label for="prd-resource-op" :class="{ 'prd-resource-op-edit-label': editMode }">OP No</label>
          <input id="prd-resource-op" class="prd-resource-input" :class="{ 'prd-resource-op-edit-input': editMode }" :value="draftOpNo" readonly>
          <button class="prd-resource-lookup" type="button" :disabled="editMode || !draftProcessNo" @click="lookupMode = 'operation'">...</button>
          <span class="prd-resource-op-name">{{ draftOpName }}</span>
        </div>

        <fieldset class="prd-resource-area-map">
          <legend>OP Area Map</legend>
          <div class="prd-resource-area-columns">
            <section class="prd-resource-area-list">
              <h3>未選區域</h3>
              <div class="prd-resource-grid-wrap">
                <table class="prd-resource-grid">
                  <thead><tr><th>AreaNo</th><th>AreaName</th><th>FactoryNo</th></tr></thead>
                  <tbody>
                    <tr v-for="row in availableAreas" :key="row.areaNo" :class="{ selected: leftSelected.includes(row.areaNo) }" @click="leftSelected = leftSelected.includes(row.areaNo) ? leftSelected.filter(value => value !== row.areaNo) : [...leftSelected, row.areaNo]" @dblclick="moveToSelected([row.areaNo])">
                      <td>{{ row.areaNo }}</td><td>{{ row.areaName }}</td><td>{{ row.factoryNo }}</td>
                    </tr>
                    <tr v-if="!availableAreas.length"><td colspan="3" class="prd-resource-empty">沒有可加入的區域。</td></tr>
                  </tbody>
                </table>
              </div>
              <input v-model="leftSearch" class="prd-resource-search" aria-label="搜尋未選區域" placeholder="輸入 AreaNo 後按 Enter" @keydown.enter.prevent="moveExact('available')">
            </section>

            <div class="prd-resource-move">
              <button type="button" :disabled="readOnly || !leftSelected.length" aria-label="加入已選區域" @click="moveToSelected()">&#9654;</button>
              <button type="button" :disabled="readOnly || !rightSelected.length" aria-label="移回未選區域" @click="moveToAvailable()">&#9664;</button>
            </div>

            <section class="prd-resource-area-list">
              <h3>已選區域 <span>勾選一個預設區域</span></h3>
              <div class="prd-resource-grid-wrap">
                <table class="prd-resource-grid">
                  <thead><tr><th>Select</th><th>AreaNo</th><th>AreaName</th><th>FactoryNo</th></tr></thead>
                  <tbody>
                    <tr v-for="row in draftAreas" :key="row.areaNo" :class="{ selected: rightSelected.includes(row.areaNo) }" @click="rightSelected = rightSelected.includes(row.areaNo) ? rightSelected.filter(value => value !== row.areaNo) : [...rightSelected, row.areaNo]" @dblclick="moveToAvailable([row.areaNo])">
                      <td @click.stop><input type="checkbox" :checked="isDefault(row.defaultArea)" :disabled="readOnly" :aria-label="'設為預設區域 ' + row.areaNo" @change="toggleDefault(row.areaNo, $event)"></td>
                      <td>{{ row.areaNo }}</td><td>{{ row.areaName }}</td><td>{{ row.factoryNo }}</td>
                    </tr>
                    <tr v-if="!draftAreas.length"><td colspan="4" class="prd-resource-empty">請從左側選擇區域。</td></tr>
                  </tbody>
                </table>
              </div>
              <input v-model="rightSearch" class="prd-resource-search" aria-label="搜尋已選區域" placeholder="輸入 AreaNo 後按 Enter" @keydown.enter.prevent="moveExact('selected')">
            </section>
          </div>
        </fieldset>

        <p v-if="error" class="prd-resource-message is-error">{{ error }}</p>
        <footer class="prd-resource-definition-footer">
          <button class="prd-resource-button confirm" type="button" :disabled="!canConfirm" @click="confirmDefinition">Confirm</button>
          <button class="prd-resource-button" type="button" @click="definitionOpen = false">Close</button>
        </footer>

        <div v-if="lookupMode" class="prd-resource-lookup-backdrop" @click.self="lookupMode = null">
          <section class="prd-resource-lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupMode === 'process' ? '選擇流程' : '選擇作業站'">
            <header><strong>{{ lookupMode === 'process' ? '選擇產品流程' : '選擇流程作業站' }}</strong><button type="button" aria-label="關閉查找" @click="lookupMode = null">×</button></header>
            <table v-if="lookupMode === 'process'" class="prd-resource-grid">
              <thead><tr><th>PSNo</th><th>PSOrder</th><th>ProcessNo</th><th>ProcessVersion</th><th>MOTypeNo</th></tr></thead>
              <tbody><tr v-for="row in productProcesses" :key="processName(row)" @click="chooseProcess(row)"><td>{{ row.psNo }}</td><td>{{ row.psOrder }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ row.moTypeNo }}</td></tr></tbody>
            </table>
            <table v-else class="prd-resource-grid">
              <thead><tr><th>OPNo</th><th>OPName</th><th>ProcessNo</th><th>ProcessVersion</th></tr></thead>
              <tbody><tr v-for="row in operationOptions" :key="row.opNo" @click="chooseOperation(row)"><td>{{ row.opNo }}</td><td>{{ row.opName }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td></tr></tbody>
            </table>
            <p v-if="lookupMode === 'operation' && !operationOptions.length" class="prd-resource-empty">此流程沒有可選的作業站節點。</p>
            <p v-if="developmentPreview" class="prd-resource-dev-note">選項為 DEV 假資料，不代表正式 MES 查找結果。</p>
          </section>
        </div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.prd-resource-window{position:relative;display:flex;flex-direction:column;width:min(1008px,96vw);height:min(591px,calc(100vh - 30px));min-height:390px;background:#fff;border:1px solid #aaa79c;box-shadow:0 12px 36px #0005;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}
.prd-resource-title{display:flex;align-items:center;justify-content:space-between;min-height:34px;padding:3px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8;color:#333}
.prd-resource-title button{width:26px;height:26px;border:0;background:transparent;font-size:20px;color:#555;cursor:pointer}
.prd-resource-message{margin:5px 10px 0;padding:4px 7px;border:1px solid #e4d7a3;background:#fff8df;color:#605326}
.prd-resource-message.is-error{border-color:#d5a59b;background:#fff0ed;color:#7a261c}
.prd-resource-main{display:flex;flex-direction:column;gap:3px;flex:1;min-height:0}
.prd-resource-panel{position:relative;min-height:0;margin:0 0 0;border:1px solid #aaa79c;padding:9px 8px 7px}
.prd-resource-panel>legend{position:absolute;top:-7px;left:10px;padding:0 5px;background:#fff;color:#484538}
.prd-resource-op-panel{height:240px;flex:0 0 240px}
.prd-resource-area-panel{flex:1}
.prd-resource-panel-content{height:100%;display:grid;grid-template-columns:minmax(0,1fr) 80px;gap:7px}
.prd-resource-grid-wrap{min-height:0;overflow:auto;border:1px solid #aaa79c}
.prd-resource-grid{width:100%;border-collapse:collapse;table-layout:auto}
.prd-resource-grid th{position:sticky;top:0;z-index:1;background:#f1d982;border:1px solid #c6b983;padding:5px 6px;text-align:left;font-weight:600;white-space:nowrap}
.prd-resource-grid td{height:22px;border:1px solid #d3d0c7;padding:3px 6px;white-space:nowrap}
.prd-resource-grid tbody tr{cursor:pointer}
.prd-resource-grid tbody tr.selected{background:#fff0b0}
.prd-resource-grid tbody tr.active{outline:1px solid #bf982e;outline-offset:-1px}
.prd-resource-empty{text-align:center;color:#777;padding:12px!important}
.prd-resource-actions{display:flex;flex-direction:column;gap:7px}
.prd-resource-bottom-actions{justify-content:flex-end}
.prd-resource-button{min-height:30px;padding:4px 7px;border:1px solid #aaa79c;border-radius:3px;background:linear-gradient(#fff,#f1efe9);color:#292820;font:inherit;white-space:nowrap;cursor:pointer}
.prd-resource-button:hover:not(:disabled),.prd-resource-lookup:hover:not(:disabled){border-color:#c49a35;background:#fff5d6}
.prd-resource-button.confirm{background:#f5a62a;border-color:#dc941d;color:#fff;font-weight:600}
.prd-resource-button:disabled,.prd-resource-lookup:disabled{opacity:.5;cursor:not-allowed}
.prd-resource-modal-backdrop{position:absolute;inset:0;z-index:2;display:grid;place-items:center;background:#0005}
.prd-resource-definition{position:relative;display:flex;flex-direction:column;width:min(784px,96vw);height:min(561px,calc(100vh - 36px));min-height:360px;background:#fff;border:1px solid #aaa79c;box-shadow:0 12px 36px #0005}
.prd-resource-fields{display:grid;grid-template-columns:72px minmax(80px,151px) 49px 28px 72px minmax(100px,144px) 28px 1fr;align-items:center;gap:7px 4px;padding:10px 12px}
.prd-resource-fields label{text-align:right}
.prd-resource-input{height:24px;min-width:0;padding:3px 6px;border:1px solid #888;background:#fff0b0;color:#292820;font:inherit}
.prd-resource-version{text-align:center}
.prd-resource-lookup{height:25px;border:1px solid #aaa79c;background:#f8f7f3;cursor:pointer}
.prd-resource-op-name{color:#666;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.prd-resource-op-edit-label{grid-column:1}
.prd-resource-op-edit-input{grid-column:2}
.prd-resource-area-map{display:flex;flex-direction:column;flex:1;min-height:0;margin:0 14px 8px;padding:6px 8px 8px;border:1px solid #aaa79c}
.prd-resource-area-map>legend{padding:0 5px;color:#484538}
.prd-resource-area-columns{display:grid;grid-template-columns:minmax(0,1fr) 44px minmax(0,1fr);gap:8px;flex:1;min-height:0}
.prd-resource-area-list{display:flex;flex-direction:column;min-width:0;min-height:0}
.prd-resource-area-list h3{margin:0;padding:4px 6px;border:1px solid #d4d1c8;background:#f5f4f0;font-size:12px}
.prd-resource-area-list h3 span{float:right;color:#777;font-weight:400}
.prd-resource-area-list .prd-resource-grid-wrap{flex:1}
.prd-resource-search{height:24px;margin-top:5px;padding:3px 6px;border:1px solid #999;background:#fff0b0;font:inherit}
.prd-resource-grid input[type=checkbox]{accent-color:#b4831d}
.prd-resource-move{display:flex;flex-direction:column;justify-content:center;gap:8px}
.prd-resource-move button{height:32px;border:1px solid #aaa79c;background:linear-gradient(#fff,#f1efe9);cursor:pointer}
.prd-resource-move button:disabled{opacity:.5;cursor:not-allowed}
.prd-resource-definition-footer{display:flex;justify-content:flex-end;gap:6px;padding:0 12px 10px}
.prd-resource-definition-footer .prd-resource-button{min-width:80px;height:32px}
.prd-resource-lookup-backdrop{position:absolute;inset:34px 0 0;z-index:3;display:grid;place-items:center;background:#0005}
.prd-resource-lookup-dialog{width:min(670px,94vw);max-height:80%;overflow:auto;border:1px solid #aaa79c;background:#fff;box-shadow:0 12px 35px #0005}
.prd-resource-lookup-dialog>header{display:flex;justify-content:space-between;align-items:center;padding:7px 10px;background:#f5f4f0;border-bottom:1px solid #d4d1c8}
.prd-resource-lookup-dialog>header button{border:0;background:none;font-size:18px;cursor:pointer}
.prd-resource-lookup-dialog .prd-resource-grid tbody tr:hover{background:#fff0b0}
.prd-resource-dev-note{margin:0;padding:6px 8px;border-top:1px solid #d4d1c8;background:#fff8df;color:#605326}
@media(max-width:700px){.prd-resource-window{height:calc(100vh - 20px)}.prd-resource-op-panel{height:35%;flex-basis:35%}.prd-resource-panel-content{grid-template-columns:minmax(0,1fr)}.prd-resource-actions{flex-direction:row;flex-wrap:wrap}.prd-resource-area-columns{grid-template-columns:minmax(0,1fr) 36px minmax(0,1fr);gap:4px}.prd-resource-fields{grid-template-columns:70px minmax(70px,1fr) 40px 28px}.prd-resource-op-name{grid-column:2 / -1}.prd-resource-definition{overflow:auto}}
</style>
