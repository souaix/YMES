<script setup lang="ts">
import { computed, ref } from 'vue'
import ProductParameterSetup from './ProductParameterSetup.vue'
import ProductProcessEditor from './ProductProcessEditor.vue'
import ProductPropertyMaster from './ProductPropertyMaster.vue'
import { loadDevelopmentProductTypes, productTypeStorageKey, type ProductTypeRow } from './productTypeDevelopment'

const props = defineProps<{ currentUserNo: string; developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const rows = ref(props.developmentPreview ? loadDevelopmentProductTypes() : [])
const issueState = ref<ProductTypeRow['issueState']>(0)
const states = [{ value: 0, label: '未核准' }, { value: 1, label: '簽核中' }, { value: 2, label: '已核准' }, { value: -1, label: '不使用' }] as const
const keyword = ref('')
const query = ref('')
const visibleRows = computed(() => rows.value.filter(row => row.issueState === issueState.value && row.productType.toUpperCase().includes(query.value)))
const activeKey = ref('')
const active = computed(() => visibleRows.value.find(row => row.productType === activeKey.value) ?? visibleRows.value[0])
const marked = ref<string[]>([])
const editable = computed(() => props.developmentPreview && active.value?.issueState === 0)
const mode = ref<'add' | 'edit' | 'copy' | ''>('')
const child = ref<'process' | 'property' | 'parameter' | ''>('')
const draft = ref({ productType: '', productTypeCode: '', description: '' })
const editTarget = ref<ProductTypeRow>()
const error = ref('')
const notice = ref('')
const currentUser = computed(() => props.currentUserNo || 'DEV001')

function saveRows(next: ProductTypeRow[]): boolean {
  if (!props.developmentPreview) return false
  try {
    localStorage.setItem(productTypeStorageKey, JSON.stringify(next))
  } catch {
    error.value = '無法儲存本機 DEV 設定。'
    return false
  }
  rows.value = next
  error.value = ''
  return true
}

function openEditor(nextMode: typeof mode.value): void {
  if (!props.developmentPreview || (nextMode === 'edit' && !editable.value)) return
  const source = nextMode === 'add' ? undefined : active.value
  draft.value = { productType: source?.productType ?? '', productTypeCode: source?.productTypeCode ?? '', description: source?.description ?? '' }
  editTarget.value = source
  error.value = ''
  mode.value = nextMode
}

function saveEditor(close: boolean): void {
  const productType = draft.value.productType.trim().toUpperCase()
  if (!productType) { error.value = '請輸入產品類別。'; return }
  const original = editTarget.value
  const editing = mode.value === 'edit'
  if (editing && (original?.productType !== productType || original.issueState !== 0)) return
  if (!editing && rows.value.some(row => row.productType === productType)) { error.value = '產品類別已存在。'; return }
  const now = new Date().toISOString()
  const info = { productType, productTypeCode: draft.value.productTypeCode.trim(), description: draft.value.description.trim() }
  const newRow: ProductTypeRow = editing && original
    ? { ...original, ...info, reviser: currentUser.value, reviseDate: now, dataStamp: original.dataStamp + 1 }
    : { ...info, creator: currentUser.value, createDate: now, issueState: 0, dataStamp: 0 }
  if (mode.value === 'copy' && original) {
    const details = JSON.parse(JSON.stringify({ processes: original.processes, properties: original.properties, parameters: original.parameters }))
    Object.assign(newRow, details)
    for (const row of newRow.properties ?? []) row.productNo = productType
    for (const row of newRow.parameters?.ops ?? []) row.productNo = productType
  }
  const next = editing ? rows.value.map(row => row.productType === productType ? newRow : row) : [...rows.value, newRow]
  if (!saveRows(next)) return
  issueState.value = 0
  query.value = ''
  keyword.value = ''
  activeKey.value = productType
  marked.value = []
  notice.value = '產品類別已儲存。'
  mode.value = close ? '' : 'edit'
  editTarget.value = newRow
  draft.value.productType = productType
}

function changeState(action: 'approve' | 'delete'): void {
  if (!props.developmentPreview || !active.value || issueState.value === 1 || (action === 'approve' && !editable.value)) return
  const targets = new Set([...marked.value, active.value.productType])
  if (!window.confirm(action === 'approve' ? '確定核准選取的產品類別？' : '確定刪除或停用選取的產品類別？')) return
  const now = new Date().toISOString()
  const next = rows.value.flatMap(row => {
    if (!targets.has(row.productType) || row.issueState !== issueState.value) return [row]
    if (action === 'delete' && row.issueState !== 2) return []
    const newState = action === 'approve' ? 2 : -1
    return [{ ...row, issueState: newState as ProductTypeRow['issueState'], reviser: currentUser.value, reviseDate: now, dataStamp: row.dataStamp + 1 }]
  })
  if (!saveRows(next)) return
  marked.value = []
  notice.value = action === 'approve' ? '選取的類別已核准。' : '選取的類別已刪除或停用。'
}

function saveDetail(detail: Partial<Pick<ProductTypeRow, 'processes' | 'properties' | 'parameters'>>): void {
  const key = active.value?.productType
  if (key) saveRows(rows.value.map(row => row.productType === key ? { ...row, ...detail } : row))
}

const productContext = computed(() => ({ productNo: active.value?.productType, issueState: active.value?.issueState, productVersion: '-1' }))
</script>

<template>
  <main class="type-master">
    <header><h1>產品類別資料</h1><button type="button" @click="emit('close')">關閉</button></header>
    <p v-if="!developmentPreview">產品類別資料等待正式 API 接入。</p>
    <template v-else>
      <form class="toolbar" @submit.prevent="query = keyword.trim().toUpperCase(); marked = []">
        <label v-for="state in states" :key="state.value"><input v-model="issueState" type="radio" :value="state.value" @change="marked = []" /> {{ state.label }}</label>
        <input v-model="keyword" type="search" aria-label="產品類別" placeholder="產品類別" /><button>查詢</button>
      </form>
      <div class="toolbar">
        <button @click="openEditor('add')">ADD</button><button :disabled="!editable" @click="openEditor('edit')">EDIT</button>
        <button :disabled="!active || issueState === 1" @click="changeState('delete')">DEL</button><button :disabled="!active" @click="openEditor('copy')">Copy</button>
        <button :disabled="!editable" @click="changeState('approve')">Approve</button>
        <button :disabled="!active" @click="child = 'property'">Property</button>
        <button :disabled="!active?.processes?.length" @click="child = 'parameter'">Parameter Setup</button>
      </div>
      <div class="grid">
        <table>
          <thead><tr><th>選取</th><th>ProductType</th><th>ProductTypeCode</th><th>Description</th><th>Creator</th><th>CreateDate</th><th>Reviser</th><th>ReviseDate</th></tr></thead>
          <tbody><tr v-for="row in visibleRows" :key="row.productType" :class="{ active: active?.productType === row.productType }" @click="activeKey = row.productType">
            <td><input v-model="marked" type="checkbox" :value="row.productType" :aria-label="`選取 ${row.productType}`" /></td>
            <td><button type="button" @click="activeKey = row.productType">{{ row.productType }}</button></td><td>{{ row.productTypeCode }}</td><td>{{ row.description }}</td>
            <td>{{ row.creator }}</td><td>{{ row.createDate.replace('T', ' ').slice(0, 19) }}</td><td>{{ row.reviser }}</td><td>{{ row.reviseDate?.replace('T', ' ').slice(0, 19) }}</td>
          </tr><tr v-if="!visibleRows.length"><td colspan="8">沒有符合的產品類別。</td></tr></tbody>
        </table>
      </div>
      <section v-if="active" class="detail">
        <header><h2>Product Process</h2><button :disabled="!editable" @click="child = 'process'">Set …</button></header>
        <div class="grid"><table><thead><tr><th>MOTypeNo</th><th>PSNo</th><th>PSOrder</th><th>ProcessNo</th><th>ProcessVersion</th><th>Default</th></tr></thead>
          <tbody><tr v-for="(row, index) in active.processes ?? []" :key="index"><td>{{ row.moTypeNo }}</td><td>{{ row.psNo }}</td><td>{{ row.psOrder }}</td><td>{{ row.processNo }}</td><td>{{ row.processVersion }}</td><td>{{ Number(row.defaultProcess) ? 'Yes' : 'No' }}</td></tr>
          <tr v-if="!active.processes?.length"><td colspan="6">沒有設定流程。</td></tr></tbody></table></div>
      </section>
      <p v-if="error && !mode" class="error" role="alert">{{ error }}</p><p v-if="notice" role="status">{{ notice }}</p>
    </template>
    <div v-if="mode" class="overlay">
      <form class="editor" role="dialog" aria-modal="true" aria-labelledby="product-type-edit-title" @submit.prevent="saveEditor(true)">
        <h2 id="product-type-edit-title">產品類別 {{ mode.toUpperCase() }}</h2>
        <label>ProductType <input v-model="draft.productType" :readonly="mode === 'edit'" maxlength="50" required /></label>
        <label>ProductTypeCode <input v-model="draft.productTypeCode" maxlength="10" /></label>
        <label>Description <textarea v-model="draft.description" maxlength="255" /></label>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <footer><button>Confirm</button><button type="button" @click="saveEditor(false)">Apply</button><button type="button" @click="mode = ''; error = ''">Close</button></footer>
      </form>
    </div>
    <div v-if="child && active" class="overlay">
      <ProductProcessEditor v-if="child === 'process'" :product="productContext" :processes="active.processes ?? []" :development-preview="editable" @save="saveDetail({ processes: $event })" @close="child = ''" />
      <ProductPropertyMaster v-else-if="child === 'property'" :product="{ ...productContext, productVersion: '' }" :properties="active.properties ?? []" :development-preview="developmentPreview" :can-edit="developmentPreview" @save="saveDetail({ properties: $event })" @close="child = ''" />
      <ProductParameterSetup v-else :product="productContext" :processes="active.processes ?? []" :parameters="active.parameters" :development-preview="developmentPreview" :can-edit="editable" @save="saveDetail({ parameters: $event })" @close="child = ''" />
    </div>
  </main>
</template>

<style scoped>
.type-master{display:flex;flex-direction:column;gap:8px;min-width:0;padding:10px;background:#fff;color:#292820;font:12px "Segoe UI","Microsoft JhengHei",sans-serif}.type-master header,.toolbar,footer{display:flex;align-items:center;gap:8px}.type-master header{justify-content:space-between}.toolbar{flex-wrap:wrap;padding:6px;background:#f5f3eb;border:1px solid #dedbd0}.toolbar label{white-space:nowrap}h1,h2,p{margin:0}h1{font-size:17px}h2{font-size:14px}
.grid{overflow:auto;border:1px solid #d9d6cb}table{width:100%;border-collapse:collapse;text-align:left;white-space:nowrap}th,td{padding:6px;border:1px solid #e3e0d6}thead{background:#f5f3eb}.active{background:#fff2c6}.detail{display:grid;gap:6px}button,input,textarea{font:inherit}button{padding:4px 10px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.5}.error{color:#a32020}
.overlay{position:fixed;inset:0;z-index:91;display:grid;place-items:center;padding:12px;background:#0005}.editor{display:grid;gap:12px;width:min(520px,95vw);padding:16px;background:#fff;box-shadow:0 8px 30px #0004}.editor label{display:grid;gap:4px}.editor input,.editor textarea{padding:5px;border:1px solid #aaa797;background:#fff5d6}.editor input:read-only{background:#f4f3ec}footer{justify-content:flex-end}
</style>
