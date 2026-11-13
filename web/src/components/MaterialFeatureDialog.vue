<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Feature = 'property' | 'qc-item' | 'import' | 'gum-hour' | 'vendor-map'
type Material = Record<string, string | number | boolean | null>
type Row = Record<string, string | number | boolean | null>

const props = defineProps<{
  feature: Feature
  material: Material | null
  mode?: 'add' | 'edit'
  vendor?: Row | null
}>()
const emit = defineEmits<{ close: []; saved: [] }>()
const materialNo = computed(() => String(props.material?.materialNo ?? ''))
const titleByFeature: Record<Feature, string> = {
  property: '物料屬性設定', 'qc-item': '物料 QC Item 設定', import: '物料資料匯入',
  'gum-hour': '物料 GumHour 設定', 'vendor-map': '物料供應商對應',
}
const title = computed(() => titleByFeature[props.feature])
const lookupTitle = (field: string) => ({ materialType: '物料類別', unitType: '單位類別', unitNo: '單位編號' }[field] ?? '系統選項')
const controlledLabel = (value: string | number | boolean | null) => ({ 0: '一般', 1: '必填', 2: '提示' } as Record<number, string>)[Number(value)] ?? '一般'
const notice = ref('')
const error = ref('')

function readArray(key: string, fallback: Row[] = []): Row[] {
  try {
    const raw = window.localStorage.getItem(key)
    if (raw !== null) return JSON.parse(raw) as Row[]
    window.localStorage.setItem(key, JSON.stringify(fallback))
  } catch { /* in-memory preview still works when storage is unavailable */ }
  return fallback.map((row) => ({ ...row }))
}
function saveArray(key: string, value: Row[]): void {
  window.localStorage.setItem(key, JSON.stringify(value))
}
function scopedKey(kind: string): string {
  return `ymes:dev:material-${kind}:${encodeURIComponent(materialNo.value)}:v1`
}

const propertyRows = ref<Row[]>([])
const selectedProperty = ref<Row | null>(null)
const propertyEditorOpen = ref(false)
const propertyMode = ref<'add' | 'edit' | 'copy'>('add')
const propertyDraft = ref<Row>({ propertyNo: '', value: '', sequence: 10, controlled: 0 })

const qcTab = ref(0)
const selectedAvailableQC = ref<string[]>([])
const selectedAssignedQC = ref<string[]>([])
const assignedQC = ref<Row[]>([])
const availableQC: Row[] = [
  { qcItemNo: 'DEV-ATTR-001', qcItemName: '物料外觀（DEV）', qcItemType: 0 },
  { qcItemNo: 'DEV-ATTR-002', qcItemName: '包裝檢查（DEV）', qcItemType: 0 },
  { qcItemNo: 'DEV-VAR-001', qcItemName: '物料尺寸（DEV）', qcItemType: 1 },
  { qcItemNo: 'DEV-VAR-002', qcItemName: '黏度（DEV）', qcItemType: 1 },
  { qcItemNo: 'DEV-GROUP-001', qcItemName: '進料檢驗組（DEV）', qcItemType: 2 },
]
const availableForTab = computed(() => availableQC.filter((row) => Number(row.qcItemType) === qcTab.value
  && !assignedQC.value.some((assigned) => assigned.qcItemNo === row.qcItemNo)))
const assignedForTab = computed(() => assignedQC.value.filter((row) => Number(row.qcItemType) === qcTab.value))

const vendorCatalog = [
  { vendorNo: 'V-DEV-01', vendorName: '示範供應商（DEV）' },
  { vendorNo: 'V-DEV-02', vendorName: '測試供應商（DEV）' },
  { vendorNo: 'V-DEV-03', vendorName: '包材供應商（DEV）' },
]
const vendorDraft = ref({ vendorNo: '', description: '' })

const importRows = ref<Array<{ materialNo: string; materialName: string; materialType: string; unitNo: string; valid: boolean; message: string }>>([])
const importFileName = ref('')
const importNotice = ref('')

const gumDraft = ref({ accMin: 0, accMax: 0, refrozenFlag: false, frozenMin: 0, reForzenTimes: 0, reacclimateFlag: false, reAccMin: 0, reAccMax: 0 })

