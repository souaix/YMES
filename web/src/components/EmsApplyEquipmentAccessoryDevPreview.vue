<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()

interface EquipmentOption { equipmentNo: string; equipmentType: string; name: string }
interface AccessoryItem {
  accessoryNo: string
  accessoryType: string
  accessoryCategory: string
  version: string
  issueState: number
  currentState: string
  stateName: string
  residualLife: string
  compatibleEquipmentTypes: string[]
  violatesApply: boolean
  violatesReturn: boolean
  mountedEquipmentNo: string
  lifeAlert: string
  checkFlag: boolean
  currentUser: string
  stateStartedAt: string
}
interface AccessoryLog { accessoryNo: string; equipmentNo: string; operation: '上機' | '預先下機'; operatedAt: string; userNo: string; note: string }
interface PendingUnbind { accessoryNo: string; equipmentNo: string }

const equipmentTypes = [
  { equipmentType: 'DEV-TESTER', description: '測試機類別' },
  { equipmentType: 'DEV-FT', description: '成品測試設備' },
]
const equipmentOptions: EquipmentOption[] = [
  { equipmentNo: 'DEV-EQP-101', equipmentType: 'DEV-TESTER', name: '測試機 101' },
  { equipmentNo: 'DEV-EQP-102', equipmentType: 'DEV-TESTER', name: '測試機 102' },
  { equipmentNo: 'DEV-EQP-201', equipmentType: 'DEV-FT', name: '成品測試機 201' },
]
const initialAccessories: AccessoryItem[] = [
  { accessoryNo: 'DEV-ACC-CHUCK-01', accessoryType: 'CHUCK', accessoryCategory: '測試治具', version: '01', issueState: 2, currentState: 'READY', stateName: '可用', residualLife: '2,500 次', compatibleEquipmentTypes: ['DEV-TESTER'], violatesApply: false, violatesReturn: false, mountedEquipmentNo: '', lifeAlert: '', checkFlag: false, currentUser: 'DEV-OP-01', stateStartedAt: '2026-11-23 08:00:00' },
  { accessoryNo: 'DEV-ACC-CHUCK-02', accessoryType: 'CHUCK', accessoryCategory: '測試治具', version: '01', issueState: 2, currentState: 'WAIT', stateName: '待領用', residualLife: '480 次', compatibleEquipmentTypes: ['DEV-TESTER'], violatesApply: false, violatesReturn: false, mountedEquipmentNo: '', lifeAlert: '剩餘壽命低於 DEV 提醒門檻。', checkFlag: false, currentUser: 'DEV-OP-02', stateStartedAt: '2026-11-22 16:10:00' },
  { accessoryNo: 'DEV-ACC-CHUCK-03', accessoryType: 'CHUCK', accessoryCategory: '測試治具', version: '02', issueState: 2, currentState: 'CLEAN', stateName: '清潔完成', residualLife: '7,200 次', compatibleEquipmentTypes: ['DEV-TESTER'], violatesApply: false, violatesReturn: false, mountedEquipmentNo: 'DEV-EQP-102', lifeAlert: '', checkFlag: false, currentUser: 'DEV-OP-03', stateStartedAt: '2026-11-22 10:30:00' },
  { accessoryNo: 'DEV-ACC-SOCKET-01', accessoryType: 'SOCKET', accessoryCategory: '測試治具', version: '01', issueState: 2, currentState: 'REPAIR', stateName: '待檢修', residualLife: '—', compatibleEquipmentTypes: ['DEV-TESTER'], violatesApply: true, violatesReturn: false, mountedEquipmentNo: '', lifeAlert: '', checkFlag: false, currentUser: 'DEV-OP-04', stateStartedAt: '2026-11-21 09:15:00' },
  { accessoryNo: 'DEV-ACC-LOAD-01', accessoryType: 'LOAD BOARD', accessoryCategory: '負載板', version: '03', issueState: 2, currentState: 'READY', stateName: '可用', residualLife: '1,300 次', compatibleEquipmentTypes: ['DEV-FT'], violatesApply: false, violatesReturn: false, mountedEquipmentNo: '', lifeAlert: '', checkFlag: false, currentUser: 'DEV-OP-05', stateStartedAt: '2026-11-20 12:00:00' },
  { accessoryNo: 'DEV-ACC-LOAD-02', accessoryType: 'LOAD BOARD', accessoryCategory: '負載板', version: '02', issueState: 1, currentState: 'READY', stateName: '審核中', residualLife: '6,100 次', compatibleEquipmentTypes: ['DEV-FT'], violatesApply: false, violatesReturn: false, mountedEquipmentNo: '', lifeAlert: '', checkFlag: false, currentUser: 'DEV-OP-06', stateStartedAt: '2026-11-20 11:20:00' },
]

