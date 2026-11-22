<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean; currentUserNo?: string }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface MountedAccessory {
  accessoryNo: string
  accessoryType: string
  version: string
  equipmentType: string
  equipmentNo: string
  equipmentName: string
  stateName: string
  stateStartedAt: string
  stateUser: string
  productionLotNo: string
  logGroupSerial: string
  lifeAlert: string
  returnBlocked: boolean
}

interface ReturnLog {
  accessoryNo: string
  accessoryType: string
  version: string
  equipmentNo: string
  returnedAt: string
  userNo: string
  productionNote: string
}

const fixtures: MountedAccessory[] = dev.value ? [
  {
    accessoryNo: 'DEV-ACC-CHUCK-01', accessoryType: 'CHUCK', version: '01',
    equipmentType: 'DEV-TESTER', equipmentNo: 'DEV-EQP-101', equipmentName: '測試機 101',
    stateName: '已上機', stateStartedAt: '2026-11-23 08:42:00', stateUser: 'DEV-OP-01',
    productionLotNo: 'DEV-LOT-210', logGroupSerial: 'DEV-GRP-210',
    lifeAlert: '示意：已達保養時數，歸還後需檢查。', returnBlocked: false,
  },
  {
    accessoryNo: 'DEV-ACC-CHUCK-02', accessoryType: 'CHUCK', version: '02',
    equipmentType: 'DEV-TESTER', equipmentNo: 'DEV-EQP-102', equipmentName: '測試機 102',
    stateName: '已上機', stateStartedAt: '2026-11-22 16:05:00', stateUser: 'DEV-OP-02',
    productionLotNo: '', logGroupSerial: '', lifeAlert: '', returnBlocked: false,
  },
  {
    accessoryNo: 'DEV-ACC-LOAD-01', accessoryType: 'LOAD BOARD', version: '01',
    equipmentType: 'DEV-FT', equipmentNo: 'DEV-EQP-201', equipmentName: '測試機 201',
    stateName: '已上機', stateStartedAt: '2026-11-21 13:28:00', stateUser: 'DEV-OP-03',
    productionLotNo: 'DEV-LOT-311', logGroupSerial: 'DEV-GRP-311',
    lifeAlert: '', returnBlocked: false,
  },
  {
    accessoryNo: 'DEV-ACC-SOCKET-01', accessoryType: 'SOCKET', version: '01',
    equipmentType: 'DEV-TESTER', equipmentNo: 'DEV-EQP-301', equipmentName: '測試機 301',
    stateName: '待檢修', stateStartedAt: '2026-11-20 10:12:00', stateUser: 'DEV-OP-04',
    productionLotNo: '', logGroupSerial: '', lifeAlert: '', returnBlocked: true,
  },
] : []

const mountedAccessories = ref<MountedAccessory[]>(fixtures.map((row) => ({ ...row })))
const returnLogs = ref<ReturnLog[]>([])
const selectedType = ref('')
const selectedAccessoryNo = ref('')
const lookupTypeOpen = ref(false)
const lookupAccessoryOpen = ref(false)
const lookupText = ref('')
const message = ref('')
const messageKind = ref<'info' | 'warning' | 'error' | 'success'>('info')
const creator = computed(() => props.currentUserNo?.trim() || 'DEV-USER')
const createDate = new Date().toLocaleString('zh-TW', { hour12: false })
const selectedAccessory = computed(() => mountedAccessories.value.find((row) => row.accessoryNo === selectedAccessoryNo.value) ?? null)
const distinctTypes = computed(() => [...new Set(mountedAccessories.value.map((row) => row.accessoryType))].sort())
const visibleAccessories = computed(() => mountedAccessories.value
  .filter((row) => !selectedType.value || row.accessoryType === selectedType.value)
  .filter((row) => !lookupText.value || `${row.accessoryNo} ${row.accessoryType} ${row.equipmentNo} ${row.equipmentName}`.toLowerCase().includes(lookupText.value.trim().toLowerCase())))

const queryParams = typeof window === 'undefined' ? new URLSearchParams() : new URLSearchParams(window.location.search)
const deepLinkAccessoryNo = queryParams.get('AccessoryNo') || queryParams.get('accessoryNo') || ''
const deepLinkAccessory = mountedAccessories.value.find((row) => row.accessoryNo.toUpperCase() === deepLinkAccessoryNo.toUpperCase())
if (deepLinkAccessory) {
  selectedType.value = deepLinkAccessory.accessoryType
  selectedAccessoryNo.value = deepLinkAccessory.accessoryNo
  message.value = '已依監控傳入的配件編號帶入 DEV 掛載資料；設備與版本由假資料列補齊。'
}

function openTypeLookup(): void {
  lookupTypeOpen.value = true
  lookupAccessoryOpen.value = false
  lookupText.value = ''
}

