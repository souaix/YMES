<script setup lang="ts">
import { computed, defineComponent, h, onMounted, ref } from 'vue'
import { apiErrorMessage } from '../utils/apiError'

type Cell = string | number | null
type Row = Record<string, Cell>
interface Detail {
  basis: Row
  teamMembers: Row[]
  questions: Row[]
  actions: Row[]
  actionDetails: Row[]
  rootCauses: Row[]
  dispositions: Row[]
  qAs: Row[]
  preventions: Row[]
  preventionTracks: Row[]
  attachments: Row[]
  release: Row | null
  releaseLog: Row | null
}

const props = defineProps<{ apiBaseUrl?: string; initialQrsNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const tabs = [
  ['basis', 'Basis'], ['d1', 'Create Team'], ['d2', 'Problem Description'], ['d3', 'Immediate Action'],
  ['d4', 'Cause Analysis'], ['d5', 'Disposition'], ['d6', 'Execute Verification'],
  ['d7', 'Precausion'], ['d8', 'Summary'], ['d9', '8DClose'],
] as const
const tab = ref<string>('basis')
const detail = ref<Detail | null>(null)
const loading = ref(false)
const error = ref('')
const message = ref('')
const qrsNo = computed(() => (props.initialQrsNo || '').trim())
const selectedTab = computed(() => tabs.find(([key]) => key === tab.value)?.[1] || 'Basis')
const sections = computed(() => {
  if (!detail.value) return [] as { title: string; rows: Row[] }[]
  const value = detail.value
  const groups: Record<string, { title: string; rows: Row[] }[]> = {
    d1: [{ title: 'D1 TeamMember', rows: value.teamMembers }],
    d2: [{ title: 'D2 Question', rows: value.questions }],
    d3: [{ title: 'D3 Action', rows: value.actions }, { title: 'D3 Action Detail', rows: value.actionDetails }],
    d4: [{ title: 'D4 RootCause', rows: value.rootCauses }],
    d5: [{ title: 'D5 Disposition', rows: value.dispositions }],
    d6: [{ title: 'D6 QA Verification', rows: value.qAs }],
    d7: [{ title: 'D7 Prevention', rows: value.preventions }, { title: 'D7 Prevention Track', rows: value.preventionTracks }],
    d8: [{ title: 'Release Request', rows: value.release ? [value.release] : [] }],
    d9: [{ title: '8D Release Log', rows: value.releaseLog ? [value.releaseLog] : [] }],
  }
  return groups[tab.value] || []
})
const visibleAttachments = computed(() => {
  const stage = (tabs.find(([key]) => key === tab.value)?.[0] === 'basis' ? 'N/A' : tab.value.toUpperCase())
  return detail.value?.attachments.filter(item => String(item.stageno || '').toUpperCase() === stage) || []
})

function apiUrl(): URL {
  const base = (props.apiBaseUrl || '').trim().replace(/\/+$/, '')
  if (!base) throw new Error('目前環境尚未設定 YMES API 位址。')
  const root = new URL(`${base}/`, window.location.origin)
  const local = root.hostname === 'localhost' || root.hostname === '127.0.0.1'
  if (root.protocol !== 'https:' && !local) throw new Error('正式 API 必須使用 HTTPS。')
  return new URL(`api/wip/8d/${encodeURIComponent(qrsNo.value)}/detail`, root)
}

async function load(): Promise<void> {
  if (loading.value) return
  error.value = ''
  detail.value = null
  if (!qrsNo.value) { error.value = '缺少 QRSNo，無法讀取 8D 明細。'; return }
  loading.value = true
  try {
    const response = await fetch(apiUrl(), { credentials: 'include', cache: 'no-store' })
    if (!response.ok) throw new Error(await apiErrorMessage(response))
    detail.value = await response.json() as Detail
    message.value = '正式 8D 明細唯讀。此頁不提供修改與附件下載。'
  } catch (cause) {
    error.value = cause instanceof Error ? cause.message : '8D 明細讀取失敗，請重試。'
  } finally { loading.value = false }
}

function label(key: string): string { return key.toUpperCase() }
function display(value: unknown): string { return value === null || value === undefined || value === '' ? '—' : String(value) }
const DataTable = defineComponent({
  name: 'Wp802DataTable',
  props: { rows: { type: Array as () => Row[], required: true } },
  setup(tableProps) {
    return () => {
      if (!tableProps.rows.length) return h('p', { class: 'empty' }, '無資料')
      const columns = Object.keys(tableProps.rows[0])
      return h('div', { class: 'table-wrap' }, [h('table', [
        h('thead', [h('tr', columns.map(key => h('th', { key }, key.toUpperCase())))]),
        h('tbody', tableProps.rows.map((row, index) => h('tr', { key: index }, columns.map(key => h('td', { key }, display(row[key])))))),
      ])])
    }
  },
})
onMounted(() => { void load() })
</script>

<template>
  <section class="wp802-readonly">
    <header class="titlebar">
      <div><h1>8D 矯正單維護作業 <small>8D MAINTAIN · WP802</small></h1><p>QRSNo: <strong>{{ qrsNo || '—' }}</strong>　<span>唯讀</span></p></div>
      <div class="header-actions"><button type="button" class="mes-button" :disabled="loading" @click="load">重新讀取</button><button type="button" class="mes-button close" @click="emit('close')">Close (&X)</button></div>
    </header>
    <p v-if="error" class="feedback error" role="alert">{{ error }}</p>
    <p v-else-if="loading" class="feedback" role="status">正在讀取 8D 明細…</p>
    <p v-else-if="message" class="feedback" role="status">{{ message }}</p>
    <template v-if="detail">
      <nav class="tabs" aria-label="8D 階段">
        <button v-for="item in tabs" :key="item[0]" type="button" class="mes-button" :class="{ selected: tab === item[0] }" @click="tab = item[0]">{{ item[1] }}</button>
      </nav>
      <main class="content">
        <fieldset v-if="tab === 'basis'"><legend>8D Basis Data</legend><dl class="basis-grid"><template v-for="(value, key) in detail.basis" :key="key"><dt>{{ label(String(key)) }}</dt><dd>{{ display(value) }}</dd></template></dl></fieldset>
        <template v-else-if="tab === 'd8'">
          <fieldset><legend>Summary</legend><dl class="basis-grid"><template v-for="key in ['summarydescription','disphour','dispcost','summarydepartment','summaryuserno','summaryusername']" :key="key"><dt>{{ label(key) }}</dt><dd>{{ display(detail.basis[key]) }}</dd></template></dl></fieldset>
          <fieldset><legend>Release Request</legend><DataTable :rows="detail.release ? [detail.release] : []" /></fieldset>
        </template>
        <template v-else-if="tab === 'd9'">
          <fieldset><legend>8D Close Result</legend><dl class="basis-grid"><template v-for="key in ['curstage','closetype','closedescription','closedepartment','closeuserno','closeusername','closedate']" :key="key"><dt>{{ label(key) }}</dt><dd>{{ display(detail.basis[key]) }}</dd></template></dl></fieldset>
          <fieldset><legend>8D Release Log</legend><DataTable :rows="detail.releaseLog ? [detail.releaseLog] : []" /></fieldset>
        </template>
        <fieldset v-else v-for="section in sections" :key="section.title"><legend>{{ section.title }}</legend><DataTable :rows="section.rows" /></fieldset>
        <fieldset class="attachments"><legend>{{ selectedTab }} Attachments</legend>
          <table v-if="visibleAttachments.length"><thead><tr><th>FileName</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody><tr v-for="(file, index) in visibleAttachments" :key="`${file.attachfilename}-${index}`"><td>{{ display(file.attachfilename) }}</td><td>{{ display(file.creator) }}</td><td>{{ display(file.createdate) }}</td></tr></tbody></table>
          <p v-else class="empty">無附件資料</p>
        </fieldset>
      </main>
    </template>
  </section>
</template>

<style scoped>
.wp802-readonly{height:100%;min-height:0;box-sizing:border-box;display:flex;flex-direction:column;gap:8px;padding:10px;background:#f7f5ee;color:#333;font:13px Arial,'Microsoft JhengHei',sans-serif}.titlebar{display:flex;align-items:center;justify-content:space-between;gap:12px}.titlebar h1{font-size:18px;margin:0}.titlebar small{font-size:12px;color:#777;font-weight:400}.titlebar p{margin:5px 0 0;color:#666}.titlebar p span{border:1px solid #c7bc82;border-radius:10px;padding:2px 8px;color:#71671e}.header-actions{display:flex;gap:6px}.mes-button{border:1px solid #d8d5ca;background:#fff;color:#3a3832;border-radius:4px;padding:6px 12px;cursor:pointer}.mes-button.selected{background:#eee7bd;border-color:#b2a556}.mes-button.close{margin-left:5px}.mes-button:disabled{opacity:.5;cursor:default}.feedback{margin:0;min-height:18px}.feedback.error{color:#a22;background:#fff0ef;padding:4px}.tabs{display:flex;gap:4px;overflow-x:auto;padding-bottom:4px}.content{flex:1;min-height:0;overflow:auto;display:flex;flex-direction:column;gap:8px}fieldset{border:1px solid #d8d5ca;background:#fff;border-radius:4px;min-width:0;padding:8px}.basis-grid{display:grid;grid-template-columns:repeat(4,minmax(100px,1fr));gap:4px 10px;margin:0}.basis-grid dt{font-weight:600;color:#6b6658}.basis-grid dd{margin:0;min-width:0;overflow-wrap:anywhere;border-bottom:1px solid #eee;padding:2px 0 4px}.table-wrap{max-width:100%;overflow:auto}table{border-collapse:collapse;width:100%;min-width:max-content}th,td{border:1px solid #ddd9cd;padding:5px 7px;text-align:left;vertical-align:top;max-width:320px;overflow-wrap:anywhere}th{background:#f2f0e7;position:sticky;top:0}.empty{color:#777;margin:2px 0}@media(max-width:760px){.titlebar{align-items:flex-start;flex-direction:column}.basis-grid{grid-template-columns:repeat(2,minmax(100px,1fr))}}
</style>
