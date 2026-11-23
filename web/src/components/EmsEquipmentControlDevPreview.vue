<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'

type Equipment = {
  equipmentNo: string
  equipmentType: string
  chamberFlag: 0 | 1
  belongEquipmentNo?: string
  issueState: 2
}

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()

const equipmentTypes = ['FT Mounter', 'FT Tester', 'Oven']
const equipment: Equipment[] = [
  { equipmentNo: 'MNT-01', equipmentType: 'FT Mounter', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'MNT-01-A', equipmentType: 'FT Mounter', chamberFlag: 1, belongEquipmentNo: 'MNT-01', issueState: 2 },
  { equipmentNo: 'MNT-01-B', equipmentType: 'FT Mounter', chamberFlag: 1, belongEquipmentNo: 'MNT-01', issueState: 2 },
  { equipmentNo: 'MNT-02', equipmentType: 'FT Mounter', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'MNT-02-A', equipmentType: 'FT Mounter', chamberFlag: 1, belongEquipmentNo: 'MNT-02', issueState: 2 },
  { equipmentNo: 'TST-01', equipmentType: 'FT Tester', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'TST-01-A', equipmentType: 'FT Tester', chamberFlag: 1, belongEquipmentNo: 'TST-01', issueState: 2 },
  { equipmentNo: 'TST-02', equipmentType: 'FT Tester', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'OVN-01', equipmentType: 'Oven', chamberFlag: 0, issueState: 2 },
  { equipmentNo: 'OVN-02', equipmentType: 'Oven', chamberFlag: 0, issueState: 2 },
]

const appliedEquipment = ref(new Set(['MNT-01', 'MNT-01-A', 'MNT-01-B', 'TST-02']))
const pendingAdd = ref(new Set<string>())
const pendingDelete = ref(new Set<string>())
const selectedType = ref(equipmentTypes[0])
const availableSelection = ref<string[]>([])
const controlledSelection = ref<string[]>([])
const statusMessage = ref('')
const isConfirming = ref(false)
let closeTimer: ReturnType<typeof setTimeout> | undefined

const parents = computed(() => equipment
  .filter((row) => row.issueState === 2 && row.chamberFlag === 0 && row.equipmentType === selectedType.value)
  .sort((a, b) => a.equipmentNo.localeCompare(b.equipmentNo)))
const chamberByParent = computed(() => {
  const children = new Map<string, Equipment[]>()
  for (const row of equipment) {
    if (row.chamberFlag !== 1 || !row.belongEquipmentNo) continue
    children.set(row.belongEquipmentNo, [...(children.get(row.belongEquipmentNo) ?? []), row])
  }
  return children
})
const isControlled = (equipmentNo: string) => pendingAdd.value.has(equipmentNo)
  || (appliedEquipment.value.has(equipmentNo) && !pendingDelete.value.has(equipmentNo))
const availableParents = computed(() => parents.value.filter((row) => !isControlled(row.equipmentNo)))
const controlledParents = computed(() => parents.value.filter((row) => isControlled(row.equipmentNo)))
const pendingCount = computed(() => pendingAdd.value.size + pendingDelete.value.size)
const pendingAddCount = computed(() => pendingAdd.value.size)
const pendingDeleteCount = computed(() => pendingDelete.value.size)
const isDirty = computed(() => pendingCount.value > 0)

watch(selectedType, () => {
  availableSelection.value = []
  controlledSelection.value = []
})

function relatedRows(parentNo: string) {
  return [
    equipment.find((row) => row.equipmentNo === parentNo),
    ...(chamberByParent.value.get(parentNo) ?? []),
  ].filter((row): row is Equipment => Boolean(row))
}

function moveToControlled() {
  for (const parentNo of [...availableSelection.value]) {
    for (const row of relatedRows(parentNo)) {
      if (!pendingDelete.value.delete(row.equipmentNo) && !appliedEquipment.value.has(row.equipmentNo)) {
        pendingAdd.value.add(row.equipmentNo)
      }
    }
  }
  availableSelection.value = []
  statusMessage.value = ''
}

function moveToAvailable() {
  for (const parentNo of [...controlledSelection.value]) {
    for (const row of relatedRows(parentNo)) {
      if (!pendingAdd.value.delete(row.equipmentNo) && appliedEquipment.value.has(row.equipmentNo)) {
        pendingDelete.value.add(row.equipmentNo)
      }
    }
  }
  controlledSelection.value = []
  statusMessage.value = ''
}

function applyChanges() {
  for (const equipmentNo of pendingDelete.value) appliedEquipment.value.delete(equipmentNo)
  for (const equipmentNo of pendingAdd.value) appliedEquipment.value.add(equipmentNo)
  pendingAdd.value = new Set()
  pendingDelete.value = new Set()
  availableSelection.value = []
  controlledSelection.value = []
  statusMessage.value = 'DEV 假資料已套用；頁面保持開啟。'
}

function confirmChanges() {
  applyChanges()
  statusMessage.value = 'DEV 假資料確認完成；視窗即將關閉。'
  isConfirming.value = true
  closeTimer = setTimeout(() => emit('close'), 900)
}

