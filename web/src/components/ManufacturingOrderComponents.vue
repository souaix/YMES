<script lang="ts">
export interface WorkOrderComponent {
  componentNo: string
  goodQty: number
  unitNo: string
  releaseState?: number
  waferIdText?: string
  slotNo?: string
  inventoryNo?: string
  materialNo?: string
  materialLotNo?: string
  componentLotNo?: string
  vendorNo?: string
  cassetteNo?: string
  laserMark?: string
  testResults?: Record<string, string | number | null>
}
</script>

<script setup lang="ts">
import { computed, ref } from 'vue'
import ActionConfirmationDialog from './ActionConfirmationDialog.vue'
import { useActionConfirmation } from '../composables/useActionConfirmation'

interface WorkOrderChoice {
  moNo: string; productNo: string; customerNo: string; moQty: number; moUnitNo: string
  issueState: number; moState: number; dataStamp: number; processes: { haveComponent: boolean }[]
  components: WorkOrderComponent[]
}
interface ComponentDraft { component: WorkOrderComponent; status: 'existing' | 'new' | 'deleted' }

const props = defineProps<{
  orders: WorkOrderChoice[]
  developmentPreview: boolean
  saveComponents: (moNo: string, dataStamp: number, components: WorkOrderComponent[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()
const isDev = computed(() => import.meta.env.DEV && props.developmentPreview)
const { confirmationMessage, requestConfirmation, resolveConfirmation } = useActionConfirmation()
const productNo = ref('')
const customerNo = ref('')
const selectedNo = ref('')
const loadedNo = ref('')
const loadedStamp = ref(0)
const draft = ref<ComponentDraft[]>([])
const notice = ref('')
const error = ref('')
const choices = computed(() => props.orders.filter(row =>
  row.issueState === 2 && (row.moState === 2 || row.moState === 3) &&
  (!productNo.value.trim() || row.productNo.toUpperCase() === productNo.value.trim().toUpperCase()) &&
  (!customerNo.value.trim() || row.customerNo.toUpperCase() === customerNo.value.trim().toUpperCase()),
).sort((a, b) => a.moNo.localeCompare(b.moNo)))
const order = computed(() => props.orders.find(row => row.moNo === loadedNo.value))
const changes = computed(() => draft.value.some(row => row.status !== 'existing'))
const remaining = computed(() => Math.max(0, Math.floor(order.value?.moQty ?? 0) - draft.value.filter(row => row.status !== 'deleted').length))

function resetDraft(): void {
  draft.value = order.value?.components.map(component => ({ component: { ...component }, status: 'existing' })) ?? []
  loadedStamp.value = order.value?.dataStamp ?? 0
}

async function query(): Promise<void> {
  const row = choices.value.find(item => item.moNo === selectedNo.value)
  if (!row) { error.value = '請選擇符合條件的工單。'; return }
  if (changes.value && !await requestConfirmation('捨棄未儲存的元件異動？')) return
  notice.value = ''
  if (!row.processes.some(process => process.haveComponent) || !row.components.length ||
    row.components.some(component => !!component.inventoryNo?.trim())) {
    loadedNo.value = ''
    draft.value = []
    error.value = '工單需有元件流程及現存元件，且不可含庫房來源元件。'
    return
  }
  error.value = ''
  loadedNo.value = row.moNo
  resetDraft()
}

function appendComponent(generate: boolean): void {
  if (!order.value || !remaining.value) return
  let componentNo = ''
  if (generate) {
    const used = new Set([...props.orders.flatMap(row => row.components.map(item => item.componentNo.toUpperCase())),
      ...draft.value.map(row => row.component.componentNo.toUpperCase())])
    const prefix = order.value.moNo.slice(0, 38)
    for (let serial = 1; serial <= used.size + 1; serial++) {
      const candidate = prefix + '-C' + String(serial).padStart(3, '0')
      if (!used.has(candidate)) { componentNo = candidate; break }
    }
  }
  draft.value.push({
    component: { componentNo, goodQty: 1, unitNo: order.value.components[0]?.unitNo || order.value.moUnitNo, releaseState: 0 },
    status: 'new',
  })
  error.value = ''
}

function remove(row: ComponentDraft, index: number): void {
  if (row.component.releaseState === 1) return
  if (row.status === 'new') draft.value.splice(index, 1)
  else row.status = row.status === 'deleted' ? 'existing' : 'deleted'
}

async function confirmChanges(): Promise<void> {
  const current = order.value
  if (!isDev.value || !current || !changes.value) return
  const active = draft.value.filter(row => row.status !== 'deleted')
  const registered = new Set(props.orders.filter(row => row.moNo !== current.moNo)
    .flatMap(row => row.components.map(item => item.componentNo.toUpperCase())))
  const seen = new Set<string>()
  for (const row of active) {
    const no = row.component.componentNo.trim().toUpperCase()
    if (!no || no.length > 50 || !row.component.unitNo.trim() ||
      !Number.isFinite(row.component.goodQty) || row.component.goodQty < 0 || row.component.goodQty > 99_999_999.9999) {
      error.value = '元件編號、良品量或單位無效。'; return
    }
    if (seen.has(no) || row.status === 'new' && registered.has(no)) {
      error.value = '元件編號重複：' + no; return
    }
    seen.add(no)
  }
  if (active.length > Math.floor(current.moQty)) { error.value = '元件筆數不可超過工單量。'; return }
  if (!await requestConfirmation('確定儲存工單元件異動？')) return
  const next = active.map(row => row.status === 'new' ? {
    ...row.component, componentNo: row.component.componentNo.trim().toUpperCase(), unitNo: row.component.unitNo.trim().toUpperCase(),
  } : { ...row.component })
  if (!props.saveComponents(current.moNo, loadedStamp.value, next)) {
    error.value = '工單已變更或本機儲存失敗，請重新查詢。'; return
  }
  draft.value = next.map(component => ({ component, status: 'existing' }))
  loadedStamp.value++
  error.value = ''
  notice.value = '已儲存元件異動。'
}

async function close(): Promise<void> {
  if (!changes.value || await requestConfirmation('捨棄未儲存的元件異動？')) emit('close')
}
</script>

<template>
  <section class="component-page">
    <h2>工單元件資料維護作業</h2>
    <template v-if="isDev">
      <form class="toolbar" @submit.prevent="query">
        <label>產品 <input v-model="productNo" maxlength="50"></label>
        <label>客戶 <input v-model="customerNo" maxlength="50"></label>
        <label>工單 <select v-model="selectedNo">
          <option value="">請選擇</option><option v-for="row in choices" :key="row.moNo">{{ row.moNo }}</option>
        </select></label>
        <button type="submit">查詢</button>
      </form>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-if="notice" role="status">{{ notice }}</p>
      <template v-if="order">
        <div class="toolbar">
          <strong>{{ order.moNo }}</strong>
          <span>工單量 {{ order.moQty }} {{ order.moUnitNo }}</span><span>可再新增 {{ remaining }} 筆</span>
          <button type="button" :disabled="remaining === 0" @click="appendComponent(false)">新增元件</button>
          <button type="button" :disabled="remaining === 0" @click="appendComponent(true)">產生 DEV 編號</button>
          <button type="button" :disabled="!changes" @click="confirmChanges">確認</button>
        </div>
        <div class="grid"><table>
          <thead><tr><th>元件編號</th><th>良品量</th><th>單位</th><th>發放狀態</th><th>晶圓標示</th><th>槽位</th><th>異動</th><th>操作</th></tr></thead>
          <tbody>
            <tr v-for="(row, index) in draft" :key="index" :class="{ deleted: row.status === 'deleted' }">
              <td><input v-model="row.component.componentNo" aria-label="元件編號" maxlength="50" :readonly="row.status !== 'new'"></td>
              <td><input v-model.number="row.component.goodQty" aria-label="良品量" type="number" step="0.0001" min="0" :readonly="row.status !== 'new'"></td>
              <td><input v-model="row.component.unitNo" aria-label="單位" maxlength="30" :readonly="row.status !== 'new'"></td>
              <td>{{ row.component.releaseState === 1 ? '已發放' : '未發放' }}</td>
              <td><input v-model="row.component.waferIdText" aria-label="晶圓標示" maxlength="20" :readonly="row.status !== 'new'"></td>
              <td><input v-model="row.component.slotNo" aria-label="槽位" maxlength="3" :readonly="row.status !== 'new'"></td>
              <td>{{ row.status === 'new' ? '新增' : row.status === 'deleted' ? '刪除' : '現存' }}</td>
              <td><button type="button" :disabled="row.component.releaseState === 1" @click="remove(row, index)">{{ row.status === 'deleted' ? '復原' : '刪除' }}</button></td>
            </tr>
          </tbody>
        </table></div>
      </template>
    </template>
    <p v-else role="status">工單元件維護尚未接入正式服務。</p>
    <button type="button" @click="close">關閉</button>
    <ActionConfirmationDialog :message="confirmationMessage" @resolve="resolveConfirmation" />
  </section>
</template>

<style scoped>
.component-page { display: grid; gap: 12px; align-content: start; }
h2, p { margin: 0; }
.toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
.grid { overflow: auto; border: 1px solid #bcc7d7; background: white; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { padding: 7px 9px; border-bottom: 1px solid #dce4ed; white-space: nowrap; text-align: left; }
th { background: #eef3f9; }
td input { width: max(70px, 100%); min-width: 0; }
.deleted td { background: #f8e9e9; }
button { width: max-content; min-height: 30px; }
[role="alert"] { color: #9d2727; }
</style>