const equipmentType = ref('')
const equipmentNo = ref('')
const accessoryNoInput = ref('')
const availableOnly = ref(true)
const accessories = ref<AccessoryItem[]>(initialAccessories.map((row) => ({ ...row })))
const actionMessage = ref('')
const messageKind = ref<'info' | 'success' | 'warning' | 'error'>('info')
const showLookup = ref<'type' | 'equipment' | null>(null)
const lookupText = ref('')
const pendingUnbinds = ref<PendingUnbind[]>([])
const skippedAccessoryNos = ref<string[]>([])
const selectedForBatch = ref<string[]>([])
const confirmDisabled = ref(false)
const operationLogs = ref<AccessoryLog[]>([])
const creator = computed(() => props.currentUserNo?.trim() || 'DEV-USER')
const targetEquipment = computed(() => equipmentOptions.find((row) => row.equipmentNo === equipmentNo.value) ?? null)
const selectedRows = computed(() => accessories.value.filter((row) => row.checkFlag))
const pendingUnbind = computed(() => pendingUnbinds.value[0] ?? null)
const pendingAccessory = computed(() => pendingUnbind.value ? accessories.value.find((row) => row.accessoryNo === pendingUnbind.value?.accessoryNo) ?? null : null)
const filteredEquipments = computed(() => equipmentOptions
  .filter((row) => !equipmentType.value || row.equipmentType === equipmentType.value)
  .filter((row) => !lookupText.value || `${row.equipmentNo} ${row.name}`.toLowerCase().includes(lookupText.value.trim().toLowerCase())))
const filteredAccessories = computed(() => {
  if (!equipmentType.value || !equipmentNo.value) return []
  const applyState = 'APPLY'
  return accessories.value
    .filter((row) => row.issueState === 2 && row.currentState !== applyState)
    .filter((row) => row.compatibleEquipmentTypes.includes(equipmentType.value))
    .filter((row) => !availableOnly.value || !row.violatesApply)
    .sort((a, b) => a.accessoryNo.localeCompare(b.accessoryNo))
})
const preselectedAccessoryNo = (() => {
  if (typeof window === 'undefined') return ''
  const params = new URLSearchParams(window.location.search)
  return params.get('AccessoryNo') || params.get('accessoryNo') || ''
})()

if (preselectedAccessoryNo) {
  accessoryNoInput.value = preselectedAccessoryNo
  actionMessage.value = '已帶入監控畫面的配件編號；選擇相容設備後可在清單中定位。'
}

function chooseType(value: string): void {
  equipmentType.value = value
  equipmentNo.value = ''
  accessories.value.forEach((row) => { row.checkFlag = false })
  showLookup.value = null
  actionMessage.value = ''
}

function openLookup(kind: 'type' | 'equipment'): void {
  showLookup.value = kind
  lookupText.value = ''
}

function chooseEquipment(value: string): void {
  const option = equipmentOptions.find((row) => row.equipmentNo === value)
  if (!option) return
  equipmentType.value = option.equipmentType
  equipmentNo.value = option.equipmentNo
  accessories.value.forEach((row) => { row.checkFlag = false })
  showLookup.value = null
  if (preselectedAccessoryNo) findAccessory()
}

