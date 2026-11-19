<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'

type Row = Record<string, any>
type ProgramOption = { programNo: string; programType: string | null; description: string | null; missing?: boolean }
interface Assignments { productNo: string; productVersion: string; issueState: number | null; programNos: string[]; version: string }

const props = defineProps<{
  product: Row
  programOptions?: Row[]
  selectedProgramNos?: string[]
  developmentPreview: boolean
  apiBaseUrl?: string
}>()
const emit = defineEmits<{ save: [programNos: string[]]; saved: []; close: [] }>()
const mock = computed(() => import.meta.env.DEV && props.developmentPreview)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))
const selected = ref<string[]>([])
const options = ref<ProgramOption[]>([])
const snapshot = ref<Assignments | null>(null)
const loading = ref(false)
const saving = ref(false)
const ready = ref(false)
const error = ref('')
let sequence = 0
let loadController: AbortController | null = null
const issueState = computed(() => {
  if (!mock.value) return snapshot.value?.issueState ?? null
  const value = props.product.issueState ?? props.product.IssueState
  return value === null || value === undefined || value === '' ? null : Number(value)
})
const canConfirm = computed(() => ready.value && !loading.value && !saving.value && issueState.value === 0)
const readOnly = computed(() => !ready.value || loading.value || saving.value)

function mapOptions(rows: Row[]): ProgramOption[] { return rows.map(row => ({
  programNo: String(row.programNo ?? row.ProgramNo ?? ''),
  programType: row.programType ?? row.ProgramType ?? null,
  description: row.description ?? row.Description ?? null,
})).filter(row => row.programNo) }
const programs = computed<ProgramOption[]>(() => {
  const rows = mock.value ? mapOptions(props.programOptions ?? []) : options.value
  const assigned = mock.value ? props.selectedProgramNos ?? [] : snapshot.value?.programNos ?? []
  // Show real orphan assignments explicitly so saving cannot silently drop a
  // hidden relation. The server still rejects reassigning an absent master.
  const missing = assigned.filter(no => !rows.some(row => row.programNo.toUpperCase() === no.toUpperCase()))
    .map(no => ({ programNo: no, programType: null, description: '已指派，但程式主檔不存在；可取消勾選後儲存。', missing: true }))
  return [...rows, ...missing]
})

watch(() => props.selectedProgramNos, value => {
  if (mock.value) selected.value = [...new Set((value ?? []).map(String))]
}, { immediate: true, deep: true })

function apiUrl(path: string): string {
  const base = props.apiBaseUrl?.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), new URL(base.replace(/\/+$/, '') + '/', window.location.origin)).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init, headers })
  if (response.status === 401) {
    window.location.assign(import.meta.env.BASE_URL)
    throw new Error('登入狀態已逾期，請重新登入。')
  }
  if (!response.ok) {
    const body = await response.text()
    let message = body.trim()
    try {
      const problem: unknown = JSON.parse(body)
      if (problem && typeof problem === 'object') {
        const fields = problem as Record<string, unknown>
        const detail = [fields.detail, fields.message, fields.title].find(value => typeof value === 'string' && value.trim())
        if (typeof detail === 'string') message = detail
      }
    } catch { /* Preserve non-JSON service messages. */ }
    throw new Error(message || 'Program API 回應錯誤（' + response.status + '）。')
  }
  return await response.json() as T
}
function assignmentsPath(no: string, version: string): string {
  return 'api/products/' + encodeURIComponent(no) + '/versions/' + encodeURIComponent(version) + '/programs'
}
function validAssignments(value: Assignments): boolean {
  return !!value && typeof value.productNo === 'string' && typeof value.productVersion === 'string'
    && Array.isArray(value.programNos) && value.programNos.every(no => typeof no === 'string')
    && typeof value.version === 'string' && /^[0-9a-f]{64}$/i.test(value.version)
    && (value.issueState === null || typeof value.issueState === 'number')
}
async function load(): Promise<void> {
  const current = ++sequence
  loadController?.abort(); loadController = null
  ready.value = false; snapshot.value = null; selected.value = []; options.value = []; error.value = ''
  if (mock.value) {
    selected.value = [...new Set(props.selectedProgramNos ?? [])]
    ready.value = true; loading.value = false
    return
  }
  loading.value = true
  const no = productNo.value; const version = productVersion.value
  try {
    if (!no || !version) throw new Error('請先選取有效的產品與版次。')
    loadController = new AbortController()
    const [choices, value] = await Promise.all([
      request<ProgramOption[]>('api/products/options/programs', { signal: loadController.signal }),
      request<Assignments>(assignmentsPath(no, version), { signal: loadController.signal }),
    ])
    if (!Array.isArray(choices) || !validAssignments(value)
      || value.productNo.toUpperCase() !== no.toUpperCase() || value.productVersion.toUpperCase() !== version.toUpperCase())
      throw new Error('產品 Program 回應不完整，請關閉後重新開啟。')
    if (sequence !== current) return
    options.value = mapOptions(choices)
    snapshot.value = value
    selected.value = [...new Set(value.programNos.map(code => choices.find(row => row.programNo.toUpperCase() === code.toUpperCase())?.programNo ?? code))]
    ready.value = true
  } catch (reason) {
    if (sequence === current) error.value = reason instanceof Error ? reason.message : '載入產品 Program 指派失敗。'
  } finally { if (sequence === current) loading.value = false }
}
watch([productNo, productVersion, mock], () => { void load() }, { immediate: true })
onBeforeUnmount(() => { sequence++; loadController?.abort() })

