<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

interface Equipment {
  equipmentNo: string
  equipmentType: string
  equipmentName: string
  engineerGroupNo: string
  vendorNo: string
  modelNo: string
  assetNo: string
  equipmentClass: string
  equipmentClassNo: string
  description: string
  capacity: number
  loadPort: number
  autoFlag: number
  eaController: string
  eqpRecipe: number
  qcListNo: string
  maxTime: number | null
  fixEqpTime: number | null
  varEqpTime: number | null
  countEqpUnitQty: number
  countEquipmentUnitQty: number
  queueServer: string
  requestMsgQueue: string
  responseMsgQueue: string
  requestMsgQueueMes: string
  responseMsgQueueEap: string
  chamberFlag: boolean
  belongEquipmentNo: string | null
  issueState: number
  dataStamp: number
  createdAt: string | null
  revisedAt: string | null
}
type EquipmentWrite = Omit<Equipment, 'chamberFlag' | 'belongEquipmentNo' | 'issueState' | 'createdAt' | 'revisedAt'>

const props = defineProps<{ apiBaseUrl: string }>()
const appBase = import.meta.env.BASE_URL.replace(/\/+$/, '')
const equipment = ref<Equipment[]>([])
const chambers = ref<Equipment[]>([])
const selectedEquipmentNo = ref('')
const issueState = ref(0)
const keyword = ref('')
const loading = ref(false)
const chamberLoading = ref(false)
const busy = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const chamberError = ref('')
const equipmentFormMode = ref<'new' | 'edit' | 'copy' | null>(null)
const equipmentForm = ref<EquipmentWrite>({} as EquipmentWrite)
const chamberFormMode = ref<'new' | 'edit' | null>(null)
const chamberForm = ref({ equipmentNo: '', belongEquipmentNo: '', equipmentName: '', description: '', dataStamp: 0 })
let initialSelectedEquipmentNo = ''
let chamberRequestId = 0

const selected = computed(() =>
  equipment.value.find((item) => item.equipmentNo === selectedEquipmentNo.value) ?? null,
)

function makeApiUrl(path: string): string {
  const configuredBase = props.apiBaseUrl.trim()
  if (!configuredBase) throw new Error('目前環境尚未設定 API 位址。')
  const base = new URL(configuredBase, window.location.origin)
  const baseWithSlash = base.toString().replace(/\/+$/, '') + '/'
  return new URL(path.replace(/^\/+/, ''), baseWithSlash).toString()
}

async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(makeApiUrl(path), {
    credentials: 'include',
    ...init,
  })

  if (response.status === 401) {
    window.location.assign(appBase + '/')
    throw new Error('登入狀態已逾期，請重新登入。')
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as {
      title?: string
      detail?: string
      message?: string
    } | null
    throw new Error(problem?.detail || problem?.message || problem?.title || `API 回應錯誤 (${response.status})`)
  }

  if (response.status === 204) return undefined as T
  return await response.json() as T
}

function updateFilterAddress(): void {
  const url = new URL(window.location.href)
  url.search = ''
  url.searchParams.set('issueState', String(issueState.value))
  if (keyword.value.trim()) url.searchParams.set('keyword', keyword.value.trim())
  window.history.replaceState(window.history.state, '', url.pathname + url.search)
}

function newEquipmentValues(): EquipmentWrite {
  return {
    equipmentNo: '', equipmentType: '', equipmentName: '', engineerGroupNo: '', vendorNo: '', modelNo: '', assetNo: '',
    equipmentClass: '一般設備', equipmentClassNo: '', description: '', capacity: -1, loadPort: -1, autoFlag: 0,
    eaController: 'N/A', eqpRecipe: 0, qcListNo: '', maxTime: null, fixEqpTime: 0, varEqpTime: 0,
    countEqpUnitQty: 1, countEquipmentUnitQty: 1, queueServer: '', requestMsgQueue: '', responseMsgQueue: '',
    requestMsgQueueMes: '', responseMsgQueueEap: '', dataStamp: 0,
  }
}

function openEquipmentForm(mode: 'new' | 'edit' | 'copy'): void {
  const item = selected.value
  equipmentFormMode.value = mode
  if (mode === 'new' || !item) equipmentForm.value = newEquipmentValues()
  else {
    equipmentForm.value = { ...item, equipmentNo: mode === 'copy' ? '' : item.equipmentNo, dataStamp: mode === 'copy' ? 0 : item.dataStamp }
  }
}

