<script setup lang="ts">
import { computed, defineAsyncComponent, onMounted, ref } from 'vue'

const props = withDefaults(defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean }>(), {
  apiBaseUrl: '',
  developmentPreview: false,
})
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const Preview = import.meta.env.DEV
  ? defineAsyncComponent(() => import('./EmsAccessoryStateQueryDevPreview.vue'))
  : null

interface AccessoryStateRow {
  accSerialNo: string
  accessoryNo: string
  accessoryVersion: string
  accessoryType: string
  accessoryState: number
  stateName: string
  startTime: string
  userNo: string
  description: string | null
  applyTime: string | null
  accumulateQty: number | null
  accSpareQty: number | null
  accRepairQty: number | null
  accTotalUsedQty: number | null
  timeCycleStart: string | null
  repairCycle: number | null
  accSpareQtyLimit: number | null
}

const accessoryType = ref('')
const accessoryNo = ref('')
const items = ref<AccessoryStateRow[]>([])
const page = ref(1)
const pageSize = 50
const hasMore = ref(false)
const loading = ref(false)
const errorMessage = ref('')
const queried = ref(false)

function apiUrl(): string {
  const base = props.apiBaseUrl.trim().replace(/\/+$/, '')
  if (!base) throw new Error('尚未設定 API 位址。')
  return `${base}/api/ems/accessory-states`
}

async function query(targetPage = 1): Promise<void> {
  if (dev.value || loading.value) return
  loading.value = true
  errorMessage.value = ''
  try {
    const url = new URL(apiUrl(), window.location.origin)
    const type = accessoryType.value.trim()
    const no = accessoryNo.value.trim()
    if (type) url.searchParams.set('accessoryType', type)
    if (no) url.searchParams.set('accessoryNo', no)
    url.searchParams.set('page', String(targetPage))
    url.searchParams.set('pageSize', String(pageSize))
    const response = await fetch(url, { credentials: 'include', cache: 'no-store' })
    if (!response.ok) {
      let detail = ''
      try {
        const problem = await response.json() as { detail?: string; title?: string }
        detail = problem.detail || problem.title || ''
      } catch { /* Keep the short, stable fallback below. */ }
      throw new Error(detail || `查詢失敗 (${response.status})，請稍後重試。`)
    }
    const result = await response.json() as { page: number; pageSize: number; hasMore: boolean; items: AccessoryStateRow[] }
    items.value = Array.isArray(result.items) ? result.items : []
    page.value = result.page
    hasMore.value = result.hasMore
    queried.value = true
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '配件狀態查詢失敗，請稍後重試。'
    items.value = []
    hasMore.value = false
    queried.value = true
  } finally {
    loading.value = false
  }
}

function formatDate(value: string | null): string {
  if (!value) return ''
  const parsed = new Date(value)
  return Number.isNaN(parsed.getTime()) ? value : new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'short', timeStyle: 'medium',
  }).format(parsed)
}

function formatNumber(value: number | null): string {
  return value === null || value === undefined ? '' : new Intl.NumberFormat('zh-TW', { maximumFractionDigits: 4 }).format(value)
}