function findAccessory(): void {
  const searchValue = accessoryNoInput.value.trim().toUpperCase()
  if (!searchValue) {
    actionMessage.value = '請輸入 Accessory No。'
    messageKind.value = 'error'
    return
  }
  const row = filteredAccessories.value.find((item) => item.accessoryNo.toUpperCase() === searchValue)
  if (!row) {
    actionMessage.value = '查無符合設備類別與 Available Only 條件的可選配件。'
    messageKind.value = 'error'
    return
  }
  row.checkFlag = true
  actionMessage.value = `已選取 ${row.accessoryNo}。`
  messageKind.value = row.lifeAlert ? 'warning' : 'info'
}

function toggleRow(accessory: AccessoryItem): void {
  accessory.checkFlag = !accessory.checkFlag
}

function selectVisibleAccessories(event: Event): void {
  const checked = (event.target as HTMLInputElement).checked
  filteredAccessories.value.forEach((row) => { row.checkFlag = checked })
}

function setAvailableOnly(event: Event): void {
  availableOnly.value = (event.target as HTMLInputElement).checked
  accessories.value.forEach((row) => { row.checkFlag = false })
}

function startApply(): void {
  actionMessage.value = ''
  if (!equipmentType.value) {
    actionMessage.value = '請選擇 Equipment Type。'
    messageKind.value = 'error'
    return
  }
  if (!targetEquipment.value || targetEquipment.value.equipmentType !== equipmentType.value) {
    actionMessage.value = '請選擇與 Equipment Type 相符的 Equipment No。'
    messageKind.value = 'error'
    return
  }
  if (!selectedRows.value.length) {
    actionMessage.value = '請至少勾選一筆可上機配件。'
    messageKind.value = 'error'
    return
  }
  skippedAccessoryNos.value = []
  selectedForBatch.value = selectedRows.value.map((row) => row.accessoryNo)
  pendingUnbinds.value = selectedRows.value
    .filter((row) => row.mountedEquipmentNo && row.mountedEquipmentNo !== equipmentNo.value)
    .map((row) => ({ accessoryNo: row.accessoryNo, equipmentNo: row.mountedEquipmentNo }))
  messageKind.value = 'info'
  if (pendingUnbinds.value.length) return
  executeApplyBatch()
}

function resolveExistingMount(shouldReturn: boolean): void {
  const pending = pendingUnbind.value
  const row = pendingAccessory.value
  if (!pending || !row) {
    pendingUnbinds.value.shift()
    if (!pendingUnbinds.value.length) executeApplyBatch()
    return
  }
  if (shouldReturn) {
    if (row.violatesReturn) {
      skippedAccessoryNos.value.push(row.accessoryNo)
      actionMessage.value = `${row.accessoryNo} 的 DEV Return 互斥規則不允許下機，已略過此配件。`
      messageKind.value = 'error'
    } else {
      const returnedAt = new Date().toLocaleString('zh-TW', { hour12: false })
      operationLogs.value.unshift({ accessoryNo: row.accessoryNo, equipmentNo: pending.equipmentNo, operation: '預先下機', operatedAt: returnedAt, userNo: creator.value, note: '使用者選擇先解除既有設備關聯，再執行上機。' })
      row.mountedEquipmentNo = ''
      row.currentState = 'RETURN'
      row.stateName = '已下機'
      row.currentUser = creator.value
      row.stateStartedAt = returnedAt
      actionMessage.value = `${row.accessoryNo} 已在 DEV 假資料先模擬下機。`
      messageKind.value = 'info'
    }
  } else {
    skippedAccessoryNos.value.push(row.accessoryNo)
    actionMessage.value = `已略過 ${row.accessoryNo}；其餘選取配件繼續檢查。`
    messageKind.value = 'warning'
  }
  pendingUnbinds.value.shift()
  if (!pendingUnbinds.value.length) executeApplyBatch()
}