function initialize(): void {
  notice.value = ''; error.value = ''; importNotice.value = ''
  propertyEditorOpen.value = false
  if (props.feature === 'property' && materialNo.value) {
    propertyRows.value = readArray(scopedKey('properties'), [
      { propertyNo: 'KEEP_TEMP', value: '25', sequence: 10, controlled: 0, issueState: 0 },
      { propertyNo: 'SHELF_LIFE_HR', value: '', sequence: 20, controlled: 0, issueState: 0 },
    ])
  }
  if (props.feature === 'qc-item' && materialNo.value) assignedQC.value = readArray(scopedKey('qc-items'))
  if (props.feature === 'vendor-map' && materialNo.value) {
    const current = props.vendor
    vendorDraft.value = current
      ? { vendorNo: String(current.vendorNo ?? ''), description: String(current.description ?? '') }
      : { vendorNo: '', description: '' }
  }
  if (props.feature === 'gum-hour' && materialNo.value) {
    const stored = window.localStorage.getItem(scopedKey('gum-hour'))
    if (stored) {
      try { gumDraft.value = { ...gumDraft.value, ...JSON.parse(stored) } } catch { /* use empty DEV defaults */ }
    }
  }
}
watch(() => [props.feature, materialNo.value, props.mode, props.vendor], initialize, { immediate: true })

function openPropertyEditor(mode: 'add' | 'edit' | 'copy'): void {
  error.value = ''
  propertyMode.value = mode
  const source = selectedProperty.value ?? {}
  propertyDraft.value = mode === 'add'
    ? { propertyNo: '', value: '', sequence: (propertyRows.value.length + 1) * 10, controlled: 0 }
    : { ...source, propertyNo: mode === 'copy' ? '' : String(source.propertyNo ?? ''), issueState: 0 }
  propertyEditorOpen.value = true
}
function saveProperty(): void {
  const propertyNo = String(propertyDraft.value.propertyNo ?? '').trim().toUpperCase()
  if (!propertyNo) { error.value = '屬性編號為必填欄位。'; return }
  const duplicate = propertyRows.value.some((row) => String(row.propertyNo).toUpperCase() === propertyNo
    && (propertyMode.value !== 'edit' || row.propertyNo !== selectedProperty.value?.propertyNo))
  if (duplicate) { error.value = `屬性 ${propertyNo} 已存在。`; return }
  const nextRow = { ...propertyDraft.value, propertyNo, issueState: 0 }
  propertyRows.value = propertyMode.value === 'edit'
    ? propertyRows.value.map((row) => row.propertyNo === selectedProperty.value?.propertyNo ? nextRow : row)
    : [...propertyRows.value, nextRow]
  saveArray(scopedKey('properties'), propertyRows.value)
  selectedProperty.value = nextRow
  propertyEditorOpen.value = false
  notice.value = '屬性 DEV 假資料已儲存。'
}
function deleteProperty(): void {
  if (!selectedProperty.value || !window.confirm(`確定刪除屬性 ${String(selectedProperty.value.propertyNo)}？`)) return
  propertyRows.value = propertyRows.value.filter((row) => row.propertyNo !== selectedProperty.value?.propertyNo)
  saveArray(scopedKey('properties'), propertyRows.value)
  selectedProperty.value = null
}
function approveProperty(): void {
  if (!selectedProperty.value || Number(selectedProperty.value.issueState ?? 0) !== 0) return
  propertyRows.value = propertyRows.value.map((row) => row.propertyNo === selectedProperty.value?.propertyNo ? { ...row, issueState: 2 } : row)
  saveArray(scopedKey('properties'), propertyRows.value)
  selectedProperty.value = propertyRows.value.find((row) => row.propertyNo === selectedProperty.value?.propertyNo) ?? null
  notice.value = '屬性 DEV 假資料已核准。'
}

function addQCItems(): void {
  const additions = availableForTab.value.filter((row) => selectedAvailableQC.value.includes(String(row.qcItemNo)))
  assignedQC.value = [...assignedQC.value, ...additions.map((row) => ({ ...row }))]
  selectedAvailableQC.value = []
}
function removeQCItems(): void {
  assignedQC.value = assignedQC.value.filter((row) => !selectedAssignedQC.value.includes(String(row.qcItemNo)))
  selectedAssignedQC.value = []
}
function saveQCItems(): void {
  saveArray(scopedKey('qc-items'), assignedQC.value)
  notice.value = 'QC Item DEV 假資料已儲存。'
  emit('saved')
}

