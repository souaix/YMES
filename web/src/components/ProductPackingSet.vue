<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type PackSettings = { boxQty: number; cartonQty: number; palletQty: number; packOiPath: string }
type Audit = { creator: string; createDate: string; reviser: string; reviseDate: string; dataStamp: number }
type PackAccessory = Audit & { barcode: string; description: string }
type RuleMapping = Audit & { ruleNo: string; ruleVersion: string }
type PackProduct = { key: string; productNo: string; productVersion: string; productName: string; issueState: number; settings: PackSettings }
type PackingRelation = { packingAccessories?: PackAccessory[]; packingRuleMap?: RuleMapping | null }
type PackingChange = PackingRelation & { key: string }
type CopyAction = 'settings' | 'accessories'

const props = defineProps<{
  product: PackProduct
  products: PackProduct[]
  relations: Record<string, PackingRelation>
  currentUser: string
  developmentPreview: boolean
  saveSettings: (keys: string[], settings: PackSettings) => boolean
  saveRelations: (changes: PackingChange[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()

const mode = ref<'0' | '1'>('0')
const boxQty = ref('')
const cartonQty = ref('')
const palletQty = ref('')
const packOiPath = ref('')
const error = ref('')
const notice = ref('')
const accessorySelection = ref<string[]>([])
const accessoryDraft = ref<{ previous: PackAccessory | null; barcode: string; description: string } | null>(null)
const copyAction = ref<CopyAction | null>(null)
const copyKeys = ref<string[]>([])
const mapSelection = ref<string[]>([])
const mapDraft = ref<{ action: 'add' | 'edit' | 'copy'; key: string; ruleNo: string; ruleVersion: string; current: boolean } | null>(null)
const localRules = [
  { no: 'DEV-PACK-STD', versions: ['01', '02'] },
  { no: 'DEV-PACK-EXP', versions: ['01'] },
]
const productsInUse = new Set(['DEV-PRD-1002|02'])
const canEditLegacy = computed(() => props.developmentPreview && props.product.issueState === 0)
const accessories = computed(() => props.relations[props.product.key]?.packingAccessories ?? [])
const mappedProducts = computed(() => props.products.flatMap(product => {
  const mapping = props.relations[product.key]?.packingRuleMap
  return mapping ? [{ ...product, mapping }] : []
}))
const selectedMapping = computed(() => mappedProducts.value.find(row => row.key === mapSelection.value[0]))
const selectedAccessory = computed(() => accessories.value.find(row => row.barcode === accessorySelection.value[0]))
const ruleVersions = computed(() => localRules.find(rule => rule.no === mapDraft.value?.ruleNo)?.versions ?? [])

watch(() => props.product.settings, settings => {
  boxQty.value = String(settings.boxQty)
  cartonQty.value = String(settings.cartonQty)
  palletQty.value = String(settings.palletQty)
  packOiPath.value = settings.packOiPath
}, { immediate: true })

function finish(message: string, saved: boolean): boolean {
  error.value = saved ? '' : '包裝假資料儲存失敗。'
  notice.value = saved ? message : ''
  return saved
}

function audit(old?: Audit | null): Audit {
  const now = new Date().toISOString()
  return old
    ? { creator: old.creator, createDate: old.createDate, reviser: props.currentUser, reviseDate: now, dataStamp: old.dataStamp + 1 }
    : { creator: props.currentUser, createDate: now, reviser: '', reviseDate: '', dataStamp: 0 }
}

function savePackSettings(): void {
  if (!canEditLegacy.value) return
  const box = Number(boxQty.value)
  const carton = Number(cartonQty.value)
  const pallet = Number(palletQty.value)
  if (!/^\d{1,4}(?:\.\d{1,4})?$/.test(boxQty.value.trim()) || box <= 0) {
    error.value = 'BoxQty 必須大於 0，整數和小數各最多四位。'
    return
  }
  if (![cartonQty.value, palletQty.value].every(value => /^\d{1,8}$/.test(value.trim()))) {
    error.value = 'CartonQty 與 PalletQty 必須是 0 至 99999999 的整數。'
    return
  }
  if (carton === 0 && pallet !== 0) {
    error.value = 'CartonQty 為 0 時，PalletQty 必須為 0。'
    return
  }
  finish('Pack Setting 假資料已儲存。', props.saveSettings([props.product.key], {
    boxQty: box, cartonQty: carton, palletQty: pallet, packOiPath: packOiPath.value.trim() || props.product.settings.packOiPath,
  }))
}

function openAccessory(row: PackAccessory | null): void {
  accessoryDraft.value = { previous: row, barcode: row?.barcode ?? '', description: row?.description ?? '' }
  error.value = ''
}

function saveAccessory(): void {
  const draft = accessoryDraft.value
  if (!canEditLegacy.value || !draft) return
  const barcode = draft.barcode.trim().toUpperCase()
  if (!barcode || barcode.length > 50 || draft.description.length > 255) {
    error.value = '請檢查 AccessoryBarCode（1–50 碼）和 Description（最多 255 字）。'
    return
  }
  if (!draft.previous && accessories.value.some(row => row.barcode === barcode)) {
    error.value = 'AccessoryBarCode 已存在。'
    return
  }
  const next = [...accessories.value.filter(row => row.barcode !== barcode), { barcode, description: draft.description.trim(), ...audit(draft.previous) }]
  if (finish('包裝配件假資料已儲存。', props.saveRelations([{ key: props.product.key, packingAccessories: next }]))) {
    accessoryDraft.value = null
    accessorySelection.value = []
  }
}

function deleteAccessories(): void {
  if (!canEditLegacy.value || !accessorySelection.value.length || !window.confirm('確定移除選取的包裝配件？')) return
  const next = accessories.value.filter(row => !accessorySelection.value.includes(row.barcode))
  if (finish('已移除選取的包裝配件。', props.saveRelations([{ key: props.product.key, packingAccessories: next }]))) accessorySelection.value = []
}

function openCopy(action: CopyAction): void {
  copyAction.value = action
  copyKeys.value = []
  error.value = ''
}

function copyPacking(): void {
  if (!canEditLegacy.value || !copyAction.value || !copyKeys.value.length) return
  const saved = copyAction.value === 'settings'
    ? props.saveSettings(copyKeys.value, props.product.settings)
    : props.saveRelations(copyKeys.value.map(key => ({
      key, packingAccessories: accessories.value.map(({ barcode, description }) => ({ barcode, description, ...audit() })),
    })))
  if (finish('已將包裝假資料複製到選取的產品版次。', saved)) copyAction.value = null
}

function openMap(action: 'add' | 'edit' | 'copy'): void {
  const source = action === 'add' ? undefined : selectedMapping.value
  if (action !== 'add' && !source) return
  mapDraft.value = {
    action, key: source?.key ?? props.product.key, ruleNo: source?.mapping.ruleNo ?? '',
    ruleVersion: source?.mapping.ruleVersion === '*' ? '' : source?.mapping.ruleVersion ?? '',
    current: !source || source.mapping.ruleVersion === '*',
  }
  error.value = ''
}

function saveMap(closeAfter: boolean): void {
  const draft = mapDraft.value
  if (!props.developmentPreview || !draft) return
  if (!draft.key || !localRules.some(rule => rule.no === draft.ruleNo) || (!draft.current && !ruleVersions.value.includes(draft.ruleVersion))) {
    error.value = '請選擇產品版次、包裝規則及指定版次。'
    return
  }
  if (productsInUse.has(draft.key)) {
    error.value = '此 DEV 產品的包裝規則正在使用中。'
    return
  }
  const old = props.relations[draft.key]?.packingRuleMap
  if (draft.action !== 'edit' && old) {
    error.value = '此產品版次已設定包裝規則。'
    return
  }
  const mapping = { ruleNo: draft.ruleNo, ruleVersion: draft.current ? '*' : draft.ruleVersion, ...audit(old) }
  if (!finish('包裝規則對應假資料已儲存。', props.saveRelations([{ key: draft.key, packingRuleMap: mapping }]))) return
  mapSelection.value = [draft.key]
  if (closeAfter) mapDraft.value = null
  else draft.action = 'edit'
}

function deleteMappings(): void {
  if (!mapSelection.value.length || !window.confirm('確定刪除選取的包裝規則對應？')) return
  if (mapSelection.value.some(key => productsInUse.has(key))) {
    error.value = '選取的 DEV 產品中有工單正在使用包裝規則。'
    return
  }
  if (finish('已移除選取的包裝規則對應。', props.saveRelations(mapSelection.value.map(key => ({ key, packingRuleMap: null }))))) mapSelection.value = []
}

function displayDate(value: string): string {
  return value ? new Date(value).toLocaleString('sv-SE') : ''
}
</script>

<template>
  <section class="pack-window" role="dialog" aria-modal="true" aria-labelledby="pack-title">
    <header class="pack-bar">
      <h2 id="pack-title">{{ mode === '1' ? 'Product PackingRule Map' : 'Packing Set' }}</h2>
      <span v-if="developmentPreview" class="pack-muted">DEV 假資料</span>
      <button class="pack-close" type="button" aria-label="關閉 Packing Set" @click="emit('close')">×</button>
    </header>
    <p v-if="!developmentPreview" class="pack-message">正式 Packing Set API 尚未接線。</p>
    <template v-else>
      <div class="pack-bar">
        <span>{{ product.productNo }} / {{ product.productVersion }}　{{ product.productName }}</span>
        <label class="pack-close">PackOperationMode
          <select v-model="mode" @change="error = ''; notice = ''; accessoryDraft = null; copyAction = null; mapDraft = null">
            <option value="0">0 — 三階包裝</option><option value="1">1 — 多階包裝規則</option>
          </select>
        </label>
      </div>
      <div v-if="mode === '0'" class="pack-body">
        <form class="pack-card" @submit.prevent="savePackSettings">
          <h3>Pack Setting</h3>
          <div class="pack-fields">
            <label>BoxQty <input v-model="boxQty" inputmode="decimal" :readonly="!canEditLegacy"></label>
            <label>CartonQty <input v-model="cartonQty" inputmode="numeric" :readonly="!canEditLegacy"></label>
            <label>PalletQty <input v-model="palletQty" inputmode="numeric" :readonly="!canEditLegacy"></label>
            <label class="pack-wide">PackOIPath <input v-model="packOiPath" maxlength="500" :readonly="!canEditLegacy"></label>
          </div>
          <div class="pack-actions"><button type="submit" :disabled="!canEditLegacy">Set</button><button type="button" :disabled="!canEditLegacy" @click="openCopy('settings')">Copy</button></div>
        </form>
        <div class="pack-card">
          <h3>Packing Accessory</h3>
          <div class="pack-actions">
            <button type="button" :disabled="!canEditLegacy" @click="openAccessory(null)">Add</button>
            <button type="button" :disabled="!canEditLegacy || !selectedAccessory" @click="openAccessory(selectedAccessory ?? null)">Edit</button>
            <button type="button" :disabled="!canEditLegacy || !accessorySelection.length" @click="deleteAccessories">Delete</button>
            <button type="button" :disabled="!canEditLegacy || !accessories.length" @click="openCopy('accessories')">Copy</button>
          </div>
          <div class="pack-table"><table>
            <thead><tr><th scope="col">選取</th><th scope="col">AccessoryBarCode</th><th scope="col">Description</th><th scope="col">Creator</th><th scope="col">CreateDate</th><th scope="col">Reviser</th><th scope="col">ReviseDate</th></tr></thead>
            <tbody>
              <tr v-for="item in accessories" :key="item.barcode">
                <td><input v-model="accessorySelection" type="checkbox" :value="item.barcode" :aria-label="'選取 ' + item.barcode"></td>
                <td>{{ item.barcode }}</td><td>{{ item.description }}</td><td>{{ item.creator }}</td><td>{{ displayDate(item.createDate) }}</td><td>{{ item.reviser }}</td><td>{{ displayDate(item.reviseDate) }}</td>
              </tr>
              <tr v-if="!accessories.length"><td colspan="7" class="pack-muted">目前沒有產品包裝配件。</td></tr>
            </tbody>
          </table></div>
        </div>
        <p class="pack-muted">包裝序號規則、Label Rule 與標籤預覽仍待接續。</p>
      </div>
      <div v-else class="pack-body">
        <div class="pack-actions">
          <button type="button" @click="openMap('add')">Add</button>
          <button type="button" :disabled="!selectedMapping" @click="openMap('edit')">Edit</button>
          <button type="button" :disabled="!mapSelection.length" @click="deleteMappings">Delete</button>
          <button type="button" :disabled="!selectedMapping" @click="openMap('copy')">Copy</button>
        </div>
        <div class="pack-table"><table>
          <thead><tr><th scope="col">選取</th><th scope="col">ProductNo</th><th scope="col">ProductVersion</th><th scope="col">PackingRuleNo</th><th scope="col">PackingRuleVersion</th>
            <th scope="col">Creator</th><th scope="col">CreateDate</th><th scope="col">Reviser</th><th scope="col">ReviseDate</th></tr></thead>
          <tbody>
            <tr v-for="row in mappedProducts" :key="row.key">
              <td><input v-model="mapSelection" type="checkbox" :value="row.key" :aria-label="'選取 ' + row.productNo + ' / ' + row.productVersion"></td>
              <td>{{ row.productNo }}</td><td>{{ row.productVersion }}</td><td>{{ row.mapping.ruleNo }}</td><td>{{ row.mapping.ruleVersion }}</td>
              <td>{{ row.mapping.creator }}</td><td>{{ displayDate(row.mapping.createDate) }}</td><td>{{ row.mapping.reviser }}</td><td>{{ displayDate(row.mapping.reviseDate) }}</td>
            </tr>
            <tr v-if="!mappedProducts.length"><td colspan="9" class="pack-muted">目前沒有包裝規則對應。</td></tr>
          </tbody>
        </table></div>
      </div>
    </template>
    <p v-if="error && !accessoryDraft && !mapDraft && !copyAction" class="pack-message pack-error" role="alert">{{ error }}</p>
    <p v-else-if="notice && !accessoryDraft && !mapDraft && !copyAction" class="pack-message" role="status">{{ notice }}</p>
    <footer class="pack-actions pack-footer"><button type="button" @click="emit('close')">Close</button></footer>

    <div v-if="accessoryDraft || mapDraft || copyAction" class="pack-overlay">
      <form v-if="accessoryDraft" class="pack-dialog" role="dialog" aria-label="Packing Accessory Definition" @submit.prevent="saveAccessory">
        <h3>Packing Accessory</h3>
        <label>AccessoryBarCode <input v-model="accessoryDraft.barcode" maxlength="50" :readonly="!!accessoryDraft.previous"></label>
        <label>Description <input v-model="accessoryDraft.description" maxlength="255"></label>
        <p v-if="error" class="pack-error" role="alert">{{ error }}</p>
        <div class="pack-actions"><button type="submit">Confirm</button><button type="button" @click="accessoryDraft = null; error = ''">Close</button></div>
      </form>
      <form v-else-if="mapDraft" class="pack-dialog" role="dialog" aria-label="Packing Rule Map Definition" @submit.prevent="saveMap(true)">
        <h3>Product PackingRule Map</h3>
        <label>Product / Version
          <select v-model="mapDraft.key" :disabled="mapDraft.action === 'edit'">
            <option v-for="item in products" :key="item.key" :value="item.key">{{ item.productNo }} / {{ item.productVersion }}</option>
          </select>
        </label>
        <label>PackingRuleNo
          <select v-model="mapDraft.ruleNo" @change="mapDraft.ruleVersion = ''">
            <option value="">請選擇包裝規則</option><option v-for="rule in localRules" :key="rule.no" :value="rule.no">{{ rule.no }}</option>
          </select>
        </label>
        <fieldset><legend>PackingRuleVersion</legend>
          <label><input v-model="mapDraft.current" type="radio" :value="true"> Current (*)</label>
          <label><input v-model="mapDraft.current" type="radio" :value="false"> Specified</label>
          <select v-if="!mapDraft.current" v-model="mapDraft.ruleVersion" aria-label="指定版次">
            <option value="">請選擇版次</option><option v-for="version in ruleVersions" :key="version">{{ version }}</option>
          </select>
        </fieldset>
        <p v-if="error" class="pack-error" role="alert">{{ error }}</p>
        <p v-else-if="notice" role="status">{{ notice }}</p>
        <div class="pack-actions"><button type="button" @click="saveMap(false)">Apply</button><button type="submit">Confirm</button><button type="button" @click="mapDraft = null; error = ''">Close</button></div>
      </form>
      <form v-else class="pack-dialog" role="dialog" aria-label="Copy Packaging" @submit.prevent="copyPacking">
        <h3>{{ copyAction === 'settings' ? 'Copy Pack Setting' : 'Copy Packing Accessories' }}</h3>
        <label v-for="item in products.filter(item => item.key !== product.key)" :key="item.key">
          <input v-model="copyKeys" type="checkbox" :value="item.key"> {{ item.productNo }} / {{ item.productVersion }}　{{ item.productName }}
        </label>
        <p v-if="error" class="pack-error" role="alert">{{ error }}</p>
        <div class="pack-actions"><button type="submit" :disabled="!copyKeys.length">Confirm</button><button type="button" @click="copyAction = null; error = ''">Close</button></div>
      </form>
    </div>
  </section>
</template>

<style scoped>
.pack-window { position: relative; width: min(1040px, 96vw); max-height: 92vh; overflow: auto; background: #f7f5ee; color: var(--ink, #26251f); border: 1px solid #a9a69d; box-shadow: 0 8px 30px #0004; }
.pack-bar, .pack-actions { display: flex; align-items: center; gap: 8px 12px; flex-wrap: wrap; }
.pack-bar, .pack-footer { padding: 8px 12px; border-bottom: 1px solid var(--line, #e5e1d5); }
.pack-bar h2, .pack-card h3, .pack-dialog h3 { margin: 0; font-size: 15px; }
.pack-close { margin-left: auto; }
.pack-body { padding: 10px 12px; }
.pack-card { border: 1px solid var(--line, #e5e1d5); padding: 8px; margin-bottom: 8px; }
.pack-fields { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin: 8px 0; }
.pack-fields label, .pack-dialog > label { display: flex; align-items: center; gap: 8px; min-width: 0; }
.pack-wide { grid-column: 1 / -1; }
.pack-window input:not([type="checkbox"], [type="radio"]), .pack-window select { border: 1px solid #b7ae90; padding: 4px; background: #fff2bd; color: inherit; font: inherit; min-width: 0; }
.pack-fields input { width: 100%; }
.pack-window input:read-only:not([type="checkbox"], [type="radio"]) { background: #eeece4; }
.pack-window button { min-height: 27px; border: 1px solid #b7ae90; background: #fff; padding: 3px 8px; color: inherit; font: inherit; cursor: pointer; }
.pack-window button:disabled { opacity: .5; cursor: default; }
.pack-window button:hover:not(:disabled) { background: #fff2bd; }
.pack-table { max-height: 42vh; overflow: auto; background: #fff; margin-top: 8px; }
.pack-table table { width: 100%; border-collapse: collapse; white-space: nowrap; }
.pack-table th, .pack-table td { border: 1px solid var(--line, #e5e1d5); padding: 4px 6px; text-align: left; font-size: 12px; }
.pack-table th { position: sticky; top: 0; background: #eeece4; }
.pack-footer { justify-content: flex-end; }
.pack-muted { font-size: 12px; color: var(--muted, #777366); }
.pack-error { color: #941b18; }
.pack-message { margin: 8px 12px; }
.pack-overlay { position: absolute; inset: 0; display: grid; place-items: center; background: #0005; padding: 8px; }
.pack-dialog { width: min(480px, 100%); max-height: 100%; overflow: auto; box-sizing: border-box; background: #fff; border: 1px solid #a9a69d; padding: 12px; display: grid; gap: 9px; }
@media (max-width: 640px) { .pack-fields { grid-template-columns: 1fr; } }
</style>
