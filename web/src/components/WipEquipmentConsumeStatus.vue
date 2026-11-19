<script setup lang="ts">
import { computed, reactive, ref } from 'vue'

type Mode = 'add' | 'edit' | 'copy'
type Equipment = { equipmentNo: string; equipmentType: string; description: string; issueState: number }
type ConsumeBasis = { equipmentType: string; consumeNo: string; quantity: number; toleranceQty: number; period: number; tolerancePeriod: number }
type ConsumeStatus = {
  equipmentNo: string; consumeNo: string; consumeSeq: string; curQty: number; curPeriod: number
  creator: string; createDate: string; revisor: string; reviseDate: string
}
type ConsumeLog = {
  equipmentNo: string; consumeNo: string; consumeSeq: string; creator: string; createDate: string
  accQty: number; accPeriod: number
}
type Snapshot = { version: 1; statuses: ConsumeStatus[]; logs: ConsumeLog[] }

const props = defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const storageKey = 'ymes.dev.wp303-consume-status.v1'
const productionUnavailable = '設備耗品現況服務尚未提供，暫無法查詢或操作。'
const userNo = computed(() => props.currentUserNo || (dev.value ? 'DEVUSER' : ''))

const equipment: Equipment[] = [
  { equipmentNo: 'DEV-EQP-ETCH-01', equipmentType: 'DEV-ETCH', description: 'DEV 蝕刻設備 01', issueState: 2 },
  { equipmentNo: 'DEV-EQP-ETCH-02', equipmentType: 'DEV-ETCH', description: 'DEV 蝕刻設備 02', issueState: 2 },
  { equipmentNo: 'DEV-EQP-CMP-01', equipmentType: 'DEV-CMP', description: 'DEV CMP 設備 01', issueState: 2 },
  { equipmentNo: 'DEV-EQP-RETIRED-01', equipmentType: 'DEV-ETCH', description: 'DEV 停用設備範例', issueState: 1 },
]
const consumeBases: ConsumeBasis[] = [
  { equipmentType: 'DEV-ETCH', consumeNo: 'DEV-ETCH-KIT', quantity: 500, toleranceQty: 25, period: 720, tolerancePeriod: 24 },
  { equipmentType: 'DEV-ETCH', consumeNo: 'DEV-ETCH-FILTER', quantity: 300, toleranceQty: 15, period: -1, tolerancePeriod: 0 },
  { equipmentType: 'DEV-CMP', consumeNo: 'DEV-CMP-PAD', quantity: 1000, toleranceQty: 50, period: 360, tolerancePeriod: 12 },
]

function seedSnapshot(): Snapshot {
  return {
    version: 1,
    statuses: [
      { equipmentNo: 'DEV-EQP-ETCH-01', consumeNo: 'DEV-ETCH-KIT', consumeSeq: 'DEV-KIT-2026-001', curQty: 132, curPeriod: 184.5, creator: 'DEVUSER', createDate: '2026/11/18 08:30:00', revisor: '', reviseDate: '' },
      { equipmentNo: 'DEV-EQP-ETCH-01', consumeNo: 'DEV-ETCH-FILTER', consumeSeq: 'DEV-FILTER-2026-004', curQty: 66, curPeriod: 92, creator: 'DEVUSER', createDate: '2026/11/17 10:15:00', revisor: 'DEVUSER', reviseDate: '2026/11/19 14:20:00' },
      { equipmentNo: 'DEV-EQP-ETCH-02', consumeNo: 'DEV-ETCH-KIT', consumeSeq: 'DEV-KIT-2026-002', curQty: 298, curPeriod: 355.25, creator: 'DEVUSER', createDate: '2026/11/16 09:05:00', revisor: '', reviseDate: '' },
      { equipmentNo: 'DEV-EQP-CMP-01', consumeNo: 'DEV-CMP-PAD', consumeSeq: 'DEV-PAD-2026-002', curQty: 410, curPeriod: 88, creator: 'DEVUSER', createDate: '2026/11/14 13:40:00', revisor: 'DEVUSER', reviseDate: '2026/11/19 16:02:00' },
    ],
    logs: [
      { equipmentNo: 'DEV-EQP-ETCH-01', consumeNo: 'DEV-ETCH-KIT', consumeSeq: 'DEV-KIT-2026-000', creator: 'DEVUSER', createDate: '2026/11/01 08:15:00', accQty: 486, accPeriod: 702 },
      { equipmentNo: 'DEV-EQP-CMP-01', consumeNo: 'DEV-CMP-PAD', consumeSeq: 'DEV-PAD-2026-001', creator: 'DEVUSER', createDate: '2026/10/29 17:00:00', accQty: 975, accPeriod: 342.5 },
    ],
  }
}
function emptySnapshot(): Snapshot {
  return { version: 1, statuses: [], logs: [] }
}
function readSnapshot(): Snapshot {
  if (!dev.value) return emptySnapshot()
  try {
    const value = window.localStorage.getItem(storageKey)
    if (value) {
      const parsed = JSON.parse(value) as Snapshot
      if (parsed.version === 1 && Array.isArray(parsed.statuses) && Array.isArray(parsed.logs)) return parsed
    }
  } catch { /* keep deterministic DEV fixtures */ }
  return seedSnapshot()
}