function saveVendor(): void {
  const vendorNo = vendorDraft.value.vendorNo.trim().toUpperCase()
  if (!materialNo.value || !vendorNo) { error.value = '物料編號與供應商編號為必填欄位。'; return }
  const key = `ymes:dev:material-vendors:${encodeURIComponent(materialNo.value)}:v1`
  const rows = readArray(key)
  const oldVendorNo = String(props.vendor?.vendorNo ?? '')
  if (props.mode === 'edit' && oldVendorNo !== vendorNo) { error.value = '供應商編號是對應鍵，修改時不可變更。'; return }
  if (props.mode !== 'edit' && rows.some((row) => String(row.vendorNo).toUpperCase() === vendorNo)) { error.value = `供應商 ${vendorNo} 已對應此物料。`; return }
  const vendorName = vendorCatalog.find((vendor) => vendor.vendorNo === vendorNo)?.vendorName ?? `${vendorNo}（DEV）`
  const value: Row = { materialNo: materialNo.value, vendorNo, vendorName, description: vendorDraft.value.description.trim() }
  saveArray(key, props.mode === 'edit' ? rows.map((row) => String(row.vendorNo) === oldVendorNo ? value : row) : [...rows, value])
  emit('saved'); emit('close')
}

function parseCsv(text: string): string[][] {
  const rows: string[][] = []
  let row: string[] = [], cell = '', quoted = false
  const source = text.replace(/^\uFEFF/, '')
  for (let i = 0; i < source.length; i++) {
    const char = source[i]
    if (char === '"' && quoted && source[i + 1] === '"') { cell += '"'; i++ }
    else if (char === '"') quoted = !quoted
    else if (char === ',' && !quoted) { row.push(cell.trim()); cell = '' }
    else if ((char === '\n' || char === '\r') && !quoted) {
      if (char === '\r' && source[i + 1] === '\n') i++
      row.push(cell.trim()); if (row.some(Boolean)) rows.push(row); row = []; cell = ''
    } else cell += char
  }
  row.push(cell.trim()); if (row.some(Boolean)) rows.push(row)
  return rows
}
async function previewImport(event: Event): Promise<void> {
  error.value = ''; importNotice.value = ''; importRows.value = []
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  importFileName.value = file.name
  if (!file.name.toLowerCase().endsWith('.csv')) {
    importNotice.value = 'WinForms 支援 Excel 範本；目前簡化版只讀 CSV，請另存為 CSV 後預覽。'
    return
  }
  try {
    const matrix = parseCsv(await file.text())
    if (matrix.length < 2) throw new Error('CSV 至少要有標題列與一筆資料。')
    const headers = matrix[0].map((value) => value.replace(/[\s_]/g, '').toLowerCase())
    const findColumn = (names: string[]) => headers.findIndex((header) => names.includes(header))
    const noIndex = findColumn(['materialno', '物料編號'])
    const nameIndex = findColumn(['materialname', '物料名稱'])
    const typeIndex = findColumn(['materialtype', '物料類別'])
    const unitIndex = findColumn(['unitno', '單位編號'])
    if (noIndex < 0 || nameIndex < 0) throw new Error('CSV 標題必須包含 MaterialNo 與 MaterialName。')
    importRows.value = matrix.slice(1).map((row) => {
      const materialNo = String(row[noIndex] ?? '').trim().toUpperCase()
      const materialName = String(row[nameIndex] ?? '').trim()
      const valid = Boolean(materialNo && materialName)
      return { materialNo, materialName, materialType: String(row[typeIndex] ?? 'CHEM').trim() || 'CHEM', unitNo: String(row[unitIndex] ?? 'EA').trim() || 'EA', valid, message: valid ? '待匯入' : '物料編號與名稱必填' }
    })
    importNotice.value = `解析 ${importRows.value.length} 筆；這是本機預覽。`
  } catch (reason) { error.value = reason instanceof Error ? reason.message : 'CSV 讀取失敗。' }
}
function commitImport(): void {
  const validRows = importRows.value.filter((row) => row.valid)
  if (!validRows.length) { error.value = '沒有可匯入的有效資料。'; return }
  const storageKey = 'ymes:dev:master-data:materials:v1'
  const current = readArray(storageKey)
  const existing = new Set(current.map((row) => String(row.materialNo ?? '').toUpperCase()))
  const additions = validRows.filter((row) => !existing.has(row.materialNo)).map((row): Row => ({
    materialNo: row.materialNo, materialName: row.materialName, materialType: row.materialType,
    materialSpec: '', description: `CSV 匯入（DEV）`, putInPlace: 0, countWay: 0, checkLotNo: 0,
    unitType: '數量', unitNo: row.unitNo, safeQty: 0, keyMaterials: false,
    openLimitTimeFlag: false, openLimitTime: 0, resetOpenDate: false, expireDateFlag: false,
    erpNo: '', issueState: 0, dataStamp: 0,
  }))
  saveArray(storageKey, [...current, ...additions])
  importNotice.value = `匯入 ${additions.length} 筆；略過 ${validRows.length - additions.length} 筆重複編號。`
  emit('saved'); emit('close')
}