function openChamberForm(mode: 'new' | 'edit', chamber?: Equipment): void {
  const parent = selected.value
  if (!parent) return
  chamberFormMode.value = mode
  chamberForm.value = mode === 'new'
    ? { equipmentNo: '', belongEquipmentNo: parent.equipmentNo, equipmentName: '', description: '', dataStamp: 0 }
    : { equipmentNo: chamber?.equipmentNo ?? '', belongEquipmentNo: parent.equipmentNo, equipmentName: chamber?.equipmentName ?? '', description: chamber?.description ?? '', dataStamp: chamber?.dataStamp ?? 0 }
}

async function saveEquipment(): Promise<void> {
  const isEdit = equipmentFormMode.value === 'edit'
  busy.value = true; errorMessage.value = ''
  try {
    const item = await requestJson<Equipment>(isEdit ? `api/equipment/${encodeURIComponent(String(equipmentForm.value.equipmentNo))}` : 'api/equipment', {
      method: isEdit ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(equipmentForm.value),
    })
    equipmentFormMode.value = null
    successMessage.value = `設備 ${item.equipmentNo} 已${isEdit ? '更新' : '建立'}。`
    selectedEquipmentNo.value = item.equipmentNo
    await loadEquipment()
  } catch (error) { errorMessage.value = error instanceof Error ? error.message : '設備資料儲存失敗。' }
  finally { busy.value = false }
}

async function saveChamber(): Promise<void> {
  const parent = selected.value
  if (!parent) return
  const isEdit = chamberFormMode.value === 'edit'
  busy.value = true; chamberError.value = ''
  try {
    const root = `api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers`
    const endpoint = isEdit ? `${root}/${encodeURIComponent(chamberForm.value.equipmentNo)}` : root
    await requestJson<Equipment>(endpoint, { method: isEdit ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(chamberForm.value) })
    chamberFormMode.value = null
    successMessage.value = `Chamber ${chamberForm.value.equipmentNo} 已${isEdit ? '更新' : '建立'}。`
    await loadChambers(parent.equipmentNo)
  } catch (error) { chamberError.value = error instanceof Error ? error.message : 'Chamber 資料儲存失敗。' }
  finally { busy.value = false }
}

async function loadChambers(equipmentNo: string): Promise<void> {
  const requestId = ++chamberRequestId
  chamberError.value = ''
  chambers.value = []
  if (!equipmentNo) return

  chamberLoading.value = true
  try {
    const result = await requestJson<Equipment[]>(`api/equipment/${encodeURIComponent(equipmentNo)}/chambers`)
    if (requestId === chamberRequestId) chambers.value = result
  } catch (error) {
    if (requestId === chamberRequestId) {
      chamberError.value = error instanceof Error ? error.message : 'Chamber 資料載入失敗。'
    }
  } finally {
    if (requestId === chamberRequestId) chamberLoading.value = false
  }
}

async function loadEquipment(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const query = new URLSearchParams({ issueState: String(issueState.value) })
    if (keyword.value.trim()) query.set('keyword', keyword.value.trim())
    const rows = await requestJson<Equipment[]>(`api/equipment?${query.toString()}`)
    equipment.value = rows

    const previous = selectedEquipmentNo.value
    const requested = initialSelectedEquipmentNo
    const next = rows.find((item) => item.equipmentNo === previous)
      ?? rows.find((item) => item.equipmentNo === requested)
      ?? rows[0]
    selectedEquipmentNo.value = next?.equipmentNo ?? ''
    initialSelectedEquipmentNo = ''
    await loadChambers(selectedEquipmentNo.value)
    updateFilterAddress()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備資料載入失敗。'
  } finally {
    loading.value = false
  }
}

async function selectEquipment(item: Equipment): Promise<void> {
  if (selectedEquipmentNo.value === item.equipmentNo) return
  selectedEquipmentNo.value = item.equipmentNo
  successMessage.value = ''
  await loadChambers(item.equipmentNo)
}