onMounted(() => { if (!dev.value) void query(1) })
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <section v-else class="accessory-state-page" aria-label="配件狀態查詢" :aria-busy="loading">
    <form class="query-panel" @submit.prevent="query(1)">
      <label>
        <span>配件類型</span>
        <input v-model="accessoryType" maxlength="50" autocomplete="off" />
      </label>
      <label>
        <span>配件編號</span>
        <input v-model="accessoryNo" maxlength="50" autocomplete="off" />
      </label>
      <button class="primary-button" type="submit" :disabled="loading">{{ loading ? '查詢中…' : '查詢' }}</button>
    </form>

    <p v-if="errorMessage" class="state-message error" role="alert">{{ errorMessage }}</p>
    <p v-else-if="loading" class="state-message" role="status">正在載入配件狀態…</p>
    <p v-else-if="queried && items.length === 0" class="state-message" role="status">查無配件狀態資料。</p>

    <div v-if="items.length" class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>配件類型</th><th>配件編號</th><th>版本</th><th>狀態</th><th>開始時間</th>
            <th>使用者</th><th>說明</th><th>申請時間</th><th>累積數量</th>
            <th>剩餘數量</th><th>維修次數</th><th>總使用量</th><th>時間週期開始</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in items" :key="`${row.accessoryNo}:${row.accessoryVersion}:${row.accSerialNo}`">
            <td>{{ row.accessoryType }}</td><td>{{ row.accessoryNo }}</td><td>{{ row.accessoryVersion }}</td>
            <td>{{ row.stateName }}</td><td>{{ formatDate(row.startTime) }}</td><td>{{ row.userNo }}</td>
            <td>{{ row.description }}</td><td>{{ formatDate(row.applyTime) }}</td>
            <td class="number-cell">{{ formatNumber(row.accumulateQty) }}</td>
            <td class="number-cell">{{ formatNumber(row.accSpareQtyLimit) }}</td>
            <td class="number-cell">{{ formatNumber(row.accRepairQty) }}</td>
            <td class="number-cell">{{ formatNumber(row.accTotalUsedQty) }}</td>
            <td>{{ formatDate(row.timeCycleStart) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <footer class="list-footer">
      <span>{{ items.length ? `第 ${page} 頁・${items.length} 筆` : '' }}</span>
      <div>
        <button type="button" :disabled="loading || page <= 1" @click="query(page - 1)">上一頁</button>
        <button type="button" :disabled="loading || !hasMore" @click="query(page + 1)">下一頁</button>
        <button type="button" class="close-button" @click="emit('close')">關閉</button>
      </div>
    </footer>
  </section>
</template>

<style scoped>
.accessory-state-page { display:flex; flex-direction:column; gap:14px; min-height:100%; padding:16px; box-sizing:border-box; color:var(--ink,#26251f); background:#f7f5ee; }
.query-panel { display:flex; flex-wrap:wrap; align-items:end; gap:12px; padding:14px; border:1px solid var(--line,#dedbd1); border-radius:10px; background:#fff; }
.query-panel label { display:grid; gap:6px; min-width:180px; color:var(--muted,#777366); font-size:13px; }
.query-panel input { min-height:38px; padding:6px 10px; border:1px solid var(--line,#dedbd1); border-radius:7px; color:var(--ink,#26251f); background:white; font:inherit; }
.query-panel input:focus-visible,.list-footer button:focus-visible,.primary-button:focus-visible { outline:2px solid var(--yellow,#e7c600); outline-offset:2px; }
.primary-button,.list-footer button { min-height:38px; padding:7px 14px; border:1px solid var(--line,#dedbd1); border-radius:7px; color:var(--ink,#26251f); background:#fff; font:inherit; cursor:pointer; }
.primary-button { border-color:#3b3930; background:#3b3930; color:white; }
.primary-button:disabled,.list-footer button:disabled { cursor:not-allowed; opacity:.45; }
.state-message { margin:0; color:var(--muted,#777366); }
.state-message.error { color:#a13a34; }
.table-wrap { overflow:auto; border:1px solid var(--line,#dedbd1); border-radius:10px; background:#fff; }
table { width:100%; border-collapse:collapse; white-space:nowrap; font-size:13px; }
th,td { padding:9px 11px; border-bottom:1px solid #ebe9e2; text-align:left; }
th { position:sticky; top:0; z-index:1; color:var(--muted,#777366); background:#f8f6ef; font-weight:600; }
tbody tr:last-child td { border-bottom:0; }
.number-cell { text-align:right; font-variant-numeric:tabular-nums; }
.list-footer { display:flex; justify-content:space-between; align-items:center; gap:12px; margin-top:auto; color:var(--muted,#777366); font-size:13px; }
.list-footer div { display:flex; flex-wrap:wrap; gap:8px; }
.list-footer .close-button { background:#3b3930; color:white; }
@media (max-width:640px) { .accessory-state-page { padding:10px; } .query-panel label { min-width:min(100%,220px); flex:1 1 100%; } .list-footer { align-items:flex-start; flex-direction:column; } }
</style>