function validateGum(): boolean {
  const numberFields: Array<[keyof typeof gumDraft.value, string]> = [['accMin','ACCMin'],['accMax','ACCMax']]
  if (gumDraft.value.refrozenFlag) numberFields.push(['frozenMin','FROZENMIN'])
  if (gumDraft.value.reacclimateFlag) numberFields.push(['reAccMin','REACCMIN'],['reAccMax','REACCMAX'])
  for (const [key, label] of numberFields) {
    const value = Number(gumDraft.value[key])
    if (!Number.isFinite(value) || value < 0) { error.value = `${label} 必須是大於或等於 0 的數值。`; return false }
  }
  if ((gumDraft.value.accMin !== 0 || gumDraft.value.accMax !== 0) && gumDraft.value.accMax <= gumDraft.value.accMin) { error.value = 'ACCMax 必須大於 ACCMin。'; return false }
  if (gumDraft.value.refrozenFlag && (!Number.isInteger(gumDraft.value.reForzenTimes) || gumDraft.value.reForzenTimes <= 0)) { error.value = '啟用再冷凍時，次數必須是大於 0 的整數。'; return false }
  if (gumDraft.value.reacclimateFlag && (gumDraft.value.reAccMin !== 0 || gumDraft.value.reAccMax !== 0) && gumDraft.value.reAccMin >= gumDraft.value.reAccMax) { error.value = 'REACCMIN 必須小於 REACCMAX。'; return false }
  return true
}
function saveGum(): void {
  error.value = ''
  if (!validateGum()) return
  window.localStorage.setItem(scopedKey('gum-hour'), JSON.stringify(gumDraft.value))
  notice.value = 'GumHour DEV 假資料已儲存。'
  emit('saved'); emit('close')
}
</script>