function toggleProgram(programNo: string, event: Event): void {
  if (readOnly.value) return
  error.value = ''
  const checked = (event.target as HTMLInputElement).checked
  selected.value = checked
    ? [...new Set([...selected.value, programNo])]
    : selected.value.filter(value => value !== programNo)
}

async function confirm(): Promise<void> {
  if (!canConfirm.value) return
  if (mock.value) { emit('save', [...selected.value]); emit('close'); return }
  const original = snapshot.value
  if (!original) return
  saving.value = true; error.value = ''
  try {
    const result = await request<Assignments>(assignmentsPath(original.productNo, original.productVersion), {
      method: 'PUT', body: JSON.stringify({ programNos: [...selected.value], version: original.version }),
    })
    if (!validAssignments(result) || result.productNo !== original.productNo || result.productVersion !== original.productVersion)
      throw new Error('儲存後回應不完整，請關閉後重新查詢確認。')
    snapshot.value = result; selected.value = [...result.programNos]
    emit('save', [...result.programNos]); emit('saved'); emit('close')
  } catch (reason) { error.value = reason instanceof Error ? reason.message : '儲存產品 Program 指派失敗。' }
  finally { saving.value = false }
}
</script>

<template>
  <section class="prd-program-window" role="dialog" aria-modal="true" aria-label="Product Program">
    <header class="prd-program-title">
      <span>Product Program</span><span v-if="mock" class="dev-badge">DEV</span>
      <button type="button" aria-label="關閉視窗" :disabled="saving" @click="emit('close')">×</button>
    </header>
    <main class="prd-program-body">
      <div class="prd-program-context">
        <label for="program-product">Product No</label><input id="program-product" :value="productNo" readonly>
        <label for="program-version">Product Version</label><input id="program-version" :value="productVersion" readonly>
      </div>

      <fieldset class="prd-program-group">
        <legend>Programs</legend>
        <div class="prd-program-grid-wrap">
          <table class="prd-program-grid">
            <colgroup><col class="select-col"><col class="no-col"><col class="type-col"><col class="description-col"></colgroup>
            <thead><tr><th>Select</th><th>ProgramNo</th><th>ProgramType</th><th>Description</th></tr></thead>
            <tbody>
              <tr v-for="row in programs" :key="row.programNo">
                <td class="check-cell"><input :aria-label="'Select ' + row.programNo" type="checkbox" :checked="selected.includes(row.programNo)" :disabled="readOnly" @change="toggleProgram(row.programNo, $event)"></td>
                <td>{{ row.programNo }}</td><td>{{ row.programType }}</td><td class="description">{{ row.description }}</td>
              </tr>
              <tr v-if="!programs.length"><td colspan="4" class="empty">{{ loading ? '載入中…' : '沒有可指派的程式資料。' }}</td></tr>
            </tbody>
          </table>
        </div>
      </fieldset>

      <div class="prd-program-status">
        <span v-if="loading">載入中…</span>
        <span v-else-if="issueState !== 0">產品狀態不是未核准，Confirm 停用。</span>
        <span v-else>已勾選 {{ selected.length }} 個 Program</span>
      </div>
      <p v-if="error" class="program-error" role="alert">{{ error }}</p>
      <footer class="prd-program-footer">
        <button class="button primary" type="button" :disabled="!canConfirm" @click="confirm">Confirm</button>
        <button class="button" type="button" :disabled="saving" @click="emit('close')">Close</button>
      </footer>
    </main>
  </section>