const snapshot = ref<Snapshot>(readSnapshot())
const selectedKeys = ref<string[]>([])
const activeKey = ref('')
const formOpen = ref(false)
const formMode = ref<Mode>('add')
const equipmentPickerOpen = ref(false)
const deleteConfirmOpen = ref(false)
const resetOpen = ref(false)
const resetAt = ref('')
const errorText = ref('')
const successText = ref('')
const formError = ref('')
const resetError = ref('')
const equipmentFilter = ref('')

const blankForm = () => ({ equipmentNo: '', consumeNo: '', consumeSeq: '', curQty: '0', curPeriod: '0', creator: userNo.value, createDate: nowText() })
const form = reactive(blankForm())
const formBaseline = ref('')
const resetForm = reactive({ consumeSeq: '', curQty: 0, curPeriod: 0 })
const resetSource = ref<ConsumeStatus | null>(null)
const resetWasDepleted = ref(false)

function nowText(): string {
  return new Date().toLocaleString('zh-TW', { hour12: false }).replaceAll('-', '/')
}
function keyOf(row: Pick<ConsumeStatus, 'equipmentNo' | 'consumeNo'>): string { return row.equipmentNo + '\u0000' + row.consumeNo }
function persist(): void { if (dev.value) window.localStorage.setItem(storageKey, JSON.stringify(snapshot.value)) }
function feedback(error = '', success = ''): void { errorText.value = error; successText.value = success }
function resetDev(): void {
  if (!dev.value) return
  snapshot.value = seedSnapshot()
  selectedKeys.value = []
  activeKey.value = ''
  persist()
  feedback('', '已還原 WP303 DEV 假資料。')
}

const sortedStatuses = computed(() => [...snapshot.value.statuses].sort((a, b) =>
  a.equipmentNo.localeCompare(b.equipmentNo) || a.consumeNo.localeCompare(b.consumeNo)))
const activeRow = computed(() => sortedStatuses.value.find(row => keyOf(row) === activeKey.value) ?? null)
const selectedRows = computed(() => snapshot.value.statuses.filter(row => selectedKeys.value.includes(keyOf(row))))
const filteredEquipment = computed(() => (dev.value ? equipment : [])
  .filter(item => item.issueState === 2)
  .filter(item => !equipmentFilter.value || (item.equipmentNo + ' ' + item.equipmentType + ' ' + item.description).toLowerCase().includes(equipmentFilter.value.toLowerCase()))
  .sort((a, b) => a.equipmentType.localeCompare(b.equipmentType) || a.equipmentNo.localeCompare(b.equipmentNo)))