async function approveSelected(): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return

  busy.value = true
  errorMessage.value = ''
  try {
    await requestJson<Equipment>(`api/equipment/${encodeURIComponent(item.equipmentNo)}/approve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dataStamp: item.dataStamp }),
    })
    successMessage.value = `設備 ${item.equipmentNo} 已核准。`
    selectedEquipmentNo.value = ''
    await loadEquipment()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '設備核准失敗。'
  } finally {
    busy.value = false
  }
}

async function deleteSelected(): Promise<void> {
  const item = selected.value
  if (!item || busy.value) return
  const action = item.issueState === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action}設備 ${item.equipmentNo}？`)) return

  busy.value = true
  errorMessage.value = ''
  try {
    const query = new URLSearchParams({ issueState: String(item.issueState), dataStamp: String(item.dataStamp) })
    await requestJson<Equipment>(`api/equipment/${encodeURIComponent(item.equipmentNo)}?${query.toString()}`, {
      method: 'DELETE',
    })
    successMessage.value = `設備 ${item.equipmentNo} 已${action}。`
    selectedEquipmentNo.value = ''
    await loadEquipment()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : `設備${action}失敗。`
  } finally {
    busy.value = false
  }
}

async function approveChamber(chamber: Equipment): Promise<void> {
  const parent = selected.value
  if (!parent || busy.value) return
  busy.value = true
  chamberError.value = ''
  try {
    await requestJson<Equipment>(`api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers/${encodeURIComponent(chamber.equipmentNo)}/approve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ dataStamp: chamber.dataStamp }),
    })
    successMessage.value = `Chamber ${chamber.equipmentNo} 已核准。`
    await loadChambers(parent.equipmentNo)
  } catch (error) {
    chamberError.value = error instanceof Error ? error.message : 'Chamber 核准失敗。'
  } finally {
    busy.value = false
  }
}

async function deleteChamber(chamber: Equipment): Promise<void> {
  const parent = selected.value
  if (!parent || busy.value) return
  const action = chamber.issueState === 2 ? '停用' : '刪除'
  if (!window.confirm(`確定${action} Chamber ${chamber.equipmentNo}？`)) return

  busy.value = true
  chamberError.value = ''
  try {
    const query = new URLSearchParams({ issueState: String(chamber.issueState), dataStamp: String(chamber.dataStamp) })
    await requestJson<Equipment>(`api/equipment/${encodeURIComponent(parent.equipmentNo)}/chambers/${encodeURIComponent(chamber.equipmentNo)}?${query.toString()}`, {
      method: 'DELETE',
    })
    successMessage.value = `Chamber ${chamber.equipmentNo} 已${action}。`
    await loadChambers(parent.equipmentNo)
  } catch (error) {
    chamberError.value = error instanceof Error ? error.message : `Chamber ${action}失敗。`
  } finally {
    busy.value = false
  }
}

function stateName(state: number): string {
  return ({ 0: '未簽核', 1: '簽核中', 2: '已簽核', '-1': '不使用' } as Record<string, string>)[String(state)] ?? '未知'
}

function formatNumber(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : Number(value).toLocaleString('zh-TW', { maximumFractionDigits: 3 })
}

function navigateHome(): void {
  window.history.pushState({}, '', appBase + '/')
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function printPage(): void {
  window.print()
}

function downloadCsv(): void {
  const headers = ['設備編號', '設備類別資料', '設備名稱', '管理群組', '設備分類', '供應商編號', '設備型號', '財產編號', '批量限制', '批數限制', '預設產能', '機時單位批量', '固定機時', '變動機時', '點檢表編號', '自動設備', '說明']
  const rows = equipment.value.map((item) => [item.equipmentNo, item.equipmentType, item.equipmentName, item.engineerGroupNo, item.equipmentClass, item.vendorNo, item.modelNo, item.assetNo, item.capacity, item.loadPort, item.maxTime, item.countEqpUnitQty, item.fixEqpTime, item.varEqpTime, item.qcListNo, item.autoFlag === 1 ? '是' : '否', item.description])
  const csv = [headers, ...rows].map((row) => row.map((value) => `"${String(value ?? '').replaceAll('"', '""')}"`).join(',')).join('\r\n')
  const url = URL.createObjectURL(new Blob(['\uFEFF', csv], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = 'equipment.csv'
  link.click()
  URL.revokeObjectURL(url)
}

function handlePopState(): void {
  const params = new URLSearchParams(window.location.search)
  issueState.value = Number(params.get('issueState') ?? 0)
  keyword.value = params.get('keyword') ?? ''
  initialSelectedEquipmentNo = params.get('selectedEquipmentNo') ?? ''
  void loadEquipment()
}

onMounted(() => {
  const params = new URLSearchParams(window.location.search)
  issueState.value = Number(params.get('issueState') ?? 0)
  keyword.value = params.get('keyword') ?? ''
  initialSelectedEquipmentNo = params.get('selectedEquipmentNo') ?? ''
  window.addEventListener('popstate', handlePopState)
  void loadEquipment()
})

onBeforeUnmount(() => window.removeEventListener('popstate', handlePopState))
</script>

<template>
  <div class="equipment-master-page">
    <form class="equipment-query" @submit.prevent="loadEquipment">
      <fieldset class="equipment-state-filter">
        <legend>設備</legend>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="0" />未簽核</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="1" />簽核中</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="2" />已簽核</label>
        <label><input v-model.number="issueState" type="radio" name="issueState" :value="-1" />不使用</label>
      </fieldset>
      <span class="equipment-query-label">設備</span>
      <select aria-label="查詢欄位" disabled><option>設備編號</option></select>
      <input v-model="keyword" class="equipment-keyword" type="search" aria-label="設備編號" />
      <span class="equipment-like">%</span>
      <button class="equipment-query-button" type="submit" :disabled="loading">查詢</button>
    </form>

    <div v-if="errorMessage" class="equipment-message error" role="alert">{{ errorMessage }}</div>
    <div v-if="successMessage" class="equipment-message success" role="status">{{ successMessage }}</div>

    <section class="equipment-list-panel" :aria-busy="loading">
      <header class="equipment-panel-heading"><h2>設備</h2><span>設備基本資料</span></header>
      <div class="equipment-table-scroll">
        <table class="equipment-table">
          <thead><tr>
            <th>設備編號</th><th>設備類別資料</th><th>設備名稱</th><th>管理群組</th><th>設備分類</th><th>供應商編號</th><th>設備型號</th><th>財產編號</th><th>批量限制</th><th>批數限制</th><th>預設產能</th><th>機時單位批量</th><th>固定機時</th><th>變動機時</th><th>點檢表編號</th><th>自動設備</th><th>說明</th>
          </tr></thead>
          <tbody>
            <tr
              v-for="item in equipment"
              :key="item.equipmentNo"
              :class="{ 'selected-row': selectedEquipmentNo === item.equipmentNo }"
              :aria-selected="selectedEquipmentNo === item.equipmentNo"
              tabindex="0"
              @click="selectEquipment(item)"
              @keydown.enter.prevent="selectEquipment(item)"
              @keydown.space.prevent="selectEquipment(item)"
            >
              <td class="equipment-code">{{ item.equipmentNo }}</td><td>{{ item.equipmentType }}</td><td>{{ item.equipmentName }}</td><td>{{ item.engineerGroupNo }}</td><td>{{ item.equipmentClass }}</td><td>{{ item.vendorNo }}</td><td>{{ item.modelNo }}</td><td>{{ item.assetNo }}</td><td>{{ formatNumber(item.capacity) }}</td><td>{{ item.loadPort }}</td><td>{{ formatNumber(item.maxTime) }}</td><td>{{ item.countEqpUnitQty }}</td><td>{{ formatNumber(item.fixEqpTime) }}</td><td>{{ formatNumber(item.varEqpTime) }}</td><td>{{ item.qcListNo }}</td><td>{{ item.autoFlag === 1 ? '是' : '否' }}</td><td>{{ item.description }}</td>
            </tr>
            <tr v-if="!loading && equipment.length === 0"><td colspan="17" class="equipment-empty">沒有符合條件的設備資料</td></tr>
            <tr v-if="loading && equipment.length === 0"><td colspan="17" class="equipment-empty">設備資料載入中…</td></tr>
          </tbody>
        </table>
      </div>

      <aside class="equipment-actions" aria-label="設備主檔功能">
        <button class="equipment-action primary" type="button" @click="openEquipmentForm('new')">新增(A)</button>
        <button v-if="selected && selected.issueState === 0" class="equipment-action" type="button" @click="openEquipmentForm('edit')">修改(E)</button>
        <button v-if="selected && [0, 2, -1].includes(selected.issueState)" class="equipment-action danger" type="button" :disabled="busy" @click="deleteSelected">{{ selected.issueState === 2 ? '停用(D)' : '刪除(D)' }}</button>
        <button v-if="selected" class="equipment-action" type="button" @click="openEquipmentForm('copy')">複製(C)</button>
        <button v-if="selected && selected.issueState === 0" class="equipment-action" type="button" :disabled="busy" @click="approveSelected">核准</button>
        <button class="equipment-action" type="button" disabled title="設備屬性 API 尚未提供">屬性</button>
        <button class="equipment-action" type="button" disabled title="設備參數 API 尚未提供">參數</button>
        <button class="equipment-action" type="button" disabled title="點檢表設定 API 尚未提供">點檢表</button>
        <button class="equipment-action" type="button" @click="downloadCsv">Download</button>
        <button class="equipment-action" type="button" @click="printPage">Print</button>
        <button class="equipment-action" type="button" disabled title="設備匯入 API 尚未提供">設備匯入</button>
        <button class="equipment-action" type="button" @click="navigateHome">關閉</button>
      </aside>
      <footer class="equipment-list-footer">資料總筆數 <strong>{{ equipment.length }}</strong></footer>
    </section>

    <details class="equipment-chamber-panel" open>
      <summary>設備附屬 Chamber 資料（{{ selected?.equipmentNo ?? '未選取設備' }}）</summary>
      <div v-if="selected" class="equipment-detail-panel">
        <div class="equipment-detail-header">
          <div><h2>{{ selected.equipmentNo }}　{{ selected.equipmentName }}</h2><p>Chamber 子設備</p></div>
          <button v-if="[0, 2].includes(selected.issueState)" class="equipment-small-action" type="button" @click="openChamberForm('new')">新增 Chamber</button>
        </div>
        <div v-if="chamberError" class="equipment-message error" role="alert">{{ chamberError }}</div>
        <div v-else-if="chamberLoading" class="equipment-detail-note">Chamber 資料載入中…</div>
        <div v-else-if="chambers.length === 0" class="equipment-detail-note">此設備目前沒有 Chamber 子設備。</div>
        <div v-else class="equipment-chamber-scroll">
          <table class="equipment-table equipment-chamber-table">
            <thead><tr><th>Chamber 編號</th><th>名稱</th><th>狀態</th><th>操作</th></tr></thead>
            <tbody>
              <tr v-for="chamber in chambers" :key="chamber.equipmentNo">
                <td>{{ chamber.equipmentNo }}</td><td>{{ chamber.equipmentName }}</td><td>{{ stateName(chamber.issueState) }}</td>
                <td class="equipment-chamber-actions">
                  <template v-if="chamber.issueState === 0">
                    <button type="button" @click="openChamberForm('edit', chamber)">修改</button>
                    <button type="button" :disabled="busy" @click="approveChamber(chamber)">核准</button>
                  </template>
                  <button v-if="[0, 2, -1].includes(chamber.issueState)" type="button" :disabled="busy" @click="deleteChamber(chamber)">{{ chamber.issueState === 2 ? '停用' : '刪除' }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </details>

    <div v-if="equipmentFormMode" class="equipment-form-overlay" @click.self="equipmentFormMode = null">
      <form class="equipment-win-form" @submit.prevent="saveEquipment">
        <header><strong>設備基本資料定義 ({{ equipmentFormMode === 'edit' ? 'EDIT' : equipmentFormMode === 'copy' ? 'COPY' : 'ADD' }}) (EP104)</strong><button type="button" aria-label="關閉" @click="equipmentFormMode = null">×</button></header>
        <fieldset><legend>設備主檔資料</legend>
          <div class="equipment-form-columns">
            <div class="equipment-form-column">
              <label><span>設備編號</span><input v-model="equipmentForm.equipmentNo" required :readonly="equipmentFormMode === 'edit'" /></label>
              <label><span>設備名稱</span><input v-model="equipmentForm.equipmentName" required /></label>
              <label><span>設備分類</span><select class="equipment-kd-field" v-model="equipmentForm.equipmentClass"><option>一般設備</option><option>生產設備</option><option>測試設備</option><option>其他設備</option></select></label>
              <label><span>設備型號</span><input v-model="equipmentForm.modelNo" /></label>
              <label><span>批量限制</span><input class="equipment-kd-field" v-model.number="equipmentForm.capacity" type="number" min="-1" /><small>(-1 代表不限制)</small></label>
              <label><span>預設產能</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.maxTime" type="number" min="0" step="0.01" /><small>小時</small></span></label>
              <label><span>固定機時</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.fixEqpTime" type="number" min="0" step="0.01" /><small>分鐘</small></span></label>
              <label><span>點檢表編號</span><input v-model="equipmentForm.qcListNo" /></label>
              <label class="equipment-long-field"><span>說明</span><textarea v-model="equipmentForm.description" rows="5" /></label>
            </div>
            <div class="equipment-form-column">
              <label><span>設備類別資料</span><input v-model="equipmentForm.equipmentType" required /></label>
              <label><span>管理群組</span><input v-model="equipmentForm.engineerGroupNo" required /></label>
              <label><span>供應商編號</span><input v-model="equipmentForm.vendorNo" required /></label>
              <label><span>財產編號</span><input v-model="equipmentForm.assetNo" /></label>
              <label><span>批數限制</span><input class="equipment-kd-field" v-model.number="equipmentForm.loadPort" type="number" min="-1" /><small>(-1 代表不限制)</small></label>
              <label><span>機時單位批量</span><input v-model.number="equipmentForm.countEqpUnitQty" type="number" min="0" /></label>
              <label><span>變動機時</span><span class="equipment-number-unit"><input v-model.number="equipmentForm.varEqpTime" type="number" min="0" step="0.01" /><small>分鐘</small></span></label>
              <div class="equipment-auto-field"><span>自動設備</span><span class="equipment-radio-set"><label><input v-model.number="equipmentForm.autoFlag" type="radio" :value="1" />是</label><label><input v-model.number="equipmentForm.autoFlag" type="radio" :value="0" />否</label></span></div>
              <fieldset class="equipment-message-settings"><legend>訊息佇列設定</legend><label><span>佇列伺服器</span><input v-model="equipmentForm.queueServer" /></label><label><span>請求訊息佇列</span><input v-model="equipmentForm.requestMsgQueue" /></label><label><span>回應訊息佇列</span><input v-model="equipmentForm.responseMsgQueue" /></label><label><span>MES 請求佇列</span><input v-model="equipmentForm.requestMsgQueueMes" /></label><label><span>EAP 回應佇列</span><input v-model="equipmentForm.responseMsgQueueEap" /></label><label><span>財產分類編號</span><input v-model="equipmentForm.equipmentClassNo" /></label><label><span>EAP Controller</span><input v-model="equipmentForm.eaController" /></label><label><span>Recipe 管理</span><select v-model.number="equipmentForm.eqpRecipe"><option :value="1">是</option><option :value="0">否</option></select></label></fieldset>
            </div>
          </div>
        </fieldset>
        <div class="equipment-form-audit"><span>建立人　—</span><span>建立日期　{{ selected?.createdAt ?? '—' }}</span><span>修改人　—</span><span>修改日期　{{ selected?.revisedAt ?? '—' }}</span></div>
        <footer><button class="equipment-form-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="equipmentFormMode = null">關閉</button><button type="button" disabled>套用</button></footer>
      </form>
    </div>

    <div v-if="chamberFormMode" class="equipment-form-overlay" @click.self="chamberFormMode = null">
      <form class="equipment-chamber-form" @submit.prevent="saveChamber">
        <header><strong>Chamber 基本資料 ({{ chamberFormMode === 'edit' ? 'EDIT' : 'ADD' }})</strong><button type="button" aria-label="關閉" @click="chamberFormMode = null">×</button></header>
        <label><span>設備編號</span><input v-model="chamberForm.equipmentNo" required :readonly="chamberFormMode === 'edit'" /></label>
        <label><span>所屬設備</span><input v-model="chamberForm.belongEquipmentNo" readonly /></label>
        <label><span>設備名稱</span><input v-model="chamberForm.equipmentName" required /></label>
        <label><span>說明</span><textarea v-model="chamberForm.description" rows="3" /></label>
        <footer><button class="equipment-form-confirm" type="submit" :disabled="busy">確定</button><button type="button" @click="chamberFormMode = null">關閉</button></footer>
      </form>
    </div>
  </div>
</template>

<style scoped>
.equipment-master-page{min-width:0;padding:10px 14px 24px;background:#fff;color:#30363b;font-family:'Segoe UI','Microsoft JhengHei',sans-serif;font-size:12px}
.equipment-action,.equipment-small-action{font-family:inherit;cursor:pointer}
.equipment-form-overlay{position:fixed;inset:0;z-index:30;display:grid;place-items:center;padding:14px;background:#0005}
.equipment-win-form,.equipment-chamber-form{display:flex;width:min(1220px,98vw);max-height:96vh;flex-direction:column;gap:10px;overflow:auto;padding:12px 16px;border:1px solid #aaa;background:#fff;color:#444;box-shadow:0 10px 35px #0004;font:14px 'Segoe UI','Microsoft JhengHei',sans-serif}
.equipment-win-form>header,.equipment-chamber-form>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}
.equipment-win-form>header button,.equipment-chamber-form>header button{width:32px;height:30px;border:1px solid #ccc;background:#fafafa;font-size:20px}
.equipment-win-form>fieldset{min-width:0;margin:0;padding:12px;border:1px solid #aaa}
.equipment-win-form>fieldset>legend,.equipment-message-settings>legend{padding:0 6px;color:#555}
.equipment-form-columns{display:grid;grid-template-columns:1fr 1fr;gap:26px}
.equipment-form-column{display:grid;align-content:start;gap:9px}
.equipment-form-column>label,.equipment-chamber-form>label{display:grid;grid-template-columns:minmax(115px,30%) minmax(0,1fr) auto;align-items:center;gap:7px}
.equipment-form-column input,.equipment-form-column select,.equipment-form-column textarea,.equipment-chamber-form input,.equipment-chamber-form textarea{width:100%;min-width:0;min-height:32px;padding:4px 7px;border:1px solid #888;border-radius:0;background:#fff;font:inherit}
.equipment-form-column input:not([readonly]),.equipment-form-column select{border-color:#b3a765}.equipment-form-column input[required],.equipment-form-column input.equipment-kd-field,.equipment-form-column select.equipment-kd-field,.equipment-chamber-form input[required]{background:#fff1aa}
.equipment-form-column input[readonly],.equipment-chamber-form input[readonly]{background:#eee}
.equipment-number-unit{display:flex;align-items:center;gap:8px}.equipment-number-unit input{max-width:150px}.equipment-form-column small{white-space:nowrap;color:#666}
.equipment-auto-field{display:grid;grid-template-columns:minmax(115px,30%) minmax(0,1fr);align-items:center;gap:7px}.equipment-radio-set{display:flex;gap:16px}.equipment-radio-set label{display:flex;align-items:center;gap:4px}.equipment-radio-set input,.equipment-check input{width:auto}
.equipment-message-settings{display:grid;grid-template-columns:minmax(0,1fr);gap:7px;margin-top:2px;padding:9px;border:1px solid #bbb}
.equipment-message-settings>label:not(.equipment-check){display:grid;grid-template-columns:minmax(110px,32%) minmax(0,1fr);align-items:center;gap:7px}
.equipment-message-settings input,.equipment-message-settings select{height:30px;border:1px solid #888;background:#fff;padding:4px;font:inherit}
.equipment-message-settings .equipment-check{display:flex;align-items:center;gap:7px;margin-left:32%}
.equipment-form-audit{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px;padding:9px;border:1px solid #c7c7c7;color:#777}
.equipment-win-form>footer,.equipment-chamber-form>footer{display:flex;justify-content:flex-end;gap:9px;padding-top:2px}
.equipment-win-form>footer button,.equipment-chamber-form>footer button{min-width:100px;height:38px;border:1px solid #ccc;background:#fafafa;font:inherit}
.equipment-win-form>footer .equipment-form-confirm,.equipment-chamber-form>footer .equipment-form-confirm{border-color:#f0a126;background:#ffa62b;color:white}
.equipment-chamber-form{width:min(600px,95vw)}.equipment-chamber-form>label{grid-template-columns:130px minmax(0,1fr)}
.equipment-query{display:flex;min-height:52px;align-items:center;gap:8px 10px;margin:0 0 6px;padding:4px 7px;border:1px solid #dedede;background:#fff;white-space:nowrap}
.equipment-state-filter{display:flex;align-items:center;gap:10px 16px;margin:0;padding:0 4px;border:0}
.equipment-state-filter legend{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}
.equipment-state-filter label{display:inline-flex;align-items:center;gap:4px}
.equipment-state-filter input{width:14px;height:14px;margin:0;accent-color:#444}
.equipment-query-label{font-size:12px}
.equipment-query select,.equipment-keyword{height:29px;border:1px solid #888;border-radius:0;background:#fff;color:#333;font:inherit}
.equipment-query select{width:205px;padding:3px 7px}
.equipment-keyword{width:min(210px,24vw);padding:4px 7px}
.equipment-like{font-size:12px}
.equipment-query-button{min-width:82px;height:29px;padding:4px 16px;border:1px solid #aaa;border-radius:3px;background:linear-gradient(#fff,#eee);color:#333;font:inherit}
.equipment-list-panel{display:grid;grid-template-columns:minmax(0,1fr) 116px;grid-template-rows:auto minmax(260px,1fr) auto;min-width:0;max-height:62vh;overflow:hidden;padding:5px;border:1px solid #c7c7c7;background:#fff}
.equipment-panel-heading{grid-column:1/-1;display:flex;align-items:center;gap:10px;min-height:26px;padding:2px 4px 5px}
.equipment-panel-heading h2{margin:0;color:#333;font-size:13px;font-weight:500}
.equipment-panel-heading span{color:#777;font-size:11px}
.equipment-table-scroll,.equipment-chamber-scroll{min-width:0;overflow:auto;border:1px solid #c7c7c7}
.equipment-table{width:100%;border-collapse:collapse;font-size:11px;white-space:nowrap}
.equipment-table th{position:sticky;top:0;z-index:1;height:31px;padding:5px 8px;border:1px solid #c7b852;background:#e7c600;color:#26251f;font-weight:500;text-align:left}
.equipment-table th:first-child{background:#e7c600;color:#26251f}
.equipment-table td{height:33px;padding:5px 9px;border:1px solid #d0d0d0;line-height:1.25;text-align:left}
.equipment-table tbody tr{cursor:pointer}
.equipment-table tbody tr:hover{background:#f3f8fd}
.equipment-table tbody tr.selected-row{background:#e7f3ff;box-shadow:inset 3px 0 #278bdc}
.equipment-table tbody tr:focus-visible{outline:2px solid #278bdc;outline-offset:-2px}
.equipment-code{color:#273b50;font:12px 'Segoe UI',sans-serif}
.equipment-actions{display:flex;grid-column:2;grid-row:2;flex-direction:column;gap:5px;min-width:0;max-height:60vh;margin:0;padding:4px 0 4px 8px;overflow-y:auto;border-left:1px solid #c7c7c7;background:#fff}
.equipment-action{display:flex;min-height:34px;align-items:center;justify-content:center;padding:4px 6px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;font:inherit;text-align:center;text-decoration:none;cursor:pointer}
.equipment-action:hover:not(:disabled){background:#f2f2f2}
.equipment-action.primary{border-color:#f2a532;background:#ffb33b;color:#fff;font-weight:600}
.equipment-action.danger{color:#7e2929}
.equipment-action:disabled{color:#aaa;cursor:not-allowed}
.equipment-list-footer{grid-column:1/-1;min-height:24px;padding:5px 3px 0;color:#777;font-size:11px}
.equipment-list-footer strong{margin-left:8px;color:#555;font-weight:500}
.equipment-message{margin:5px 0;padding:7px 10px;border:1px solid;font-size:12px}
.equipment-message.error{border-color:#d5a3a3;background:#fff0f0;color:#7a2424}
.equipment-message.success{border-color:#9bc3a3;background:#eef8ef;color:#275d33}
.equipment-chamber-panel{margin-top:8px;border:1px solid #bcbcbc;background:#fff}
.equipment-chamber-panel>summary{padding:8px 10px;background:#f1f1f1;color:#444;font-weight:600;cursor:pointer}
.equipment-chamber-panel[open]>summary{border-bottom:1px solid #ccc}
.equipment-detail-panel{margin:0;padding:7px;border:0}
.equipment-detail-header{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:3px 4px 8px}
.equipment-detail-header h2{margin:0;color:#30363b;font-size:14px;font-weight:600}
.equipment-detail-header p{margin:3px 0 0;color:#777;font-size:11px}
.equipment-small-action,.equipment-chamber-actions a,.equipment-chamber-actions button{padding:4px 9px;border:1px solid #c4c4c4;border-radius:3px;background:linear-gradient(#fff,#f5f5f5);color:#333;font:inherit;text-decoration:none;white-space:nowrap}
.equipment-detail-note{padding:10px;color:#777}
.equipment-chamber-table th:first-child{background:#e7c600;color:#26251f}
.equipment-chamber-actions{display:flex;gap:5px}
.equipment-chamber-actions button{cursor:pointer}
@media(max-width:900px){.equipment-query{flex-wrap:wrap}.equipment-list-panel{grid-template-columns:minmax(0,1fr) 96px}.equipment-actions{padding-left:5px}.equipment-table{min-width:1050px}}
@media(max-width:580px){.equipment-master-page{padding:7px}.equipment-list-panel{grid-template-columns:minmax(0,1fr) 82px}.equipment-action{min-height:32px;padding:4px;font-size:10px}.equipment-table{min-width:900px}.equipment-state-filter{flex-wrap:wrap;gap:7px 10px}}
</style>