</template>

<style scoped>
.prd-program-window{box-sizing:border-box;width:min(684px,98vw);height:min(516px,96vh);min-height:430px;background:#fff;border:1px solid #aeb4ba;box-shadow:0 8px 28px #18253635;display:flex;flex-direction:column;color:#20252a;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif;overflow:hidden}
.prd-program-title{height:30px;min-height:30px;display:flex;align-items:center;padding:0 9px 0 14px;border-bottom:1px solid #d4d8dc;background:#f6f6f6;font-size:14px;font-weight:600}
.prd-program-title button{margin-left:auto;border:0;background:transparent;font-size:21px;line-height:1;color:#4f555b;cursor:pointer}
.dev-badge{margin-left:10px;padding:2px 7px;border-radius:10px;background:#fff0cf;color:#8c5a00;font:10px Arial,sans-serif}
.prd-program-body{position:relative;flex:1;min-height:0}
.prd-program-context{position:absolute;left:16px;top:14px;height:22px;display:flex;align-items:center}
.prd-program-context label{width:96px;text-align:right;margin-right:6px}
.prd-program-context input{box-sizing:border-box;width:202px;height:21px;padding:2px 5px;border:1px solid #aaa;background:#eee;font:13px "Times New Roman",Arial,sans-serif}
.prd-program-context label:nth-of-type(2){width:104px;margin-left:6px}
.prd-program-context input:last-child{width:72px}
.prd-program-group{position:absolute;left:16px;right:16px;top:42px;height:388px;margin:0;padding:14px 15px;border:1px solid #aeb4ba;color:#2d4771}
.prd-program-group legend{padding:0 5px;color:#2d4771}
.prd-program-grid-wrap{position:absolute;left:15px;right:15px;top:24px;bottom:14px;overflow:auto;border:1px solid #b9bec3;color:#20252a}
.prd-program-grid{width:100%;border-collapse:collapse;table-layout:fixed;white-space:nowrap;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif}
.prd-program-grid .select-col{width:70px}.prd-program-grid .no-col{width:100px}.prd-program-grid .type-col{width:100px}.prd-program-grid .description-col{width:150px}
.prd-program-grid th{position:sticky;top:0;z-index:1;height:26px;padding:3px 7px;background:#4b9ce3;color:#fff;border:1px solid #d3d8dc;text-align:left;font-weight:500}
.prd-program-grid td{height:24px;padding:2px 7px;border:1px solid #d3d7da;background:#fff;overflow:hidden;text-overflow:ellipsis}
.prd-program-grid td.check-cell{text-align:center;padding:2px}
.prd-program-grid td.check-cell input{margin:0;accent-color:#467fb0}
.prd-program-grid .description{max-width:150px}
.prd-program-grid .empty{text-align:center;color:#737b82;height:50px}
.prd-program-status{position:absolute;left:17px;right:190px;bottom:20px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#737b82;font-size:12px}
.program-error{position:absolute;left:16px;right:16px;bottom:49px;z-index:2;margin:0;max-height:78px;overflow:auto;padding:7px;border:1px solid #c88d7a;background:#fff0e9;color:#873820;white-space:normal}
.prd-program-footer{position:absolute;right:14px;bottom:13px;display:flex;gap:6px}
.button{box-sizing:border-box;width:80px;height:32px;padding:4px 9px;border:1px solid #c5c9cd;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#292d31;cursor:pointer;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif}
.button:hover:not(:disabled){border-color:#eea145;background:#fff7e8}
.button:disabled{opacity:.5;cursor:not-allowed}
.button.primary{border-color:#ec9b20;background:#ffa51f;color:#fff;font-weight:600}
@media(max-width:600px){.prd-program-window{width:98vw}.prd-program-context{left:8px;transform:scale(.82);transform-origin:left center}.prd-program-group{left:8px;right:8px}.prd-program-status{left:8px}}
</style>