function chooseType(type: string): void {
  selectedType.value = type
  selectedAccessoryNo.value = ''
  lookupTypeOpen.value = false
  message.value = ''
}

function openAccessoryLookup(): void {
  lookupAccessoryOpen.value = true
  lookupTypeOpen.value = false
  lookupText.value = ''
}

function closeLookup(): void {
  lookupTypeOpen.value = false
  lookupAccessoryOpen.value = false
}

function chooseAccessory(row: MountedAccessory): void {
  selectedType.value = row.accessoryType
  selectedAccessoryNo.value = row.accessoryNo
  lookupAccessoryOpen.value = false
  message.value = row.lifeAlert
  messageKind.value = row.lifeAlert ? 'warning' : 'info'
}

function confirmReturn(): void {
  const row = selectedAccessory.value
  if (!row) {
    message.value = '請先選擇目前已掛載的配件。'
    messageKind.value = 'error'
    return
  }
  if (row.returnBlocked) {
    message.value = `DEV 互斥規則阻擋：${row.accessoryNo} 目前狀態「${row.stateName}」不可轉成 RETURN。`
    messageKind.value = 'error'
    return
  }

  const returnedAt = new Date().toLocaleString('zh-TW', { hour12: false })
  const productionNote = row.productionLotNo
    ? `已模擬結束 ${row.productionLotNo} 的配件使用紀錄（${row.logGroupSerial}）`
    : '設備目前無生產批，未模擬更新生產配件使用紀錄'
  returnLogs.value.unshift({
    accessoryNo: row.accessoryNo,
    accessoryType: row.accessoryType,
    version: row.version,
    equipmentNo: row.equipmentNo,
    returnedAt,
    userNo: creator.value,
    productionNote,
  })
  mountedAccessories.value = mountedAccessories.value.filter((item) => item.accessoryNo !== row.accessoryNo)
  selectedAccessoryNo.value = ''
  selectedType.value = ''
  message.value = `${row.accessoryNo} 已在 DEV 假資料中模擬下機。${row.lifeAlert ? ` ${row.lifeAlert}` : ''}`
  messageKind.value = row.lifeAlert ? 'warning' : 'success'
}
</script>

