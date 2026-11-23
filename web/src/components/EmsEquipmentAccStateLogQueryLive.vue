<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'

interface LogRow {
  accSerialNo: string
  equipmentType: string
  equipmentNo: string
  accessoryType: string
  accessoryNo: string
  accessoryVersion: string
  userNo: string | null
  startTime: string | null
  endTime: string | null
}
interface LogPage { page: number; pageSize: number; hasMore: boolean; items: LogRow[] }

const props = defineProps<{ apiBaseUrl: string }>()
const emit = defineEmits<{ close: [] }>()
const filters = reactive({
  equipmentType: '', equipmentNo: '', userNo: '', accessoryType: '', accessoryNo: '',
  startTimeFrom: '', startTimeTo: '', endTimeFrom: '', endTimeTo: '',
})
const rows = ref<LogRow[]>([])
const currentPage = ref(1)
const hasMore = ref(false)
const loading = ref(false)
const queried = ref(false)
const errorMessage = ref('')
const statusMessage = computed(() => loading.value
  ? '查詢中…'
  : errorMessage.value || (queried.value
    ? `第 ${currentPage.value} 頁，${rows.value.length} 筆${hasMore.value ? '，還有下一頁。' : '。'}`
    : '請設定至少一個時間界限後查詢。'))

function apiUrl(): URL {
  const base = props.apiBaseUrl.trim().replace(/\/+$/, '')
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL('api/ems/equipment-accessory-state-logs', new URL(`${base}/`, location.origin))
}

async function query(page = 1): Promise<void> {
  if (loading.value) return
  loading.value = true
  errorMessage.value = ''
  const normalized = Object.fromEntries(Object.entries(filters).map(([key, value]) => [key, value.trim()]))
  if (!normalized.startTimeFrom && !normalized.startTimeTo && !normalized.endTimeFrom && !normalized.endTimeTo) {
    errorMessage.value = '請至少設定一個 Start Time 或 End Time 界限。'
    loading.value = false
    queried.value = true
    rows.value = []
    return
  }
  try {
    const url = apiUrl()
    for (const [key, value] of Object.entries(normalized)) if (value) url.searchParams.set(key, value)
    url.searchParams.set('page', String(page))
    url.searchParams.set('pageSize', '50')
    const response = await fetch(url, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) {
      const body = await response.json().catch(() => null) as { detail?: string; title?: string } | null
      throw new Error(body?.detail || body?.title || `查詢失敗 (${response.status})，請稍後重試。`)
    }
    const result = await response.json() as LogPage
    rows.value = Array.isArray(result.items) ? result.items : []
    currentPage.value = result.page
    hasMore.value = result.hasMore
    queried.value = true
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備配件歷程查詢失敗。'
    rows.value = []
    hasMore.value = false
    queried.value = true
  } finally {
    loading.value = false
  }
}

function formatTime(value: string | null): string {
  return value ? value.replace('T', ' ') : '—'
}

onMounted(() => {
  const initialAccessoryNo = new URLSearchParams(location.search).get('AccessoryNo')?.trim().toUpperCase()
  if (!initialAccessoryNo) return
  filters.accessoryNo = initialAccessoryNo
  const from = new Date(Date.now() - 365 * 24 * 60 * 60 * 1000)
  const local = new Date(from.getTime() - from.getTimezoneOffset() * 60_000)
  filters.startTimeFrom = local.toISOString().slice(0, 16)
  void query(1)
})
</script>

