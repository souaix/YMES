<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

interface Lot {
  no: string
  status: 0 | 4
  currentNode: string
  area: string
  factory: string
  stamp: number
  overrides: Record<string, string>
}

const areas = new Map([['A01', 'F1'], ['A02', 'F1'], ['A03', 'F2'], ['T01', 'F2'], ['T02', 'F2']])
const nodes = [
  { id: 'N1', flow: 'MAIN', op: 'WB', label: '打線', defaultArea: 'A01', choices: ['A01', 'A02', 'A03'] },
  { id: 'N2', flow: 'MAIN', op: 'FT', label: '最終測試', defaultArea: 'T01', choices: ['T01', 'T02'] },
  { id: 'N3', flow: 'SUB', op: 'FT', label: '重測', defaultArea: 'T02', choices: ['T01', 'T02'] },
  { id: 'N4', flow: 'SUB', op: 'FT', label: '停用節點', defaultArea: 'T02', choices: ['T01', 'T02'], removed: true },
]
const lots = ref<Lot[]>([
  { no: 'DEV-AREA001', status: 0, currentNode: 'N1', area: 'A01', factory: 'F1', stamp: 3, overrides: {} },
  { no: 'DEV-AREA002', status: 4, currentNode: 'N2', area: 'T01', factory: 'F2', stamp: 6, overrides: { N3: 'T01' } },
])
const lotInput = ref('')
const loadedNo = ref('')
const flow = ref('MAIN')
const nodeId = ref('')
const plannedArea = ref('')
const overrides = ref<Record<string, string>>({})
const loaded = computed(() => lots.value.find(lot => lot.no === loadedNo.value))
const currentNode = computed(() => nodes.find(node => node.id === nodeId.value))
const visible = computed(() => nodes.filter(node => node.flow === flow.value))
const dirty = computed(() => !!loaded.value && nodes.some(node => loaded.value?.overrides[node.id] !== overrides.value[node.id]))
const pending = ref<'save' | 'load' | 'close'>()
const confirmation = ref<string | null>(null)
const error = ref('')
const result = ref('')

function loadLot(): void {
  const next = lots.value.find(lot => lot.no === lotInput.value.trim().toUpperCase())
  if (!next) {
    error.value = '查無排隊或完成狀態的生產批。'
    return
  }
  lotInput.value = loadedNo.value = next.no
  overrides.value = { ...next.overrides }
  selectFlow('MAIN')
  error.value = result.value = ''
}

function selectFlow(value: string): void {
  flow.value = value
  selectNode('')
}

function selectNode(id: string): void {
  nodeId.value = id
  plannedArea.value = overrides.value[id] || ''
  error.value = ''
}

function setOverride(): void {
  const node = currentNode.value
  if (!node || node.removed) return
  if (!node.choices.includes(plannedArea.value) || plannedArea.value === node.defaultArea) {
    error.value = '請選擇與預設作業區不同的有效作業區。'
    return
  }
  overrides.value[node.id] = plannedArea.value
  error.value = ''
}

function clearOverride(): void {
  delete overrides.value[nodeId.value]
  plannedArea.value = ''
  error.value = ''
}

function request(action: typeof pending.value): void {
  if (action === 'save' && !dirty.value) return
  if (action !== 'save' && !dirty.value) {
    if (action === 'load') loadLot()
    if (action === 'close') emit('close')
    return
  }
  pending.value = action
  confirmation.value = action === 'save' ? `儲存 ${loadedNo.value} 的作業區變更？` : '捨棄尚未儲存的作業區變更？'
}

function resolve(accepted: boolean): void {
  const action = pending.value
  confirmation.value = null
  pending.value = undefined
  if (!accepted) return
  if (action === 'load') return loadLot()
  if (action === 'close') return emit('close')
  const lot = loaded.value
  if (action !== 'save' || !lot) return
  const area = overrides.value[lot.currentNode]
  if (lot.status === 0 && area && area !== lot.area) {
    lot.area = area
    lot.factory = areas.get(area) ?? lot.factory
    lot.stamp++
  }
  lot.overrides = { ...overrides.value }
  result.value = '已儲存 DEV 作業區變更。'
}
</script>

<template>
  <section class="area-change" aria-labelledby="area-change-title">
    <h2 id="area-change-title">生產批作業區變更</h2>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="request('load')">
        <label>生產批號 <input v-model="lotInput" list="area-change-lots" maxlength="50"></label>
        <datalist id="area-change-lots"><option v-for="lot in lots" :key="lot.no" :value="lot.no" /></datalist>
        <button type="submit">查詢</button>
        <button type="button" :disabled="!dirty" @click="request('save')">儲存</button>
        <button type="button" @click="request('close')">關閉</button>
      </form>
      <template v-if="loaded">
        <p>{{ loaded.no }} · {{ loaded.status === 0 ? '排隊' : '完成' }} · 目前作業區 {{ loaded.area }} · 廠別 {{ loaded.factory }}</p>
        <label>流程
          <select :value="flow" @change="selectFlow(($event.target as HTMLSelectElement).value)">
            <option value="MAIN">DEV-MAIN / A</option><option value="SUB">DEV-SUB / A</option>
          </select>
        </label>
        <ol class="flow" aria-label="作業站">
          <li v-for="node in visible" :key="node.id">
            <button type="button" :disabled="node.removed" :aria-pressed="nodeId === node.id" @click="selectNode(node.id)">
              <strong>{{ node.op }} {{ node.label }}</strong>
              <span>預設 {{ node.defaultArea }}</span>
              <span v-if="overrides[node.id]">變更 {{ overrides[node.id] }}</span>
            </button>
          </li>
        </ol>
        <form v-if="currentNode" class="toolbar" @submit.prevent="setOverride">
          <label>變更作業區
            <select v-model="plannedArea"><option value="">請選擇</option><option v-for="area in currentNode.choices" :key="area">{{ area }}</option></select>
          </label>
          <button type="submit">套用</button>
          <button type="button" :disabled="!overrides[nodeId]" @click="clearOverride">移除變更</button>
        </form>
      </template>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolve" />
    </template>
    <template v-else>
      <p>生產批作業區變更 API 待接續。</p>
      <button type="button" @click="emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.area-change { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p, ol { margin: 0; }
.toolbar, label, .flow { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.flow { list-style: none; padding: 0; }
.flow button { display: grid; gap: 5px; min-width: 144px; padding: 8px; text-align: left; }
.flow button[aria-pressed="true"] { background: #fff6c5; }
.flow li + li::before { content: '→'; align-self: center; }
.flow li { display: flex; gap: 8px; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