function executeApplyBatch(): void {
  const targets = selectedForBatch.value
    .filter((accessoryNo) => !skippedAccessoryNos.value.includes(accessoryNo))
    .map((accessoryNo) => accessories.value.find((row) => row.accessoryNo === accessoryNo))
    .filter((row): row is AccessoryItem => Boolean(row))
  if (!targets.length) {
    if (!actionMessage.value) actionMessage.value = '沒有可送出的配件。'
    messageKind.value = 'warning'
    return
  }
  const blocked = targets.filter((row) => row.violatesApply)
  if (blocked.length) {
    actionMessage.value = `Apply 狀態互斥阻擋：${blocked.map((row) => `${row.accessoryNo}（${row.stateName}）`).join('、')}。本批上機沒有套用；先前已完成的預先下機仍保留在 DEV 假資料。`
    messageKind.value = 'error'
    return
  }
  const now = new Date().toLocaleString('zh-TW', { hour12: false })
  for (const row of targets) {
    row.mountedEquipmentNo = equipmentNo.value
    row.currentState = 'APPLY'
    row.stateName = '已上機'
    row.currentUser = creator.value
    row.stateStartedAt = now
    row.checkFlag = false
    operationLogs.value.unshift({ accessoryNo: row.accessoryNo, equipmentNo: equipmentNo.value, operation: '上機', operatedAt: now, userNo: creator.value, note: 'DEV 模擬新增設備配件關聯及 Apply 狀態。' })
  }
  const alerts = targets.filter((row) => row.lifeAlert).map((row) => `${row.accessoryNo}：${row.lifeAlert}`)
  actionMessage.value = `${targets.length} 件配件已模擬上機至 ${equipmentNo.value}。${alerts.length ? ` 壽命提醒：${alerts.join('；')}` : ''} 假資料只存在本頁。`
  messageKind.value = alerts.length ? 'warning' : 'success'
  confirmDisabled.value = true
}
</script>