function closeWindow() {
  emit('close')
}

onBeforeUnmount(() => {
  if (closeTimer) clearTimeout(closeTimer)
})
</script>

<template>
  <section class="equipment-control" aria-label="設備控管設定 DEV 預覽" :data-development-preview="props.developmentPreview ? 'true' : 'false'">
    <header class="page-heading">
      <div>
        <p class="eyebrow">EMS CONFIG · ES101</p>
        <h1>設備控管設定</h1>
        <p class="subheading">依設備類別管理納入 EMS 狀態控管的設備。</p>
      </div>
      <span class="dev-badge">DEV 假資料</span>
    </header>

    <div class="toolbar">
      <label for="equipment-type">Equipment Type</label>
      <select id="equipment-type" v-model="selectedType" :disabled="isConfirming">
        <option v-for="type in equipmentTypes" :key="type" :value="type">{{ type }}</option>
      </select>
      <span class="toolbar-note">只顯示核准設備；Chamber 隨所屬設備一起變更。</span>
    </div>

    <div class="transfer-panel">
      <section class="equipment-list" aria-labelledby="available-title">
        <div class="list-heading">
          <div>
            <h2 id="available-title">Unselect Equipments</h2>
            <p>未納入控管的設備</p>
          </div>
          <span class="count">{{ availableParents.length }}</span>
        </div>
        <div class="list-body" role="group" aria-label="未管制設備">
          <label v-for="row in availableParents" :key="row.equipmentNo" class="equipment-row">
            <input v-model="availableSelection" type="checkbox" :value="row.equipmentNo" :disabled="isConfirming">
            <span class="equipment-no">{{ row.equipmentNo }}</span>
            <span v-if="chamberByParent.get(row.equipmentNo)?.length" class="chamber-tag">
              {{ chamberByParent.get(row.equipmentNo)?.length }} Chamber
            </span>
          </label>
          <p v-if="availableParents.length === 0" class="empty-state">此類別沒有未管制設備</p>
        </div>
      </section>

      <div class="transfer-actions" aria-label="移動設備">
        <button type="button" class="move-button" title="加入設備控管" aria-label="加入設備控管" :disabled="!availableSelection.length || isConfirming" @click="moveToControlled">→</button>
        <button type="button" class="move-button" title="移出設備控管" aria-label="移出設備控管" :disabled="!controlledSelection.length || isConfirming" @click="moveToAvailable">←</button>
      </div>

      <section class="equipment-list controlled-list" aria-labelledby="controlled-title">
        <div class="list-heading">
          <div>
            <h2 id="controlled-title">Selected Equipments</h2>
            <p>已納入控管的設備</p>
          </div>
          <span class="count">{{ controlledParents.length }}</span>
        </div>
        <div class="list-body" role="group" aria-label="已管制設備">
          <label v-for="row in controlledParents" :key="row.equipmentNo" class="equipment-row">
            <input v-model="controlledSelection" type="checkbox" :value="row.equipmentNo" :disabled="isConfirming">
            <span class="equipment-no">{{ row.equipmentNo }}</span>
            <span v-if="chamberByParent.get(row.equipmentNo)?.length" class="chamber-tag">
              {{ chamberByParent.get(row.equipmentNo)?.length }} Chamber
            </span>
          </label>
          <p v-if="controlledParents.length === 0" class="empty-state">此類別沒有已管制設備</p>
        </div>
      </section>
    </div>

    <div class="change-summary" :class="{ 'has-changes': isDirty }" aria-live="polite">
      <span class="summary-state"><i />{{ isDirty ? '有尚未套用的異動' : '目前沒有待套用異動' }}</span>
      <span>新增 {{ pendingAddCount }} 筆</span>
      <span>移除 {{ pendingDeleteCount }} 筆</span>
      <span class="summary-explainer">異動筆數含自動納入或移除的 Chamber。</span>
    </div>

    <p v-if="statusMessage" class="status-message" role="status">{{ statusMessage }}</p>

    <footer class="form-actions">
      <div class="action-note">新增設備會以 Idle 狀態建立；Close 會捨棄尚未套用的異動。</div>
      <div class="buttons">
        <button type="button" class="button secondary" :disabled="isConfirming" @click="closeWindow">Close</button>
        <button type="button" class="button secondary" :disabled="!isDirty || isConfirming" @click="applyChanges">Apply</button>
        <button type="button" class="button primary" :disabled="!isDirty || isConfirming" @click="confirmChanges">Confirm</button>
      </div>
    </footer>
  </section>
</template>