const formEquipment = computed(() => equipment.find(item => item.equipmentNo === form.equipmentNo) ?? null)
const formConsumeOptions = computed(() => dev.value ? consumeBases.filter(item => item.equipmentType === formEquipment.value?.equipmentType) : [])
const resetBasis = computed(() => {
  const current = resetSource.value
  const equipmentType = equipment.find(item => item.equipmentNo === current?.equipmentNo)?.equipmentType
  return consumeBases.find(item => item.equipmentType === equipmentType && item.consumeNo === current?.consumeNo) ?? null
})
const remainingQty = computed(() => {
  if (!resetBasis.value || !resetSource.value) return null
  const difference = resetBasis.value.quantity - resetSource.value.curQty
  return difference > 0 ? difference : resetBasis.value.quantity
})
const remainingPeriod = computed(() => {
  if (!resetBasis.value || !resetSource.value) return null
  if (resetBasis.value.period === -1) return -1
  const difference = resetBasis.value.period - resetSource.value.curPeriod
  // WinForms fallback uses ToleranceQty here (not TolerancePeriod).
  return difference > 0 ? difference : resetBasis.value.toleranceQty
})

function selectRow(event: MouseEvent, row: ConsumeStatus): void {
  if (!dev.value) return
  const key = keyOf(row)
  activeKey.value = key
  if (event.ctrlKey || event.metaKey) {
    selectedKeys.value = selectedKeys.value.includes(key) ? selectedKeys.value.filter(item => item !== key) : [...selectedKeys.value, key]
  } else {
    selectedKeys.value = [key]
  }
  feedback()
}
function openForm(mode: Mode, row?: ConsumeStatus): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  formMode.value = mode
  formError.value = ''
  Object.assign(form, blankForm())
  if (row && (mode === 'edit' || mode === 'copy')) {
    Object.assign(form, { equipmentNo: row.equipmentNo, consumeNo: row.consumeNo, consumeSeq: row.consumeSeq, curQty: String(row.curQty), curPeriod: String(row.curPeriod), creator: mode === 'edit' ? row.creator : userNo.value, createDate: mode === 'edit' ? row.createDate : nowText() })
  }
  formBaseline.value = JSON.stringify(form)
  formOpen.value = true
}
function openEdit(): void { if (activeRow.value) openForm('edit', activeRow.value) }
function openCopy(): void { if (activeRow.value) openForm('copy', activeRow.value) }
function openEquipmentPicker(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  equipmentPickerOpen.value = true
}
function chooseEquipment(item: Equipment): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  form.equipmentNo = item.equipmentNo
  if (!formConsumeOptions.value.some(option => option.consumeNo === form.consumeNo)) form.consumeNo = ''
  equipmentPickerOpen.value = false
  equipmentFilter.value = ''
}
function saveForm(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  formError.value = ''
  if (!form.equipmentNo) { formError.value = 'EquipmentNo 不可空白。'; return }
  if (!form.consumeNo) { formError.value = 'ConsumeNo 不可空白。'; return }
  if (!form.consumeSeq.trim()) { formError.value = 'ConsumeSeq 不可空白。'; return }
  const qty = Number(form.curQty)
  const period = Number(form.curPeriod)
  if (!form.curQty.trim() || !Number.isFinite(qty) || !Number.isInteger(qty) || qty < 0) { formError.value = 'CurQty 必須是大於或等於 0 的整數。'; return }
  if (!form.curPeriod.trim() || !Number.isFinite(period)) { formError.value = 'CurPeriod 必須是數字。'; return }
  if (snapshot.value.logs.some(item => item.consumeNo === form.consumeNo && item.consumeSeq.toUpperCase() === form.consumeSeq.trim().toUpperCase())) { formError.value = '相同 ConsumeNo 的歷程已存在此 ConsumeSeq。'; return }
  const duplicateKey = snapshot.value.statuses.some(item => item.equipmentNo === form.equipmentNo && item.consumeNo === form.consumeNo && keyOf(item) !== (formMode.value === 'edit' ? activeKey.value : ''))
  if (duplicateKey) { formError.value = '此設備的 ConsumeNo 已有現況資料（複合主鍵重複）。'; return }
  if (formMode.value === 'edit') {
    const target = snapshot.value.statuses.find(item => keyOf(item) === activeKey.value)
    if (!target) { formError.value = '找不到目前選取的耗品現況。'; return }
    Object.assign(target, { consumeSeq: form.consumeSeq.trim(), curQty: qty, curPeriod: period })
  } else {
    snapshot.value.statuses.push({ equipmentNo: form.equipmentNo, consumeNo: form.consumeNo, consumeSeq: form.consumeSeq.trim(), curQty: qty, curPeriod: period, creator: userNo.value, createDate: nowText(), revisor: '', reviseDate: '' })
  }
  const savedKey = keyOf({ equipmentNo: form.equipmentNo, consumeNo: form.consumeNo })
  activeKey.value = savedKey
  selectedKeys.value = [savedKey]
  persist()
  formOpen.value = false
  feedback('', formMode.value === 'edit' ? '已更新設備耗品現況。' : formMode.value === 'copy' ? '已複製設備耗品現況。' : '已新增設備耗品現況。')
}
function requestDelete(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  if (!selectedRows.value.length) { feedback('請先選取要刪除的資料。'); return }
  deleteConfirmOpen.value = true
}
function deleteSelected(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  const deletedKeys = [...selectedKeys.value]
  snapshot.value.statuses = snapshot.value.statuses.filter(row => !deletedKeys.includes(keyOf(row)))
  selectedKeys.value = []
  activeKey.value = ''
  deleteConfirmOpen.value = false
  persist()
  feedback('', '已刪除選取的 ' + deletedKeys.length + ' 筆資料。')
}
function openReset(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  if (!activeRow.value) { feedback('請先選取一筆設備耗品現況。'); return }
  resetSource.value = { ...activeRow.value }
  resetAt.value = nowText()
  resetForm.consumeSeq = ''
  resetForm.curQty = activeRow.value.curQty
  resetForm.curPeriod = activeRow.value.curPeriod
  resetError.value = ''
  resetWasDepleted.value = false
  if (resetBasis.value) {
    const qtyRemain = resetBasis.value.quantity - activeRow.value.curQty
    const periodRemain = resetBasis.value.period === -1 ? 1 : resetBasis.value.period - activeRow.value.curPeriod
    if (qtyRemain <= 0 || periodRemain <= 0) {
      // The WinForms dialog zeroes its accumulated fields when either limit is reached.
      resetForm.consumeSeq = ''
      resetForm.curQty = 0
      resetForm.curPeriod = 0
      resetWasDepleted.value = true
    }
  }
  resetOpen.value = true
}
function confirmReset(): void {
  if (!dev.value) { feedback(productionUnavailable); return }
  resetError.value = ''
  const source = resetSource.value
  if (!source) { resetError.value = '找不到目前設備耗品現況。'; return }
  const newSequence = resetForm.consumeSeq.trim()
  if (!newSequence) { resetError.value = '新 ConsumeSeq 不可空白。'; return }
  if (newSequence.toUpperCase() === source.consumeSeq.trim().toUpperCase()) { resetError.value = '新 ConsumeSeq 必須與原序號不同。'; return }
  if (snapshot.value.logs.some(item => item.consumeNo === source.consumeNo && item.consumeSeq.toUpperCase() === newSequence.toUpperCase())) { resetError.value = '相同 ConsumeNo 的歷程已存在此 ConsumeSeq。'; return }
  const sourceRow = snapshot.value.statuses.find(item => keyOf(item) === keyOf(source))
  if (!sourceRow) { resetError.value = '設備耗品現況已變更，請重新查詢。'; return }
  const at = resetAt.value
  snapshot.value.logs.push({ equipmentNo: source.equipmentNo, consumeNo: source.consumeNo, consumeSeq: source.consumeSeq, creator: userNo.value, createDate: at, accQty: resetForm.curQty, accPeriod: resetForm.curPeriod })
  Object.assign(sourceRow, { consumeSeq: newSequence, curQty: 0, curPeriod: 0, revisor: userNo.value, reviseDate: at })
  persist()
  resetOpen.value = false
  resetSource.value = null
  feedback('', '已重置耗品序號並新增一筆耗品更換歷程。')
}

