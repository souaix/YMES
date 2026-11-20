<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()

interface ReverseStep {
  id: number
  type: number
  op: string
  description: string
  createdAt: string
  committed?: boolean
}

interface Lot {
  no: string
  status: number
  stamp: number
  packaged?: boolean
  steps: ReverseStep[]
}

const steps: ReverseStep[] = [
  { id: 3, type: 0, op: 'FT', description: '測試站進站', createdAt: '2026-11-20 10:20:00' },
  { id: 2, type: 9, op: 'FT', description: '解除等待', createdAt: '2026-11-20 10:10:00' },
  { id: 1, type: 0, op: 'WB', description: '打線站出站', createdAt: '2026-11-20 09:30:00' },
]
const lots = ref<Lot[]>([
  { no: 'DEV-RV001', status: 1, stamp: 3, steps },
  { no: 'DEV-RV002', status: 3, stamp: 2, packaged: true, steps },
  { no: 'DEV-RV003', status: 2, stamp: 3, steps: steps.map(step => ({ ...step, committed: step.id === 2 })) },
  { no: 'DEV-RV004', status: 1, stamp: 3, steps: steps.map(step => step.id === 3 ? { ...step, type: 2, description: '分批轉入' } : step) },
])
const typeNames: Record<number, string> = { 0: 'Trigger OP', 2: 'Split_To', 9: 'Wait_Release' }
const singleStepTypes = new Set([1, 2, 3, 4, 10, 21])
const candidateStatuses = new Set([0, 1, 2, 3, 11, 4])
const candidates = computed(() => lots.value.filter(lot => candidateStatuses.has(lot.status)))
const lotInput = ref('')
const loaded = ref<{ no: string; stamp: number }>()
const selectedId = ref<number>()
const reason = ref('')
const confirmation = ref<string | null>(null)
const message = ref('')
const error = ref('')
const currentLot = computed(() => lots.value.find(lot => lot.no === loaded.value?.no))
const selected = computed(() => currentLot.value?.steps.find(step => step.id === selectedId.value))

function loadLot(): void {
  const lot = candidates.value.find(candidate => candidate.no === lotInput.value.trim().toUpperCase())
  loaded.value = lot && { no: lot.no, stamp: lot.stamp }
  selectedId.value = undefined
  reason.value = ''
  message.value = ''
  error.value = lot ? '' : '查無可回復的生產批。'
  if (lot) lotInput.value = lot.no
}

function validate(lot: Lot, step: ReverseStep): string {
  if (lot.no !== lotInput.value.trim().toUpperCase() || lot.stamp !== loaded.value?.stamp) return '批號或資料版本已變更，請重新查詢。'
  if (lot.packaged) return '此批已包裝，無法回復。'
  const affected = lot.steps.filter(row => row.id >= step.id)
  const committed = affected.find(row => row.committed)
  if (committed) return `歷程 ${committed.id} 已提交，無法回復。`
  if (affected.some(row => row.id > step.id && singleStepTypes.has(row.type))) return '分批、併批、批次及改批號等歷程須逐筆回復。'
  if (singleStepTypes.has(step.type)) return '此 DEV 範例未模擬關聯生產批的回復。'
  return ''
}

function requestReverse(): void {
  const lot = currentLot.value
  const step = selected.value
  message.value = ''
  if (!lot || !step) {
    error.value = '請選取回復歷程。'
    return
  }
  error.value = validate(lot, step)
  if (!error.value) confirmation.value = `回復生產批 ${lot.no} 至歷程 ${step.id}？`
}

function resolveReverse(accepted: boolean): void {
  confirmation.value = null
  const lot = currentLot.value
  const step = selected.value
  if (!accepted || !lot || !step) return
  error.value = validate(lot, step)
  if (error.value) return
  lot.steps = lot.steps.filter(row => row.id < step.id)
  lot.stamp++
  loaded.value = lot.steps.length ? { no: lot.no, stamp: lot.stamp } : undefined
  if (!loaded.value) lotInput.value = ''
  selectedId.value = undefined
  const note = reason.value.trim()
  message.value = `DEV 已回復 ${lot.no} 至歷程 ${step.id}${note ? `（${note}）` : ''}。`
  reason.value = ''
}
</script>

<template>
  <section class="reverse-page" aria-labelledby="lot-reverse-title">
    <h2 id="lot-reverse-title">回復處理</h2>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="loadLot">
        <label>生產批號 <input v-model="lotInput" list="reverse-dev-lots" maxlength="50" autocomplete="off"></label>
        <datalist id="reverse-dev-lots"><option v-for="lot in candidates" :key="lot.no" :value="lot.no" /></datalist>
        <button type="submit">查詢</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div class="grid">
        <table aria-label="回復歷程">
          <thead>
            <tr><th scope="col">選取</th><th scope="col">回復序號</th><th scope="col">類型</th><th scope="col">作業站</th><th scope="col">批序號</th><th scope="col">說明</th><th scope="col">建立人</th><th scope="col">建立時間</th><th scope="col">已提交</th><th scope="col">提交說明</th></tr>
          </thead>
          <tbody>
            <tr v-for="step in currentLot?.steps" :key="step.id" :class="{ selected: selectedId === step.id }">
              <td><input v-model="selectedId" type="radio" :value="step.id" name="reverse-step" :aria-label="'回復歷程 ' + step.id"></td>
              <td>{{ step.id }}</td><td>{{ typeNames[step.type] }}</td><td>{{ step.op }}</td><td>{{ currentLot?.no }}-{{ step.id }}</td><td>{{ step.description }}</td><td>DEV</td><td>{{ step.createdAt }}</td><td>{{ step.committed ? 'True' : 'False' }}</td><td>{{ step.committed ? '不良品已轉庫' : '' }}</td>
            </tr>
            <tr v-if="!currentLot?.steps.length"><td colspan="10">{{ currentLot ? '此批沒有回復歷程。' : '請查詢生產批號。' }}</td></tr>
          </tbody>
        </table>
      </div>
      <div class="toolbar">
        <label>回復原因 <input v-model="reason" maxlength="255"></label>
        <button type="button" :disabled="!selected" @click="requestReverse">執行回復</button>
      </div>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="message" role="status">{{ message }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="resolveReverse" />
    </template>
    <template v-else>
      <p>回復處理 API 待接續。</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.reverse-page { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p { margin: 0; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
.selected td { background: #fff6c5; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