<style scoped>
.equipment-control { display: flex; flex-direction: column; gap: 16px; min-height: 0; height: 100%; box-sizing: border-box; padding: 22px; overflow: auto; background: #f5f4ee; color: #2b302e; font: inherit; }
.page-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.eyebrow { margin: 0 0 5px; color: #6e786f; font-size: 11px; font-weight: 700; letter-spacing: .12em; }
h1 { margin: 0; color: #202722; font-size: 23px; font-weight: 650; }
.subheading { margin: 6px 0 0; color: #727a74; font-size: 13px; }
.dev-badge { flex: 0 0 auto; padding: 5px 9px; border: 1px solid #c9dfd0; border-radius: 999px; background: #e8f3ec; color: #367148; font-size: 11px; font-weight: 700; }
.toolbar { display: flex; align-items: center; gap: 10px; min-height: 46px; padding: 0 12px; border: 1px solid #dfe3dd; border-radius: 8px; background: #fff; }
.toolbar label { color: #515c55; font-size: 12px; font-weight: 700; }
.toolbar select { min-width: 190px; height: 31px; padding: 0 9px; border: 1px solid #cfd7d0; border-radius: 5px; background: #fff; color: #29332d; font: inherit; }
.toolbar-note { margin-left: auto; color: #778078; font-size: 11px; }
.transfer-panel { display: grid; grid-template-columns: minmax(0, 1fr) 52px minmax(0, 1fr); gap: 15px; align-items: center; min-height: 300px; }
.equipment-list { display: flex; flex-direction: column; min-width: 0; height: 310px; border: 1px solid #dce1db; border-radius: 9px; background: #fff; box-shadow: 0 4px 13px #23372b0a; }
.list-heading { display: flex; justify-content: space-between; align-items: center; padding: 13px 14px 11px; border-bottom: 1px solid #edf0eb; }
.list-heading h2 { margin: 0; color: #2c3730; font-size: 13px; font-weight: 700; }
.list-heading p { margin: 3px 0 0; color: #8a918a; font-size: 11px; }
.count { display: grid; min-width: 26px; height: 26px; place-items: center; border-radius: 50%; background: #eff3ef; color: #4e6856; font-size: 11px; font-weight: 700; }
.list-body { flex: 1; overflow: auto; padding: 7px; }
.equipment-row { display: flex; align-items: center; gap: 9px; min-height: 37px; padding: 0 8px; border-radius: 5px; cursor: pointer; }
.equipment-row:hover { background: #f4f7f3; }
.equipment-row input { width: 15px; height: 15px; accent-color: #486f54; }
.equipment-no { color: #303a33; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; font-size: 12px; font-weight: 600; }
.chamber-tag { margin-left: auto; padding: 3px 6px; border-radius: 4px; background: #f2f4f1; color: #778178; font-size: 10px; }
.empty-state { margin: 0; padding: 18px 10px; color: #929890; font-size: 12px; text-align: center; }
.transfer-actions { display: flex; flex-direction: column; gap: 10px; }
.move-button { width: 42px; height: 38px; border: 1px solid #cad4cb; border-radius: 7px; background: #fff; color: #45664f; font-size: 20px; cursor: pointer; }
.move-button:disabled { color: #b9c0b9; cursor: default; }
.move-button:not(:disabled):hover { border-color: #789781; background: #edf5ef; }
.move-button:focus-visible, .button:focus-visible, .toolbar select:focus-visible { outline: 2px solid #5e8c6c; outline-offset: 2px; }
.change-summary { display: flex; align-items: center; gap: 18px; min-height: 42px; padding: 0 12px; border: 1px solid #dfe3dd; border-radius: 7px; background: #fafbf9; color: #737d74; font-size: 11px; }
.change-summary.has-changes { border-color: #e4d7aa; background: #fcf8e8; color: #806a2e; }
.summary-state { display: inline-flex; align-items: center; gap: 7px; color: #59645d; font-weight: 650; }
.summary-state i { width: 7px; height: 7px; border-radius: 50%; background: #9ba49d; }
.has-changes .summary-state i { background: #cc9a2b; }
.summary-explainer { margin-left: auto; color: #8b908a; }
.status-message { margin: -5px 0 0; color: #3b724a; font-size: 12px; }
.form-actions { display: flex; justify-content: space-between; align-items: center; gap: 16px; margin-top: auto; padding-top: 10px; border-top: 1px solid #dfe3dd; }
.action-note { color: #868d86; font-size: 11px; }
.buttons { display: flex; gap: 8px; }
.button { min-width: 82px; height: 34px; padding: 0 12px; border: 1px solid #cfd6cf; border-radius: 6px; background: #fff; color: #405047; font: inherit; font-size: 12px; font-weight: 650; cursor: pointer; }
.button:disabled { opacity: .48; cursor: default; }
.button.primary { border-color: #3f6749; background: #426e4d; color: #fff; }
.button.primary:not(:disabled):hover { background: #365e40; }
.button.secondary:not(:disabled):hover { background: #f3f5f2; }
@media (max-width: 700px) {
  .equipment-control { padding: 15px; }
  .transfer-panel { grid-template-columns: 1fr; gap: 9px; }
  .equipment-list { height: 220px; }
  .transfer-actions { flex-direction: row; justify-content: center; }
  .move-button { width: 48px; }
  .toolbar { flex-wrap: wrap; padding: 9px; }
  .toolbar-note { width: 100%; margin-left: 0; }
  .change-summary { flex-wrap: wrap; gap: 9px 14px; padding: 10px 12px; }
  .summary-explainer { width: 100%; margin-left: 0; }
  .form-actions { align-items: flex-start; flex-direction: column; }
  .buttons { align-self: flex-end; }
}
</style>