<template>
  <section class="apply-page" aria-label="配件上機 DEV 預覽" :data-development-preview="developmentPreview ? 'true' : 'false'">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES301 · DEV fixtures</div><h1>配件上機 <span>APPLY EQP ACCESSORY</span></h1></div>
      <button type="button" class="close-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>設備、配件狀態與操作紀錄只保存在此頁面記憶體。</span></div>

    <section class="selection-card" aria-label="設備與配件上機條件">
      <div class="card-title">Apply Equipment Accessory</div>
      <div class="form-grid">
        <label for="apply-type">Equipment Type</label>
        <div class="lookup-field"><input id="apply-type" :value="equipmentType" readonly placeholder="選擇設備類別" /><button type="button" aria-label="查找設備類別" @click="openLookup('type')">…</button></div>
        <label for="apply-eqp">Equipment No</label>
        <div class="lookup-field"><input id="apply-eqp" :value="targetEquipment ? `${targetEquipment.equipmentNo} · ${targetEquipment.name}` : ''" readonly placeholder="選擇目標設備" /><button type="button" aria-label="查找目標設備" @click="openLookup('equipment')">…</button></div>
        <label for="apply-acc">Accessory No</label>
        <div class="lookup-field"><input id="apply-acc" v-model="accessoryNoInput" maxlength="50" placeholder="輸入或掃描配件編號" @keyup.enter="findAccessory" /><button type="button" @click="findAccessory">Find</button></div>
        <label class="available-only"><input :checked="availableOnly" type="checkbox" @change="setAvailableOnly" />Available Only</label>
        <div class="selected-count"><strong>{{ selectedRows.length }}</strong><span>件已選取</span></div>
      </div>
      <div class="form-actions"><button type="button" class="primary-button" :disabled="confirmDisabled" @click="startApply">Confirm Apply</button><button type="button" class="secondary-button" @click="emit('close')">Close</button></div>
      <p v-if="actionMessage" class="feedback" :class="`feedback-${messageKind}`" role="status">{{ actionMessage }}</p>
    </section>

    <section class="accessory-card" aria-label="可上機配件">
      <div class="card-heading"><div><h2>可上機配件</h2><span>設備相容性、IssueState=2 與 APPLY 互斥條件已套用</span></div><b>{{ filteredAccessories.length }} 件</b></div>
      <div class="table-scroll"><table>
        <thead><tr><th class="check-cell"><input type="checkbox" aria-label="全選目前結果" :checked="filteredAccessories.length > 0 && filteredAccessories.every((row) => row.checkFlag)" :disabled="confirmDisabled" @change="selectVisibleAccessories" /></th><th>Accessory No</th><th>Accessory Type</th><th>Accessory Category</th><th>Residual Life</th><th>State</th></tr></thead>
        <tbody>
          <tr v-for="row in filteredAccessories" :key="row.accessoryNo" :class="{ selected: row.checkFlag }" @click="!confirmDisabled && toggleRow(row)">
            <td class="check-cell"><input v-model="row.checkFlag" type="checkbox" :aria-label="`選取 ${row.accessoryNo}`" :disabled="confirmDisabled" @click.stop /></td><td class="code-cell">{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.accessoryCategory }}</td><td :class="{ 'life-warning': row.lifeAlert }">{{ row.residualLife }}</td><td><span class="state-chip" :class="row.violatesApply ? 'blocked' : 'ready'">{{ row.stateName }}</span></td>
          </tr>
          <tr v-if="!equipmentType || !equipmentNo"><td colspan="6" class="empty-row">請先選擇 Equipment Type 與 Equipment No。</td></tr>
          <tr v-else-if="!filteredAccessories.length"><td colspan="6" class="empty-row">沒有符合此設備類別及 Available Only 條件的配件。</td></tr>
        </tbody>
      </table></div>
      <div v-if="selectedRows.some((row) => row.mountedEquipmentNo)" class="existing-mount-note"><strong>送出前檢查到既有設備關聯</strong><span>按 Confirm 後，系統會逐項詢問是否先下機；此資訊來自 DEV 假資料，不會寫入正式系統。</span></div>
    </section>

    <section v-if="operationLogs.length" class="history-card" aria-label="本頁模擬上機歷程">
      <div class="card-heading"><div><h2>本頁模擬操作紀錄</h2><span>Not persisted</span></div><b>{{ operationLogs.length }} 筆</b></div>
      <div class="table-scroll"><table><thead><tr><th>操作</th><th>配件／版本</th><th>設備</th><th>時間</th><th>操作人</th><th>說明</th></tr></thead>
        <tbody><tr v-for="(row, index) in operationLogs" :key="`${row.accessoryNo}-${row.operation}-${index}`"><td>{{ row.operation }}</td><td>{{ row.accessoryNo }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.operatedAt }}</td><td>{{ row.userNo }}</td><td>{{ row.note }}</td></tr></tbody>
      </table></div>
    </section>

    <div v-if="pendingUnbind" class="dialog-backdrop"><section class="confirm-dialog" role="dialog" aria-modal="true" aria-label="配件已關聯設備">
      <div class="dialog-eyebrow">EXISTING EQUIPMENT MAPPING</div><h2>{{ pendingUnbind.accessoryNo }} 已有設備關聯</h2>
      <p>目前記錄的設備為 <strong>{{ pendingUnbind.equipmentNo }}</strong>，目標設備為 <strong>{{ equipmentNo }}</strong>。WinForms 會詢問是否先執行下機，再繼續本批上機。</p>
      <p v-if="pendingAccessory?.violatesReturn" class="dialog-warning">DEV Return 互斥規則會阻擋先下機；此配件將略過。</p>
      <footer><button type="button" class="secondary-button" @click="resolveExistingMount(false)">略過此配件</button><button type="button" class="primary-button" :disabled="pendingAccessory?.violatesReturn" @click="resolveExistingMount(true)">先下機再上機</button></footer>
    </section></div>

    <div v-if="showLookup" class="dialog-backdrop" @click.self="showLookup = null"><section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="showLookup === 'type' ? '選取設備類別' : '選取設備'">
      <header><div><strong>{{ showLookup === 'type' ? '選取設備類別' : '選取 Equipment No' }}</strong><span>{{ showLookup === 'type' ? 'DEV 查找資料' : '僅列出非 Chamber 假設備' }}</span></div><button type="button" aria-label="關閉查找視窗" @click="showLookup = null">×</button></header>
      <input v-if="showLookup === 'equipment'" v-model="lookupText" class="lookup-search" type="search" placeholder="搜尋設備編號或名稱" />
      <div v-if="showLookup === 'type'" class="lookup-options"><button v-for="item in equipmentTypes" :key="item.equipmentType" type="button" @click="chooseType(item.equipmentType)"><strong>{{ item.equipmentType }}</strong><span>{{ item.description }}</span></button></div>
      <div v-else class="lookup-options"><button v-for="item in filteredEquipments" :key="item.equipmentNo" type="button" @click="chooseEquipment(item.equipmentNo)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }} · {{ item.name }}</span></button><p v-if="!filteredEquipments.length" class="empty-row">沒有符合條件的設備。</p></div>
      <footer><button type="button" class="secondary-button" @click="showLookup = null">Cancel</button></footer>
    </section></div>
  </section>