const formTitle = computed(() => formMode.value === 'add' ? '設備耗品現況 (ADD)' : formMode.value === 'edit' ? '設備耗品現況 (EDIT)' : '設備耗品現況 (Copy)')
const formDirty = computed(() => JSON.stringify(form) !== formBaseline.value)
</script>

<template>
  <section class="consume-status-page" aria-label="設備耗品現況">
    <p v-if="!dev" class="feedback" role="status">{{ productionUnavailable }}</p>
    <div class="main-layout">
      <section class="grid-panel">
        <div class="table-wrap"><table class="consume-grid">
          <thead><tr>
            <th>EquipmentNo</th><th>ConsumeNo</th><th>ConsumeSeq</th><th class="numeric">CurQty</th><th class="numeric">CurPeriod</th>
            <th>Creator</th><th>CreateDate</th><th>Revisor</th><th>ReviseDate</th>
          </tr></thead>
          <tbody>
            <tr v-for="row in sortedStatuses" :key="keyOf(row)" :class="{ selected: selectedKeys.includes(keyOf(row)) }" @click="selectRow($event, row)" @dblclick="openForm('edit', row)">
              <td>{{ row.equipmentNo }}</td><td>{{ row.consumeNo }}</td><td>{{ row.consumeSeq }}</td><td class="numeric">{{ row.curQty }}</td><td class="numeric">{{ row.curPeriod.toFixed(2) }}</td>
              <td>{{ row.creator }}</td><td>{{ row.createDate }}</td><td>{{ row.revisor }}</td><td>{{ row.reviseDate }}</td>
            </tr>
            <tr v-if="sortedStatuses.length === 0"><td colspan="9" class="empty-state">{{ dev ? '沒有設備耗品現況資料' : productionUnavailable }}</td></tr>
          </tbody>
        </table></div>
        <div class="grid-footer"><span>資料總筆數 {{ sortedStatuses.length }}</span><span>選取 {{ selectedRows.length }} 筆</span><span class="hint">Ctrl／⌘ 點選可多選刪除；雙擊列可編輯</span></div>
      </section>
      <aside class="action-rail" aria-label="設備耗品現況操作">
        <button class="primary-button" type="button" :disabled="!dev" @click="openForm('add')">ADD (&amp;A)</button>
        <button type="button" :disabled="!dev || !activeRow" @click="openEdit">EDIT (&amp;E)</button>
        <button type="button" :disabled="!dev || !activeRow" @click="openCopy">Copy (&amp;C)</button>
        <button type="button" :disabled="!dev || selectedRows.length === 0" @click="requestDelete">DEL (&amp;D)</button>
        <div class="rail-spacer"></div>
        <button type="button" :disabled="!dev || !activeRow" @click="openReset">ConsumReset</button>
        <button type="button" @click="emit('close')">Close (&amp;X)</button>
      </aside>
    </div>
    <p v-if="errorText" class="feedback error" role="alert">{{ errorText }}</p>
    <p v-else-if="successText" class="feedback success" role="status">{{ successText }}</p>
    <footer class="status-bar"><span>{{ dev ? 'DEV 假資料' : '服務尚未提供' }}</span><span>操作者 {{ userNo }}</span><span class="status-spacer">WP303 設備耗品現況</span><button v-if="dev" type="button" class="reset-dev" @click="resetDev">還原假資料</button></footer>

    <div v-if="dev && formOpen" class="modal-backdrop" role="presentation" @click.self="formOpen = false">
      <section class="dialog definition-dialog" role="dialog" aria-modal="true" aria-labelledby="wp303-form-title">
        <h2 id="wp303-form-title">{{ formTitle }}</h2>
        <fieldset class="form-group"><legend>EQP Cunsum Status</legend>
          <div class="form-grid">
            <label for="wp303-equipment">EquipmentNo</label><div class="input-with-button"><input id="wp303-equipment" v-model="form.equipmentNo" class="kd-input" readonly :disabled="formMode === 'edit'"><button type="button" :disabled="!dev || formMode === 'edit'" @click="openEquipmentPicker">...</button></div>
            <label for="wp303-consume">Consume No</label><select id="wp303-consume" v-model="form.consumeNo" class="kd-input" :disabled="formMode === 'edit' || !form.equipmentNo"><option value="">請選擇</option><option v-for="basis in formConsumeOptions" :key="basis.consumeNo" :value="basis.consumeNo">{{ basis.consumeNo }}</option></select>
            <label for="wp303-seq">Consume Seq</label><input id="wp303-seq" v-model="form.consumeSeq" class="kd-input" :disabled="!dev" maxlength="30">
            <label for="wp303-qty">Cur Qty</label><input id="wp303-qty" v-model="form.curQty" class="kd-input numeric" :disabled="!dev" inputmode="numeric">
            <label for="wp303-period">Cur Period</label><input id="wp303-period" v-model="form.curPeriod" class="kd-input numeric" :disabled="!dev" inputmode="decimal">
            <label for="wp303-creator">Creator</label><input id="wp303-creator" :value="form.creator" class="kd-input audit-input" readonly>
            <label for="wp303-createdate">Create Date</label><input id="wp303-createdate" :value="form.createDate" class="kd-input audit-input" readonly>
          </div>
        </fieldset>
        <p v-if="formError" class="feedback error" role="alert">{{ formError }}</p>
        <div class="dialog-actions"><button class="primary-button" type="button" :disabled="!dev" @click="saveForm">Confirm</button><button type="button" @click="formOpen = false">Close</button><button type="button" :disabled="!dev || !formDirty" @click="saveForm">Apply</button></div>
      </section>
    </div>

    <div v-if="dev && equipmentPickerOpen" class="modal-backdrop nested-backdrop" role="presentation" @click.self="equipmentPickerOpen = false">
      <section class="dialog picker-dialog" role="dialog" aria-modal="true" aria-labelledby="wp303-picker-title">
        <h2 id="wp303-picker-title">選擇設備 (IssueState = 2)</h2>
        <label class="picker-search">搜尋設備／類別<input v-model="equipmentFilter" class="kd-input" autocomplete="off"></label>
        <div class="picker-list"><button v-for="item in filteredEquipment" :key="item.equipmentNo" type="button" @click="chooseEquipment(item)"><strong>{{ item.equipmentNo }}</strong><span>{{ item.equipmentType }}　{{ item.description }}</span></button><p v-if="filteredEquipment.length === 0" class="empty-state">沒有符合條件的啟用設備</p></div>
        <div class="dialog-actions"><button type="button" @click="equipmentPickerOpen = false">Close</button></div>
      </section>
    </div>

    <div v-if="dev && deleteConfirmOpen" class="modal-backdrop nested-backdrop" role="presentation" @click.self="deleteConfirmOpen = false">
      <section class="dialog confirm-dialog" role="dialog" aria-modal="true" aria-labelledby="wp303-delete-title">
        <h2 id="wp303-delete-title">確認刪除</h2><p>確定刪除選取的 {{ selectedRows.length }} 筆設備耗品現況資料？</p>
        <div class="dialog-actions"><button type="button" @click="deleteConfirmOpen = false">Cancel</button><button class="danger-button" type="button" :disabled="!dev" @click="deleteSelected">OK</button></div>
      </section>
    </div>

    <div v-if="dev && resetOpen && resetSource" class="modal-backdrop" role="presentation" @click.self="resetOpen = false">
      <section class="dialog reset-dialog" role="dialog" aria-modal="true" aria-labelledby="wp303-reset-title">
        <h2 id="wp303-reset-title">EQP Cunsum Status Reset</h2>
        <fieldset class="form-group"><legend>EQP Cunsum Status Reset</legend>
          <div class="form-grid reset-grid">
            <label>EquipmentNo</label><input :value="resetSource.equipmentNo" class="kd-input audit-input" readonly>
            <label>Consume No</label><input :value="resetSource.consumeNo" class="kd-input audit-input" readonly>
            <label for="wp303-reset-seq">Consume Seq</label><input id="wp303-reset-seq" v-model="resetForm.consumeSeq" class="kd-input" maxlength="30" @input="resetError = ''">
            <label>Used Qty</label><input :value="resetForm.curQty" class="kd-input numeric audit-input" readonly>
            <label>Produced Hours</label><input :value="resetForm.curPeriod.toFixed(2)" class="kd-input numeric audit-input" readonly>
            <label>Remain Qty</label><input :value="remainingQty ?? ''" class="kd-input numeric audit-input" readonly>
            <label>Remain Period</label><input :value="remainingPeriod ?? ''" class="kd-input numeric audit-input" readonly>
            <label>Creator</label><input :value="resetSource.creator" class="kd-input audit-input" readonly>
            <label>Create Date</label><input :value="resetSource.createDate" class="kd-input audit-input" readonly>
            <label>Revisor</label><input :value="userNo" class="kd-input audit-input" readonly>
            <label>Revise Date</label><input :value="resetAt" class="kd-input audit-input" readonly>
          </div>
        </fieldset>
        <p v-if="resetWasDepleted" class="notice">來源表單在耗用量或週期達到上限時，會將本次重置記錄的 Used Qty／Produced Hours 清為 0。</p>
        <p v-if="resetError" class="feedback error" role="alert">{{ resetError }}</p>
        <div class="dialog-actions"><button type="button" @click="resetOpen = false">Close</button><button class="primary-button" type="button" :disabled="!dev || !resetForm.consumeSeq.trim()" @click="confirmReset">Confirm</button></div>
      </section>
    </div>
  </section>