<template>
  <div class="mfd-backdrop" @click.self="emit('close')">
    <section class="mfd-dialog" role="dialog" aria-modal="true" :aria-labelledby="'mfd-title'">
      <header class="mfd-header"><div><small>{{ materialNo || '物料資料' }} · 開發區本機假資料</small><h2 id="mfd-title">{{ title }}</h2></div><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
      <p v-if="error" class="mfd-message error" role="alert">{{ error }}</p><p v-if="notice" class="mfd-message" role="status">{{ notice }}</p>
      <div class="mfd-body">
        <template v-if="feature==='property'">
          <div class="mfd-toolbar"><button type="button" class="mfd-button primary" @click="openPropertyEditor('add')">新增</button><button type="button" class="mfd-button" :disabled="!selectedProperty" @click="openPropertyEditor('edit')">修改</button><button type="button" class="mfd-button" :disabled="!selectedProperty" @click="openPropertyEditor('copy')">複製</button><button type="button" class="mfd-button" :disabled="!selectedProperty" @click="deleteProperty">刪除</button><button type="button" class="mfd-button" :disabled="!selectedProperty || Number(selectedProperty.issueState)!==0" @click="approveProperty">核准</button></div>
          <div class="mfd-grid"><table><thead><tr><th>屬性編號</th><th>屬性值</th><th>順序</th><th>控制方式（DEV）</th><th>狀態</th></tr></thead><tbody><tr v-for="row in propertyRows" :key="String(row.propertyNo)" :class="{selected:selectedProperty?.propertyNo===row.propertyNo}" @click="selectedProperty=row"><td>{{ row.propertyNo }}</td><td>{{ row.value || '—' }}</td><td>{{ row.sequence }}</td><td>{{ controlledLabel(row.controlled) }}</td><td>{{ Number(row.issueState)===2?'已核准':'未簽核' }}</td></tr><tr v-if="!propertyRows.length"><td colspan="5" class="empty">尚無物料屬性</td></tr></tbody></table></div>
          <p class="mfd-hint">WinForms 的屬性來源包含物料類別已核准屬性與屬性控制檢查；目前以本機 DEV 清單模擬。</p>
          <form v-if="propertyEditorOpen" class="mfd-editor" @submit.prevent="saveProperty"><label>屬性編號<input v-model="propertyDraft.propertyNo" required :readonly="propertyMode==='edit'" /></label><label>屬性值<input v-model="propertyDraft.value" /></label><label>順序<input v-model.number="propertyDraft.sequence" type="number" min="0" /></label><label>控制方式<select v-model.number="propertyDraft.controlled"><option :value="0">一般</option><option :value="1">必填</option><option :value="2">核准前提示</option></select></label><button class="mfd-button primary" type="submit">儲存屬性</button><button class="mfd-button" type="button" @click="propertyEditorOpen=false">取消</button></form>
        </template>

        <template v-else-if="feature==='qc-item'">
          <nav class="mfd-tabs"><button v-for="tab in [{value:0,label:'Attributes'},{value:1,label:'Variables'},{value:2,label:'Group'}]" :key="tab.value" type="button" :class="{active:qcTab===tab.value}" @click="qcTab=tab.value;selectedAvailableQC=[];selectedAssignedQC=[]">{{ tab.label }}</button></nav>
          <div class="mfd-transfer"><fieldset><legend>可選項目（DEV）</legend><select v-model="selectedAvailableQC" multiple><option v-for="row in availableForTab" :key="String(row.qcItemNo)" :value="String(row.qcItemNo)">{{ row.qcItemNo }} · {{ row.qcItemName }}</option></select></fieldset><div class="mfd-transfer-actions"><button class="mfd-button" type="button" :disabled="!selectedAvailableQC.length" @click="addQCItems">加入 →</button><button class="mfd-button" type="button" :disabled="!selectedAssignedQC.length" @click="removeQCItems">← 移除</button></div><fieldset><legend>已選 QC Item</legend><select v-model="selectedAssignedQC" multiple><option v-for="row in assignedForTab" :key="String(row.qcItemNo)" :value="String(row.qcItemNo)">{{ row.qcItemNo }} · {{ row.qcItemName }}</option></select></fieldset></div>
          <p class="mfd-hint">類型代碼依 WinForms：0 Attributes、1 Variables、2 Group。右側清單是本機示範目錄。</p>
        </template>

        <template v-else-if="feature==='vendor-map'">
          <fieldset class="mfd-section"><legend>供應商資料</legend><label>物料編號<input :value="materialNo" readonly /></label><label>供應商編號<select v-model="vendorDraft.vendorNo" required :disabled="mode==='edit'"><option value="">選擇供應商</option><option v-for="vendor in vendorCatalog" :key="vendor.vendorNo" :value="vendor.vendorNo">{{ vendor.vendorNo }} · {{ vendor.vendorName }}</option></select></label><label class="wide">說明<textarea v-model="vendorDraft.description" rows="3" /></label></fieldset>
          <p class="mfd-hint">供應商目錄與對應資料均為本機 DEV 假資料；WinForms 欄位為 MaterialNo、VendorNo、Description。</p>
        </template>

        <template v-else-if="feature==='import'">
          <div class="mfd-import"><label>選擇檔案<input type="file" accept=".csv,.xls,.xlsx" @change="previewImport" /></label><span>{{ importFileName || 'WinForms 使用 Excel 範本；DEV 簡化版可預覽 CSV' }}</span></div>
          <p v-if="importNotice" class="mfd-hint">{{ importNotice }}</p>
          <div class="mfd-grid"><table><thead><tr><th>物料編號</th><th>物料名稱</th><th>類別</th><th>單位編號</th><th>檢查結果</th></tr></thead><tbody><tr v-for="(row,index) in importRows" :key="index"><td>{{ row.materialNo }}</td><td>{{ row.materialName }}</td><td>{{ row.materialType }}</td><td>{{ row.unitNo }}</td><td>{{ row.message }}</td></tr><tr v-if="!importRows.length"><td colspan="5" class="empty">選擇 CSV 後顯示匯入預覽</td></tr></tbody></table></div>
          <p class="mfd-hint">CSV 欄位需有 MaterialNo、MaterialName，可選 MaterialType、UnitNo。WinForms 的 .xls/.xlsx、範本下載與後台逐筆驗證尚未重現。</p>
        </template>

        <template v-else>
          <fieldset class="mfd-section"><legend>MTL Gum Data</legend><div class="mfd-form-grid">
            <label>MaterialType<input :value="material?.materialType" readonly /></label><label>MaterialNo<input :value="materialNo" readonly /></label>
            <label>ACCMin（Hour）<input v-model.number="gumDraft.accMin" type="number" min="0" step="0.01" /></label><label>ACCMax（Hour）<input v-model.number="gumDraft.accMax" type="number" min="0" step="0.01" /></label>
            <label class="check"><input v-model="gumDraft.refrozenFlag" type="checkbox" />REFROZENFLAG</label><label>FROZENMIN（Hour）<input v-model.number="gumDraft.frozenMin" type="number" min="0" step="0.01" :disabled="!gumDraft.refrozenFlag" /></label><label>ReForzenTimes<input v-model.number="gumDraft.reForzenTimes" type="number" min="0" step="1" :disabled="!gumDraft.refrozenFlag" /></label>
            <label class="check"><input v-model="gumDraft.reacclimateFlag" type="checkbox" />REACCLIMATEFLAG</label><label>REACCMIN（Hour）<input v-model.number="gumDraft.reAccMin" type="number" min="0" step="0.01" :disabled="!gumDraft.reacclimateFlag" /></label><label>REACCMAX（Hour）<input v-model.number="gumDraft.reAccMax" type="number" min="0" step="0.01" :disabled="!gumDraft.reacclimateFlag" /></label>
          </div></fieldset>
          <p class="mfd-hint">欄位與 ACC / 再冷凍 / 再回溫區間檢查依 frmMaterialGumType；儲存只更新本機 DEV 假資料。</p>
        </template>
      </div>
      <footer>
        <button v-if="feature==='qc-item'" class="mfd-button primary" type="button" @click="saveQCItems">確定</button>
        <button v-else-if="feature==='vendor-map'" class="mfd-button primary" type="button" @click="saveVendor">確定</button>
        <button v-else-if="feature==='import'" class="mfd-button primary" type="button" :disabled="!importRows.some((row)=>row.valid)" @click="commitImport">匯入</button>
        <button v-else-if="feature==='gum-hour'" class="mfd-button primary" type="button" @click="saveGum">確定</button>
        <button class="mfd-button" type="button" @click="emit('close')">關閉</button>
      </footer>
    </section>
  </div>