<template>
  <section class="equipment-acc-log-live" aria-label="設備配件歷史資料查詢" :aria-busy="loading">
    <header class="page-heading">
      <div>
        <p class="eyebrow">EMS EXECUTION · ES408</p>
        <h1>設備配件歷史資料查詢</h1>
        <p class="subheading">依設備、配件、人員與時間界限查詢歷程。</p>
      </div>
      <span class="readonly-badge">唯讀</span>
    </header>

    <form class="filter-panel" @submit.prevent="query(1)">
      <div class="text-filter-grid">
        <label><span>Equipment Type</span><input v-model="filters.equipmentType" maxlength="50" autocomplete="off"></label>
        <label><span>Equipment No</span><input v-model="filters.equipmentNo" maxlength="50" autocomplete="off"></label>
        <label><span>User No</span><input v-model="filters.userNo" maxlength="10" autocomplete="off"></label>
        <label><span>Accessory Type</span><input v-model="filters.accessoryType" maxlength="50" autocomplete="off"></label>
        <label><span>Accessory No</span><input v-model="filters.accessoryNo" maxlength="50" autocomplete="off"></label>
      </div>
      <div class="date-filter-grid">
        <label><span>Start Time From</span><input v-model="filters.startTimeFrom" type="datetime-local"></label>
        <label><span>Start Time To</span><input v-model="filters.startTimeTo" type="datetime-local"></label>
        <label><span>End Time From</span><input v-model="filters.endTimeFrom" type="datetime-local"></label>
        <label><span>End Time To</span><input v-model="filters.endTimeTo" type="datetime-local"></label>
      </div>
      <div class="query-actions">
        <p role="status" aria-live="polite">{{ statusMessage }}</p>
        <div>
          <button type="submit" class="primary-button" :disabled="loading">{{ loading ? '查詢中…' : 'Query' }}</button>
          <button type="button" class="secondary-button" @click="emit('close')">Close (X)</button>
        </div>
      </div>
    </form>

    <div class="table-wrap">
      <table>
        <thead><tr><th>Equipment Type</th><th>Equipment No</th><th>Accessory Type</th><th>Accessory No</th><th>Start Time</th><th>End Time</th><th>User No</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row.accSerialNo">
            <td>{{ row.equipmentType }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.accessoryNo }}</td>
            <td>{{ formatTime(row.startTime) }}</td><td>{{ formatTime(row.endTime) }}</td><td>{{ row.userNo || '—' }}</td>
          </tr>
          <tr v-if="queried && !loading && rows.length === 0"><td colspan="7" class="empty-state">{{ errorMessage || '沒有符合條件的歷程。' }}</td></tr>
          <tr v-if="!queried"><td colspan="7" class="empty-state">查詢結果會顯示在這裡。</td></tr>
        </tbody>
      </table>
    </div>
    <nav v-if="queried && !errorMessage" class="pagination" aria-label="歷程分頁">
      <button type="button" class="secondary-button" :disabled="loading || currentPage <= 1" @click="query(currentPage - 1)">上一頁</button>
      <span>第 {{ currentPage }} 頁</span>
      <button type="button" class="secondary-button" :disabled="loading || !hasMore" @click="query(currentPage + 1)">下一頁</button>
    </nav>
  </section>
</template>

<style scoped>
.equipment-acc-log-live { display:flex; flex-direction:column; gap:14px; min-height:0; height:100%; padding:18px; box-sizing:border-box; background:#f7f5ee; color:var(--ink,#26251f); font:inherit; }
.page-heading { display:flex; justify-content:space-between; align-items:center; gap:18px; }
.page-heading h1 { margin:2px 0 4px; font-size:22px; font-weight:700; }
.eyebrow { margin:0; color:#8a7b29; font-size:11px; letter-spacing:.08em; }
.subheading { margin:0; color:var(--muted,#777366); font-size:13px; }
.readonly-badge { border:1px solid #d8cf9d; border-radius:999px; padding:5px 11px; color:#716418; background:#fff9dc; font-size:12px; white-space:nowrap; }
.filter-panel { display:flex; flex-direction:column; gap:12px; padding:14px; border:1px solid #e2dfd5; border-radius:10px; background:#fff; }
.text-filter-grid { display:grid; grid-template-columns:repeat(5,minmax(120px,1fr)); gap:10px; }
.date-filter-grid { display:grid; grid-template-columns:repeat(4,minmax(140px,1fr)); gap:10px; }
.filter-panel label { display:flex; flex-direction:column; gap:5px; min-width:0; color:#555248; font-size:12px; font-weight:600; }
.filter-panel input { min-width:0; height:34px; box-sizing:border-box; border:1px solid #dcd9cf; border-radius:6px; padding:0 9px; background:#fff; color:#27261f; font:inherit; font-size:13px; }
.filter-panel input:focus-visible { outline:2px solid #d6bd32; outline-offset:1px; }
.query-actions { display:flex; justify-content:space-between; align-items:center; gap:12px; }
.query-actions p { margin:0; color:var(--muted,#777366); font-size:12px; }
.query-actions > div { display:flex; gap:8px; }
.primary-button,.secondary-button { min-height:34px; border-radius:6px; padding:0 13px; font:inherit; font-size:13px; cursor:pointer; }
.primary-button { border:1px solid #3b3930; background:#3b3930; color:#fff; }
.secondary-button { border:1px solid #dcd9cf; background:#fff; color:#4f4c43; }
button:disabled { opacity:.5; cursor:not-allowed; }
.table-wrap { min-height:0; flex:1; overflow:auto; border:1px solid #e2dfd5; border-radius:10px; background:#fff; }
table { width:100%; border-collapse:collapse; font-size:13px; }
th,td { padding:9px 11px; border-bottom:1px solid #eeece5; text-align:left; white-space:nowrap; }
th { position:sticky; top:0; z-index:1; background:#f4f1e6; color:#575348; font-weight:650; }
.empty-state { padding:28px 12px; color:#898579; text-align:center; }
.pagination { display:flex; justify-content:flex-end; align-items:center; gap:12px; color:#68655c; font-size:12px; }
@media(max-width:1050px) { .text-filter-grid { grid-template-columns:repeat(3,minmax(120px,1fr)); } .date-filter-grid { grid-template-columns:repeat(2,minmax(140px,1fr)); } }
@media(max-width:650px) { .equipment-acc-log-live { padding:12px; } .text-filter-grid,.date-filter-grid { grid-template-columns:1fr 1fr; } .query-actions { align-items:flex-start; flex-direction:column; } }
</style>