<template>
  <section v-if="dev" class="return-accessory-page" aria-label="配件下機 DEV">
    <header class="page-header">
      <div>
        <div class="eyebrow">EMS · ES302 · DEV fixtures</div>
        <h1>配件下機 <span>RETURN EQP ACCESSORY</span></h1>
      </div>
      <button type="button" class="close-button" aria-label="關閉" @click="emit('close')">×</button>
    </header>

    <div class="fixture-banner"><strong>DEV 假資料</strong><span>所有選取與下機效果只保留在本頁記憶體。</span></div>

    <section class="selection-card" aria-label="選擇配件">
      <div class="card-title">Select Accessory</div>
      <div class="form-grid">
        <label for="return-type">Accessory Type</label>
        <div class="lookup-field">
          <input id="return-type" :value="selectedType" readonly placeholder="選擇配件類別" />
          <button type="button" aria-label="查找配件類別" @click="openTypeLookup">…</button>
        </div>

        <label for="return-no">Accessory No</label>
        <div class="lookup-field">
          <input id="return-no" :value="selectedAccessory?.accessoryNo || ''" readonly placeholder="選擇已掛載配件" />
          <button type="button" aria-label="查找已掛載配件" @click="openAccessoryLookup">…</button>
        </div>

        <label for="return-creator">Creator</label>
        <input id="return-creator" :value="creator" readonly />
        <label for="return-date">Create Date</label>
        <input id="return-date" :value="createDate" readonly />
      </div>

      <div v-if="selectedAccessory" class="mount-summary">
        <div><span>Equipment</span><strong>{{ selectedAccessory.equipmentNo }} · {{ selectedAccessory.equipmentName }}</strong></div>
        <div><span>Version / Current State</span><strong>{{ selectedAccessory.version }} / {{ selectedAccessory.stateName }}</strong></div>
        <div><span>Mounted Since</span><strong>{{ selectedAccessory.stateStartedAt }} · {{ selectedAccessory.stateUser }}</strong></div>
        <div><span>In-process Lot</span><strong>{{ selectedAccessory.productionLotNo || '無' }}</strong></div>
      </div>

      <div class="form-actions">
        <button type="button" class="primary-button" :disabled="!selectedAccessory" @click="confirmReturn">Confirm Return</button>
        <button type="button" class="secondary-button" @click="emit('close')">Close</button>
      </div>
      <p v-if="message" class="feedback" :class="`feedback-${messageKind}`" role="status">{{ message }}</p>
    </section>

    <section class="mounted-card" aria-label="目前已掛載配件">
      <div class="card-heading"><div><h2>目前掛載配件</h2><span>Accessory state list</span></div><b>{{ mountedAccessories.length }} 件</b></div>
      <div class="table-scroll">
        <table>
          <thead><tr><th>配件編號</th><th>類別</th><th>版本</th><th>設備</th><th>目前狀態</th><th>生產批</th></tr></thead>
          <tbody>
            <tr v-for="row in mountedAccessories" :key="row.accessoryNo" :class="{ selected: row.accessoryNo === selectedAccessoryNo }" @click="chooseAccessory(row)">
              <td>{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.version }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.stateName }}</td><td>{{ row.productionLotNo || '—' }}</td>
            </tr>
            <tr v-if="mountedAccessories.length === 0"><td colspan="6" class="empty-row">目前沒有可下機的掛載配件。</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section v-if="returnLogs.length" class="history-card" aria-label="本頁模擬下機紀錄">
      <div class="card-heading"><div><h2>本頁模擬下機紀錄</h2><span>Not persisted</span></div><b>{{ returnLogs.length }} 筆</b></div>
      <div class="table-scroll">
        <table>
          <thead><tr><th>配件</th><th>設備</th><th>下機時間</th><th>操作人</th><th>生產使用紀錄</th></tr></thead>
          <tbody><tr v-for="(row, index) in returnLogs" :key="`${row.accessoryNo}-${index}`"><td>{{ row.accessoryNo }} / {{ row.version }}</td><td>{{ row.equipmentNo }}</td><td>{{ row.returnedAt }}</td><td>{{ row.userNo }}</td><td>{{ row.productionNote }}</td></tr></tbody>
        </table>
      </div>
    </section>

    <div v-if="lookupTypeOpen || lookupAccessoryOpen" class="lookup-backdrop" @click.self="closeLookup">
      <section class="lookup-dialog" role="dialog" aria-modal="true" :aria-label="lookupTypeOpen ? '選擇配件類別' : '選擇已掛載配件'">
        <header><div><strong>{{ lookupTypeOpen ? '選擇配件類別' : '選擇已掛載配件' }}</strong><span>{{ lookupTypeOpen ? '只列出目前有掛載的類別' : '依配件類別與設備列出目前掛載狀態' }}</span></div><button type="button" aria-label="關閉查找視窗" @click="closeLookup">×</button></header>
        <input v-if="lookupAccessoryOpen" v-model="lookupText" class="lookup-search" type="search" placeholder="搜尋配件編號、類別或設備" />
        <div v-if="lookupTypeOpen" class="type-options">
          <button v-for="type in distinctTypes" :key="type" type="button" @click="chooseType(type)">{{ type }}<span>{{ mountedAccessories.filter((row) => row.accessoryType === type).length }} 件</span></button>
          <p v-if="!distinctTypes.length" class="empty-row">沒有可選的配件類別。</p>
        </div>
        <div v-else class="table-scroll lookup-table">
          <table>
            <thead><tr><th>配件編號</th><th>類別</th><th>版本</th><th>設備類別</th><th>設備編號</th></tr></thead>
            <tbody>
              <tr v-for="row in visibleAccessories" :key="row.accessoryNo" tabindex="0" @click="chooseAccessory(row)" @keydown.enter="chooseAccessory(row)"><td>{{ row.accessoryNo }}</td><td>{{ row.accessoryType }}</td><td>{{ row.version }}</td><td>{{ row.equipmentType }}</td><td>{{ row.equipmentNo }}</td></tr>
              <tr v-if="!visibleAccessories.length"><td colspan="5" class="empty-row">沒有符合條件的掛載配件。</td></tr>
            </tbody>
          </table>
        </div>
        <footer><button type="button" class="secondary-button" @click="closeLookup">Cancel</button></footer>
      </section>
    </div>
  </section>
  <section v-else class="empty-state" aria-label="DEV 假資料未啟用">DEV 預覽未啟用。</section>
</template>