</template>

<style scoped>
.apply-page { min-height: 0; height: 100%; overflow: auto; padding: 16px; box-sizing: border-box; background: #f5f4ef; color: #26251f; font: inherit; }
.page-header,.card-heading,.form-actions,.lookup-dialog header,.lookup-dialog footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header { max-width: 1120px; margin: 0 auto 12px; }.eyebrow,.card-heading span,.lookup-dialog header span { color: #777366; font-size: 12px; }
h1 { margin: 3px 0 0; font-size: 22px; } h1 span { margin-left: 8px; color: #777366; font-size: 13px; font-weight: 500; }
.close-button,.lookup-dialog header button { width: 32px; height: 32px; border: 1px solid #c9c6b9; border-radius: 4px; background: white; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner,.selection-card,.accessory-card,.history-card { max-width: 1120px; margin: 0 auto 12px; border: 1px solid #d9d6ca; border-radius: 6px; background: white; }
.fixture-banner { display: flex; gap: 10px; align-items: center; padding: 9px 12px; background: #fff9df; color: #655612; font-size: 13px; }.fixture-banner strong { padding: 2px 7px; border-radius: 10px; background: #efdb87; font-size: 11px; }
.selection-card { padding: 14px; }.card-title { margin-bottom: 12px; font-size: 15px; font-weight: 700; }
.form-grid { display: grid; grid-template-columns: 145px minmax(200px, 1fr) 115px minmax(200px, 1fr); gap: 9px 12px; align-items: center; max-width: 900px; }
.form-grid > label:not(.available-only) { text-align: right; color: #565349; font-size: 13px; }.form-grid input:not([type="checkbox"]),.lookup-search { width: 100%; min-height: 32px; box-sizing: border-box; border: 1px solid #c9c6b9; border-radius: 3px; padding: 5px 8px; background: #f8f7f2; color: #292821; font: inherit; }
.lookup-field { display: flex; gap: 5px; min-width: 0; }.lookup-field input { flex: 1; min-width: 0; }.lookup-field button { min-width: 34px; border: 1px solid #aaa696; border-radius: 3px; background: #f4f2e9; font: inherit; cursor: pointer; }
.available-only { display: flex; justify-content: flex-end; gap: 7px; align-items: center; color: #565349; font-size: 13px; }.available-only input { width: 16px; height: 16px; }.selected-count { display: flex; align-items: baseline; gap: 7px; color: #777366; font-size: 12px; }.selected-count strong { color: #343228; font-size: 18px; }
.form-actions { justify-content: flex-end; margin-top: 14px; }.primary-button,.secondary-button { min-height: 31px; padding: 5px 13px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; cursor: pointer; }.primary-button { background: #3b3930; color: white; }.primary-button:disabled { opacity: .45; cursor: not-allowed; }.secondary-button { background: white; color: #333; }
.feedback { margin: 10px 0 0; padding: 8px 10px; border-radius: 3px; font-size: 13px; }.feedback-info { background: #edf3f6; color: #315163; }.feedback-warning { background: #fff3d6; color: #765a12; }.feedback-error { background: #fde9e7; color: #8c2721; }.feedback-success { background: #e6f2e9; color: #27633a; }
.accessory-card,.history-card { padding: 13px 14px; }.card-heading { margin-bottom: 9px; }.card-heading h2 { display: inline; margin: 0 8px 0 0; font-size: 15px; }.card-heading b { color: #625f55; font-size: 12px; font-weight: 500; }
.table-scroll { overflow: auto; border: 1px solid #e2dfd5; border-radius: 3px; }.table-scroll table { width: 100%; border-collapse: collapse; font-size: 12px; }.table-scroll th,.table-scroll td { padding: 8px 9px; border-bottom: 1px solid #e7e4da; text-align: left; white-space: nowrap; }.table-scroll th { position: sticky; top: 0; background: #f2f0e8; color: #555246; font-weight: 650; }.table-scroll tbody tr { cursor: pointer; }.table-scroll tbody tr:hover,.table-scroll tbody tr.selected { background: #f8f3dd; }.check-cell { width: 34px; text-align: center !important; }.code-cell { font-family: Consolas, monospace; font-weight: 650; }.empty-row { padding: 24px !important; color: #777366; text-align: center !important; white-space: normal !important; }.life-warning { color: #885d08; font-weight: 650; }.state-chip { padding: 3px 7px; border-radius: 10px; background: #e7eee2; color: #40583d; font-size: 11px; }.state-chip.blocked { background: #f5e2de; color: #8a4338; }
.existing-mount-note { display: grid; gap: 4px; margin-top: 9px; padding: 9px 11px; border: 1px solid #e3d59b; border-radius: 3px; background: #fff9e4; color: #67551b; font-size: 12px; }.existing-mount-note span { color: #756c4a; }
.dialog-backdrop { position: fixed; inset: 0; z-index: 1300; display: grid; place-items: center; padding: 16px; background: #17170fb0; }.confirm-dialog,.lookup-dialog { width: min(540px, 100%); max-height: 90vh; overflow: auto; box-sizing: border-box; border: 1px solid #aaa596; border-radius: 5px; background: white; box-shadow: 0 16px 48px #0004; }.confirm-dialog { padding: 17px; }.dialog-eyebrow { color: #777366; font-size: 10px; letter-spacing: .08em; }.confirm-dialog h2 { margin: 5px 0 9px; font-size: 18px; }.confirm-dialog p { color: #5d5a50; font-size: 13px; line-height: 1.55; }.confirm-dialog footer,.lookup-dialog footer { display: flex; justify-content: flex-end; gap: 7px; margin-top: 14px; padding-top: 10px; border-top: 1px solid #e4e1d8; }.dialog-warning { padding: 8px; background: #fff0ed; color: #8c2721 !important; }
.lookup-dialog { padding: 12px; }.lookup-dialog header { margin-bottom: 9px; }.lookup-dialog header div { display: grid; gap: 4px; }.lookup-options { display: grid; gap: 5px; max-height: 360px; overflow: auto; }.lookup-options button { display: flex; justify-content: space-between; gap: 12px; padding: 9px 10px; border: 1px solid #e0ddd3; border-radius: 3px; background: #fbfaf6; text-align: left; font: inherit; cursor: pointer; }.lookup-options button:hover { background: #f4f0dc; }.lookup-options button span { color: #777366; font-size: 12px; }.lookup-search { margin-bottom: 8px; }
@media (max-width: 760px) { .form-grid { grid-template-columns: 1fr; }.form-grid > label:not(.available-only) { text-align: left; }.available-only { justify-content: flex-start; }.page-header { align-items: flex-start; } }
</style>