</template>

<style scoped>
.consume-status-page { height: 100%; min-height: 0; display: flex; flex-direction: column; gap: 7px; padding: 12px; color: #292929; background: #fff; overflow: auto; }
.main-layout { flex: 1 1 auto; min-height: 300px; display: grid; grid-template-columns: minmax(0, 1fr) 96px; gap: 7px; }
.grid-panel { min-width: 0; min-height: 0; display: flex; flex-direction: column; border: 1px solid #aaa; }
.table-wrap { min-height: 0; flex: 1 1 auto; overflow: auto; }
.consume-grid { width: 100%; border-collapse: collapse; font-size: 12px; }
.consume-grid th { position: sticky; top: 0; z-index: 1; padding: 7px 6px; border: 1px solid #c5c5c5; color: #303030; background: #e8e8e8; text-align: left; white-space: nowrap; }
.consume-grid td { padding: 6px; border: 1px solid #d1d1d1; background: #fff0ad; white-space: nowrap; }
.consume-grid tbody tr { cursor: pointer; }
.consume-grid tbody tr.selected td { outline: 1px solid #a77a16; outline-offset: -1px; background: #ffe18a; }
.numeric { text-align: right; }
.empty-state { padding: 14px !important; color: #777; text-align: center; background: #fafafa !important; }
.grid-footer { min-height: 24px; display: flex; align-items: center; gap: 14px; padding: 3px 7px; border-top: 1px solid #bbb; color: #666; font-size: 11px; }
.hint { margin-left: auto; }
.action-rail { display: flex; flex-direction: column; gap: 6px; }
button { min-height: 32px; border: 1px solid #aaa; border-radius: 3px; padding: 4px 8px; color: #333; background: #f8f8f8; font: inherit; white-space: nowrap; cursor: pointer; }
button:disabled { cursor: not-allowed; color: #999 !important; border-color: #ccc !important; background: #eee !important; }
.primary-button { border-color: #d58b16; color: #fff; background: #f59a18; }
.danger-button { border-color: #a54236; color: #fff; background: #b64a42; }
.rail-spacer { flex: 1 1 auto; }
.feedback { margin: 0; padding: 7px 10px; border: 1px solid; font-size: 13px; }
.feedback.error { border-color: #d5a2a2; color: #812c2c; background: #fff0f0; }
.feedback.success { border-color: #b5c9a8; color: #3e6631; background: #f1faeb; }
.status-bar { min-height: 24px; display: flex; align-items: center; gap: 14px; padding: 2px 7px; border: 1px solid #bbb; color: #555; background: #f5f5f5; font-size: 11px; }
.status-spacer { flex: 1 1 auto; }
.reset-dev { min-height: 22px; padding: 1px 7px; font-size: 11px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 1000; display: grid; place-items: center; padding: 16px; background: rgb(0 0 0 / 34%); }
.nested-backdrop { z-index: 1010; }
.dialog { width: min(440px, 100%); max-height: min(92vh, 760px); overflow: auto; border: 1px solid #aaa; border-radius: 4px; padding: 14px; background: #fff; box-shadow: 0 8px 30px rgb(0 0 0 / 20%); }
.dialog h2 { margin: 0 0 12px; font-size: 17px; font-weight: 600; }
.form-group { margin: 0; border: 1px solid #aaa; padding: 8px 10px 10px; }
.form-group legend { padding: 0 5px; color: #444; font-weight: 600; }
.form-grid { display: grid; grid-template-columns: 96px minmax(0, 1fr); align-items: center; gap: 8px; }
.form-grid > label { text-align: right; white-space: nowrap; }
.kd-input { width: 100%; min-width: 0; height: 29px; border: 1px solid #929292; border-radius: 0; padding: 4px 7px; color: #292929; background: #fff0ad; font: inherit; }
.kd-input:focus { outline: 2px solid #c99a30; }
.kd-input:disabled, .audit-input { color: #555; background: #eee; }
.input-with-button { display: grid; grid-template-columns: minmax(0, 1fr) 34px; gap: 5px; }
.dialog-actions { display: flex; justify-content: flex-end; gap: 7px; margin-top: 12px; }
.dialog-actions .primary-button { min-width: 80px; }
.picker-dialog { width: min(540px, 100%); }
.picker-search { display: grid; grid-template-columns: 130px minmax(0, 1fr); align-items: center; gap: 8px; margin-bottom: 10px; }
.picker-list { max-height: 54vh; overflow: auto; display: flex; flex-direction: column; gap: 5px; }
.picker-list button { display: flex; justify-content: space-between; gap: 12px; text-align: left; }
.picker-list span { color: #666; }
.confirm-dialog { width: min(390px, 100%); }
.confirm-dialog p { line-height: 1.6; }
.reset-dialog { width: min(660px, 100%); }
.reset-grid { grid-template-columns: 125px minmax(0, 1fr) 125px minmax(0, 1fr); }
.reset-grid > label { text-align: right; }
.notice { margin: 9px 0 0; padding: 7px 9px; border: 1px solid #dfc38a; color: #735716; background: #fff8df; font-size: 12px; line-height: 1.5; }
@media (max-width: 900px) {
  .consume-status-page { height: auto; min-height: 100%; }
  .main-layout { grid-template-columns: 1fr; }
  .grid-panel { min-height: 330px; }
  .action-rail { flex-direction: row; flex-wrap: wrap; }
  .rail-spacer { flex-basis: 100%; }
  .reset-grid { grid-template-columns: 110px minmax(0, 1fr); }
}
</style>