<style scoped>
.return-accessory-page { min-height: 0; height: 100%; overflow: auto; padding: 16px; box-sizing: border-box; background: #f5f4ef; color: #26251f; font: inherit; }
.page-header,.card-heading,.form-actions,.lookup-dialog header,.lookup-dialog footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.page-header { margin: 0 auto 12px; max-width: 1120px; }
.eyebrow,.card-heading span,.lookup-dialog header span { color: #777366; font-size: 12px; }
h1 { margin: 3px 0 0; font-size: 22px; } h1 span { margin-left: 8px; color: #777366; font-size: 13px; font-weight: 500; }
.close-button,.lookup-dialog header button { width: 32px; height: 32px; border: 1px solid #c9c6b9; border-radius: 4px; background: white; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner,.selection-card,.mounted-card,.history-card { max-width: 1120px; margin: 0 auto 12px; border: 1px solid #d9d6ca; border-radius: 6px; background: white; }
.fixture-banner { display: flex; gap: 10px; align-items: center; padding: 9px 12px; background: #fff9df; color: #655612; font-size: 13px; }
.fixture-banner strong { padding: 2px 7px; border-radius: 10px; background: #efdb87; font-size: 11px; }
.selection-card { padding: 14px; }
.card-title { margin-bottom: 12px; font-size: 15px; font-weight: 700; }
.form-grid { display: grid; grid-template-columns: 150px minmax(180px, 1fr) 110px minmax(180px, 1fr); gap: 9px 12px; align-items: center; max-width: 850px; }
.form-grid label { text-align: right; color: #565349; font-size: 13px; }
.form-grid input,.lookup-search { width: 100%; min-height: 32px; box-sizing: border-box; border: 1px solid #c9c6b9; border-radius: 3px; padding: 5px 8px; background: #f8f7f2; color: #292821; font: inherit; }
.lookup-field { display: flex; gap: 5px; min-width: 0; }
.lookup-field input { flex: 1; min-width: 0; }
.lookup-field button { width: 34px; border: 1px solid #aaa696; border-radius: 3px; background: #f4f2e9; font: inherit; cursor: pointer; }
.mount-summary { display: grid; grid-template-columns: repeat(4, minmax(120px, 1fr)); gap: 10px; margin: 14px 0 0; padding: 10px; border: 1px solid #e2dfd4; border-radius: 4px; background: #fbfaf6; }
.mount-summary div { display: grid; gap: 4px; min-width: 0; }
.mount-summary span { color: #777366; font-size: 11px; }
.mount-summary strong { overflow-wrap: anywhere; font-size: 12px; font-weight: 600; }
.form-actions { justify-content: flex-end; margin-top: 14px; }
.primary-button,.secondary-button { min-height: 31px; padding: 5px 13px; border: 1px solid #3b3930; border-radius: 3px; font: inherit; cursor: pointer; }
.primary-button { background: #3b3930; color: white; } .primary-button:disabled { opacity: .45; cursor: not-allowed; }
.secondary-button { background: white; color: #333; }
.feedback { margin: 10px 0 0; padding: 8px 10px; border-radius: 3px; font-size: 13px; }
.feedback-info { background: #edf3f6; color: #315163; } .feedback-warning { background: #fff3d6; color: #765a12; } .feedback-error { background: #fde9e7; color: #8c2721; } .feedback-success { background: #e6f2e9; color: #27633a; }
.mounted-card,.history-card { padding: 13px 14px; }
.card-heading { margin-bottom: 9px; } .card-heading h2 { display: inline; margin: 0 8px 0 0; font-size: 15px; } .card-heading b { color: #625f55; font-size: 12px; font-weight: 500; }
.table-scroll { overflow: auto; }
table { width: 100%; border-collapse: collapse; white-space: nowrap; font-size: 12px; }
th,td { padding: 7px 9px; border-bottom: 1px solid #e7e4dc; text-align: left; }
th { background: #f2f0e8; color: #565349; font-weight: 600; }
tbody tr { cursor: pointer; } tbody tr:hover,tbody tr.selected { background: #fff6c9; }
.empty-row { color: #777366; text-align: center; cursor: default; }
.lookup-backdrop { position: fixed; inset: 0; z-index: 40; display: grid; place-items: center; padding: 16px; background: rgb(25 24 20 / 40%); }
.lookup-dialog { width: min(760px, 100%); max-height: min(620px, 90vh); display: flex; flex-direction: column; gap: 12px; padding: 14px; border-radius: 7px; background: white; box-shadow: 0 16px 48px rgb(0 0 0 / 24%); }
.lookup-dialog header { flex: 0 0 auto; } .lookup-dialog header div { display: grid; gap: 3px; }
.lookup-search { flex: 0 0 auto; background: white; }
.type-options,.lookup-table { overflow: auto; }
.type-options { display: grid; gap: 6px; }
.type-options button { display: flex; justify-content: space-between; padding: 10px; border: 1px solid #ddd9ce; border-radius: 4px; background: #fbfaf6; text-align: left; font: inherit; cursor: pointer; }
.type-options button:hover { border-color: #a28b28; background: #fff9df; }
.type-options span { color: #777366; font-size: 12px; }
.lookup-dialog footer { justify-content: flex-end; }
.empty-state { padding: 16px; color: #777366; }
button:focus-visible,tr:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) {
  .return-accessory-page { padding: 10px; }
  .form-grid { grid-template-columns: 1fr; gap: 5px; }
  .form-grid label { text-align: left; margin-top: 5px; }
  .mount-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  h1 span { display: block; margin: 2px 0 0; }
}
</style>
