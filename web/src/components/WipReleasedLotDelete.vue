<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'

defineProps<{ developmentPreview: boolean }>()
defineEmits<{ close: [] }>()

interface ReleasedLot {
  no: string
  status: 0 | 1 | 4
  related?: 'split' | 'merge'
  packaged?: boolean
  history?: { id: number; description: string; committed: boolean }
}

const lots = ref<ReleasedLot[]>([
  { no: 'DEV-DEL001', status: 0 },
  { no: 'DEV-DEL002', status: 4, history: { id: 1, description: '進站及完工歷程', committed: false } },
  { no: 'DEV-DEL003', status: 1, packaged: true },
  { no: 'DEV-DEL004', status: 0, related: 'split' },
  { no: 'DEV-DEL005', status: 4, history: { id: 1, description: '已轉庫', committed: true } },
])
const statuses = { 0: '排隊', 1: '執行中', 4: '完成' }
const lotInput = ref('')
const loadedNo = ref('')
const confirmation = ref<string | null>(null)
const error = ref('')
const result = ref('')
const current = computed(() => lots.value.find(lot => lot.no === loadedNo.value))
const fields = computed(() => {
  const lot = current.value
  if (!lot) return []
  return [
    ['生產批號', lot.no], ['作業站', 'WB'], ['站名', '打線'], ['狀態', statuses[lot.status]], ['分支流程', 'N/A'],
    ['現有數量', '100'], ['單位', 'PCS'], ['基礎批號', lot.no], ['來源批號', lot.no], ['急件', '否'],
    ['優先序', '1'], ['訂單', 'DEV-RO01'], ['項次', '1'], ['客戶', 'DEV-C01'], ['工單', 'DEV-MO01'],
    ['產品', 'DEV-IC'], ['版本', 'A'], ['客戶批號', 'DEV-CLOT01'], ['Device', 'DEV-DEVICE'], ['下線日期', '2026-11-20'],
  ]
})

function search(): void {
  const lot = lots.value.find(row => row.no === lotInput.value.trim().toUpperCase())
  loadedNo.value = ''
  result.value = ''
  error.value = ''
  if (!lot) error.value = '查無符合條件的已下線生產批。'
  else if (lot.packaged) error.value = '已包裝批號不可繼續作業。'
  else lotInput.value = loadedNo.value = lot.no
}

function deletionError(lot: ReleasedLot): string {
  if (lot.no !== lotInput.value.trim().toUpperCase()) return '批號與已載入資料不同，請重新查詢。'
  if (lot.related) return lot.related === 'split' ? '此批有分批來源，不可刪除。' : '此批有併批來源，不可刪除。'
  if (lot.history?.committed) return '歷程已提交，無法先行回復。'
  return ''
}

function requestDelete(): void {
  const lot = current.value
  if (!lot) return
  error.value = deletionError(lot)
  if (!error.value) confirmation.value = `確定刪除生產批 ${lot.no}？${lot.history ? '此批有歷程，將先回復至序號 1。' : ''}`
}

function confirmDelete(accepted: boolean): void {
  confirmation.value = null
  const lot = current.value
  if (!accepted || !lot) return
  error.value = deletionError(lot)
  if (error.value) return
  lots.value = lots.value.filter(row => row.no !== lot.no)
  loadedNo.value = lotInput.value = ''
  result.value = `已刪除 DEV 生產批 ${lot.no}。`
}
</script>

<template>
  <section class="released-lot" aria-labelledby="released-lot-title">
    <h2 id="released-lot-title">刪除已下線生產批</h2>
    <template v-if="developmentPreview">
      <form class="toolbar" @submit.prevent="search">
        <label>生產批號 <input v-model="lotInput" list="released-lot-candidates" maxlength="50" autocomplete="off"></label>
        <datalist id="released-lot-candidates"><option v-for="lot in lots" :key="lot.no" :value="lot.no" /></datalist>
        <button type="submit">查詢</button>
        <button type="button" :disabled="!current" @click="requestDelete">刪除</button>
        <button type="button" @click="$emit('close')">關閉</button>
      </form>
      <div class="grid">
        <table aria-label="生產批狀態">
          <thead><tr><th v-for="[label] in fields" :key="label" scope="col">{{ label }}</th></tr></thead>
          <tbody><tr v-if="current"><td v-for="[label, value] in fields" :key="label">{{ value }}</td></tr>
            <tr v-else><td>請查詢生產批號。</td></tr>
          </tbody>
        </table>
      </div>
      <div class="grid">
        <table aria-label="回復歷程">
          <thead><tr><th scope="col">回復序號</th><th scope="col">類型</th><th scope="col">作業站</th><th scope="col">批序號</th><th scope="col">已提交</th><th scope="col">說明</th><th scope="col">建立人</th><th scope="col">建立時間</th></tr></thead>
          <tbody>
            <tr v-if="current?.history">
              <td>{{ current.history.id }}</td><td>Trigger OP</td><td>WB</td><td>{{ current.no }}-1</td><td>{{ current.history.committed ? 'True' : 'False' }}</td><td>{{ current.history.description }}</td><td>DEV</td><td>2026-11-20 09:20:00</td>
            </tr>
            <tr v-else><td colspan="8">沒有回復歷程。</td></tr>
          </tbody>
        </table>
      </div>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="result" role="status">{{ result }}</p>
      <ActionConfirmationDialog :message="confirmation" @resolve="confirmDelete" />
    </template>
    <template v-else>
      <p>刪除已下線生產批 API 待接續。</p>
      <button type="button" @click="$emit('close')">關閉</button>
    </template>
  </section>
</template>

<style scoped>
.released-lot { display: grid; align-content: start; gap: 12px; padding: 16px; color: var(--ink); }
h2, p { margin: 0; }
.toolbar, label { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.grid { overflow: auto; border: 1px solid var(--line); background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
td, th { padding: 6px 9px; text-align: left; white-space: nowrap; border-bottom: 1px solid var(--line); }
th { background: #f7f5ee; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