</template>

<style scoped>
.mfd-backdrop{position:fixed;inset:0;z-index:1200;display:grid;place-items:center;padding:16px;background:rgb(25 24 20 / 38%)}
.mfd-dialog{display:flex;flex-direction:column;width:min(980px,96vw);max-height:92vh;border:1px solid #aaa696;background:#fff;color:#292820;box-shadow:0 12px 40px rgb(0 0 0 / 26%);font:12px 'Segoe UI','Microsoft JhengHei',sans-serif}
.mfd-header{display:flex;align-items:center;justify-content:space-between;gap:14px;padding:12px 16px;border-bottom:1px solid #d5d1c6;background:#faf9f5}.mfd-header small{color:#716d61}.mfd-header h2{margin:4px 0 0;font-size:18px}.mfd-header>button{width:30px;height:30px;border:1px solid #c9c4b7;background:#fff;font-size:20px;color:inherit}
.mfd-body{min-height:120px;overflow:auto;padding:12px 16px}.mfd-dialog footer{display:flex;justify-content:flex-end;gap:8px;padding:10px 16px;border-top:1px solid #d5d1c6;background:#faf9f5}
.mfd-button{min-height:29px;padding:5px 10px;border:1px solid #c9c4b7;border-radius:3px;background:#fff;color:inherit;font:inherit}.mfd-button:hover:not(:disabled){border-color:#a58d00;background:#fffbea}.mfd-button:disabled{opacity:.5;cursor:not-allowed}.mfd-button.primary{border-color:#3b3930;background:#3b3930;color:#fff}
.mfd-toolbar{display:flex;gap:6px;flex-wrap:wrap;margin-bottom:8px}.mfd-grid{max-height:42vh;overflow:auto;border:1px solid #d3cfc3}.mfd-dialog table{width:100%;border-collapse:collapse;white-space:nowrap}.mfd-dialog th,.mfd-dialog td{padding:6px 8px;border:1px solid #d5d1c6;text-align:left}.mfd-dialog th{position:sticky;top:0;background:#f1ead0}.mfd-dialog tbody tr{cursor:pointer}.mfd-dialog tbody tr.selected,.mfd-dialog tbody tr:hover{background:#fff3c2}.mfd-dialog .empty{color:#777;text-align:center}
.mfd-message{margin:8px 16px 0;padding:7px 10px;border:1px solid #bfc7b7;background:#f4f7ef;color:#3e5033}.mfd-message.error{border-color:#d6aaa3;background:#fff4f1;color:#8a3329}.mfd-hint{margin:9px 0 0;color:#716d61;font-size:11px}
.mfd-tabs{display:flex;gap:4px;margin-bottom:10px;border-bottom:1px solid #d5d1c6}.mfd-tabs button{padding:7px 12px;border:1px solid transparent;border-bottom:0;background:transparent;color:inherit;font:inherit}.mfd-tabs button.active{border-color:#d5d1c6;background:#fff3c2}
.mfd-transfer{display:grid;grid-template-columns:minmax(0,1fr) auto minmax(0,1fr);gap:12px;align-items:center}.mfd-transfer fieldset,.mfd-section{min-width:0;margin:0;padding:10px;border:1px solid #c9c4b7}.mfd-transfer legend,.mfd-section legend{padding:0 5px;color:#5d594e}.mfd-transfer select{width:100%;height:230px;border:1px solid #aaa58f;background:#fff}.mfd-transfer-actions{display:grid;gap:7px}
.mfd-section{display:grid;grid-template-columns:1fr 1fr;gap:10px 16px}.mfd-section>label,.mfd-form-grid>label,.mfd-editor>label{display:grid;gap:5px}.mfd-section .wide{grid-column:1/-1}.mfd-dialog input:not([type=checkbox]),.mfd-dialog select,.mfd-dialog textarea{width:100%;min-height:29px;padding:5px 7px;border:1px solid #aaa58f;background:#fff;color:inherit;font:inherit}.mfd-dialog input:focus,.mfd-dialog select:focus,.mfd-dialog textarea:focus{outline:2px solid rgb(181 154 0 / 22%);border-color:#a58d00}.mfd-dialog input[readonly]{background:#f3f2ee}
.mfd-editor{display:grid;grid-template-columns:repeat(4,minmax(120px,1fr));gap:9px;align-items:end;margin-top:12px;padding:10px;border:1px solid #d5d1c6;background:#faf9f5}.mfd-editor button{align-self:end}.mfd-import{display:flex;align-items:center;gap:14px;margin-bottom:10px}.mfd-import label{display:grid;gap:6px;min-width:260px}.mfd-import span{color:#716d61}.mfd-form-grid{display:grid;grid-template-columns:repeat(3,minmax(150px,1fr));gap:12px}.mfd-form-grid .check{display:flex;align-items:center;gap:7px}.mfd-form-grid input[type=checkbox]{accent-color:#a58d00}.mfd-form-grid input:disabled{background:#f3f2ee}.mfd-lookup-list{display:grid;gap:6px}.mfd-lookup-dialog{width:min(520px,94vw)}
@media(max-width:700px){.mfd-transfer{grid-template-columns:1fr}.mfd-transfer select{height:120px}.mfd-transfer-actions{grid-template-columns:1fr 1fr}.mfd-section,.mfd-form-grid,.mfd-editor{grid-template-columns:1fr}.mfd-import{align-items:flex-start;flex-direction:column}}
</style>
