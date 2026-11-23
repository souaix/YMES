<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface AccessoryState { code: number; name: string; color: string }
interface AccessoryCategory { code: string; name: string }

const categories: AccessoryCategory[] = [
  { code: 'DEV_MASK', name: 'DEV 光罩配件類別' },
  { code: 'DEV_PROBE', name: 'DEV 探針配件類別' },
  { code: 'DEV_TOOLING', name: 'DEV 治具配件類別' },
]
const accessoryStates: AccessoryState[] = [
  { code: 0, name: 'DEV 狀態 A', color: '#92a5ac' },
  { code: 1, name: 'DEV 狀態 B', color: '#6795a9' },
  { code: 2, name: 'DEV 狀態 C', color: '#65a074' },
  { code: 3, name: 'DEV 狀態 D', color: '#d0a149' },
  { code: 4, name: 'DEV 狀態 E', color: '#c87568' },
  { code: 5, name: 'DEV 狀態 F', color: '#8a79b0' },
]
const pairKey = (current: number, target: number) => `${current}>${target}`
const fixturePairs: Record<string, string[]> = {
  DEV_MASK: [pairKey(0, 4), pairKey(1, 3), pairKey(2, 0), pairKey(4, 1)],
  DEV_PROBE: [pairKey(0, 5), pairKey(2, 3), pairKey(3, 1), pairKey(5, 0)],
  DEV_TOOLING: [pairKey(1, 4), pairKey(3, 2), pairKey(4, 0), pairKey(5, 1)],
}

const categoryCode = ref('')
const savedPairs = ref(new Set<string>())
const draftPairs = ref(new Set<string>())
const canConfirm = ref(false)
const statusMessage = ref('請選擇配件類別。Y 軸代表配件狀態；X 軸代表禁止轉換到的狀態。')
const lastSavedAt = ref('尚未在本頁確認')
const selectedCategory = computed(() => categories.find((category) => category.code === categoryCode.value) ?? null)
const changedPairCount = computed(() => {
  const allPairs = new Set([...savedPairs.value, ...draftPairs.value])
  return [...allPairs].filter((key) => savedPairs.value.has(key) !== draftPairs.value.has(key)).length
})

function chooseCategory(code: string) {
  categoryCode.value = code
  const pairs = fixturePairs[code] ?? []
  savedPairs.value = new Set(pairs)
  draftPairs.value = new Set(pairs)
  canConfirm.value = false
  const category = categories.find((item) => item.code === code)
  statusMessage.value = `${category?.name ?? code} 的 DEV 互斥矩陣已載入。雙擊內部格子切換禁止標記。`
}

function onCategoryChange(event: Event): void {
  chooseCategory((event.target as HTMLSelectElement).value)
}

function isForbidden(current: number, target: number): boolean {
  return draftPairs.value.has(pairKey(current, target))
}

function toggleForbidden(current: number, target: number): void {
  if (!categoryCode.value) return
  const next = new Set(draftPairs.value)
  const key = pairKey(current, target)
  if (next.has(key)) next.delete(key)
  else next.add(key)
  draftPairs.value = next
  canConfirm.value = true
  statusMessage.value = `互斥矩陣已修改 ${changedPairCount.value} 個格子；Confirm 只更新本頁 DEV 假資料。`
}

function confirmChanges(): void {
  if (!canConfirm.value || !categoryCode.value) return
  savedPairs.value = new Set(draftPairs.value)
  canConfirm.value = false
  lastSavedAt.value = `${new Date().toLocaleString('zh-TW', { hour12: false })} · DEV_USER`
  statusMessage.value = `DEV 假資料已確認；目前有 ${savedPairs.value.size} 組禁止轉換設定。`
}
</script>

<template>
  <section v-if="dev" class="accessory-violative-setting" aria-label="配件狀態互斥設定 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES105 · DEV fixtures</div><h1>配件狀態互斥設定 <span>VIOLATIVE STATE CHANGE SETTING</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="fixture-banner"><strong>DEV 假資料</strong><span>類別、狀態名稱、顏色、禁止轉換組合及 Confirm 結果均為虛構，只存在本頁記憶體。</span></div>

    <section class="category-bar" aria-label="配件類別">
      <label for="accessory-category">Accessory Category</label>
      <select id="accessory-category" :value="categoryCode" @change="onCategoryChange">
        <option value="">請選擇配件類別</option>
        <option v-for="category in categories" :key="category.code" :value="category.code">{{ category.code }} · {{ category.name }}</option>
      </select>
      <span v-if="selectedCategory" class="category-note">{{ selectedCategory.name }}</span>
    </section>

    <section class="matrix-shell">
      <div v-if="!selectedCategory" class="choose-category">選擇配件類別後載入互斥矩陣 DEV 假資料。</div>
      <div v-else class="matrix-scroll">
        <table aria-label="配件狀態禁止轉換矩陣">
          <thead>
            <tr>
              <th class="axis-cell"><span>Y 軸</span><span>目前狀態 ↓</span></th>
              <th v-for="target in accessoryStates" :key="'top-' + target.code" class="state-heading" :style="{ backgroundColor: target.color }">
                <strong>{{ target.code }}</strong><span>{{ target.name }}</span>
              </th>
              <th class="axis-cell tail-axis"><span>目前狀態 ↓</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="current in accessoryStates" :key="'row-' + current.code">
              <th class="state-heading row-heading" :style="{ backgroundColor: current.color }"><strong>{{ current.code }}</strong><span>{{ current.name }}</span></th>
              <td v-for="target in accessoryStates" :key="pairKey(current.code, target.code)" class="matrix-slot">
                <button
                  type="button"
                  class="matrix-cell"
                  :class="{ forbidden: isForbidden(current.code, target.code) }"
                  :aria-label="`目前 ${current.name}，轉到 ${target.name}：${isForbidden(current.code, target.code) ? '禁止' : '允許'}`"
                  :title="`雙擊切換 ${current.name} → ${target.name}`"
                  @dblclick.prevent="toggleForbidden(current.code, target.code)"
                  @keydown.enter.prevent="toggleForbidden(current.code, target.code)"
                ><span v-if="isForbidden(current.code, target.code)" aria-hidden="true">×</span><span v-else class="allowed-mark" aria-hidden="true">·</span></button>
              </td>
              <th class="state-heading row-heading" :style="{ backgroundColor: current.color }"><strong>{{ current.code }}</strong><span>{{ current.name }}</span></th>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="matrix-footer">
        <div class="axis-note">畫面說明：Y 軸代表目前配件狀態；X 軸代表禁止轉換到的配件狀態。雙擊內部格子切換互斥標記。</div>
        <div class="save-status" role="status"><span>{{ statusMessage }}</span><span>最後 DEV 確認：{{ lastSavedAt }}</span></div>
        <div class="footer-actions"><button type="button" class="primary-button" :disabled="!canConfirm" @click="confirmChanges">Confirm <span v-if="changedPairCount">({{ changedPairCount }})</span></button><button type="button" class="plain-button" @click="emit('close')">Close (X)</button></div>
      </footer>
    </section>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.accessory-violative-setting { min-height: 100%; padding: 12px; box-sizing: border-box; background: #f7f6f1; color: #292821; font: 13px Arial, sans-serif; }
.page-header { display: flex; align-items: center; justify-content: space-between; max-width: 1240px; margin: 0 auto 9px; }
.eyebrow { color: #777366; font-size: 11px; }
.page-header h1 { margin: 3px 0 0; font-size: 21px; }
.page-header h1 span { margin-left: 7px; color: #777366; font-size: 12px; font-weight: 500; }
.close-icon { width: 32px; height: 32px; border: 1px solid #aaa69a; border-radius: 3px; background: #f8f7f2; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner { display: flex; align-items: center; gap: 9px; max-width: 1240px; margin: 0 auto 9px; padding: 7px 10px; border: 1px solid #e4d69d; border-radius: 3px; background: #fff8dc; color: #62551c; font-size: 12px; }
.fixture-banner strong { padding: 2px 6px; border-radius: 8px; background: #efda83; font-size: 11px; }
.category-bar { display: flex; align-items: center; gap: 9px; max-width: 1240px; margin: 0 auto 9px; padding: 8px 10px; border: 1px solid #d2cfc4; border-radius: 3px; background: #fffdf7; }
.category-bar label { color: #555247; font-size: 11px; font-weight: 700; }
.category-bar select { min-width: 260px; min-height: 29px; padding: 3px 7px; border: 1px solid #c8c2b4; border-radius: 3px; background: white; color: #292821; font: inherit; font-size: 12px; }
.category-note { color: #777366; font-size: 11px; }
.matrix-shell { display: flex; flex-direction: column; max-width: 1240px; min-height: min(68vh, 620px); margin: 0 auto; border: 1px solid #aaa69a; background: #f9f8f4; }
.matrix-scroll { flex: 1; overflow: auto; }
.matrix-scroll table { width: 100%; height: 100%; min-width: 860px; border-collapse: collapse; table-layout: fixed; }
.matrix-scroll th,.matrix-scroll td { border: 1px solid #c3c0b5; }
.matrix-scroll th { font-weight: 500; }
.axis-cell { width: 115px; padding: 7px; background: #eceae3; color: #56544c; text-align: center; font-size: 11px; }
.axis-cell span { display: block; }
.tail-axis { width: 115px; }
.state-heading { width: 145px; height: 70px; padding: 5px; color: #161611; text-align: center; text-shadow: 0 1px rgb(255 255 255 / 35%); }
.state-heading strong,.state-heading span { display: block; }
.state-heading strong { font-size: 15px; }
.state-heading span { margin-top: 4px; font-size: 11px; }
.row-heading { width: 125px; height: 66px; }
.matrix-slot { height: 66px; padding: 0; background: white; }
.matrix-cell { display: grid; width: 100%; height: 100%; place-items: center; border: 0; background: transparent; color: #b7211b; font: 700 40px Arial, sans-serif; cursor: default; }
.matrix-cell:hover { background: #fff6e1; }
.matrix-cell .allowed-mark { color: #d8d6cf; font-size: 22px; font-weight: 400; }
.choose-category { display: grid; flex: 1; min-height: 280px; place-items: center; color: #817d72; font-size: 12px; }
.matrix-footer { display: grid; grid-template-columns: 1fr auto; gap: 6px 12px; padding: 7px 9px; border-top: 1px solid #c3c0b5; background: #f1f0eb; }
.axis-note { grid-column: 1 / -1; align-self: center; color: #57554c; font-size: 11px; }
.save-status { display: grid; gap: 3px; align-content: center; color: #69665c; font-size: 11px; }
.footer-actions { display: flex; justify-content: flex-end; gap: 6px; }
.primary-button,.plain-button { min-height: 30px; padding: 4px 12px; border: 1px solid #77746b; border-radius: 3px; background: #f8f7f2; color: #292821; font: inherit; cursor: pointer; }
.primary-button { border-color: #49463d; background: #49463d; color: white; }
.primary-button:disabled { border-color: #c5c2b8; background: #e8e6df; color: #96938a; cursor: default; }
.plain-button:hover { background: #fff2bf; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
button:focus-visible, select:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) { .accessory-violative-setting { padding: 8px; }.page-header h1 span { display: block; margin: 2px 0; }.category-bar { align-items: stretch; flex-direction: column; }.category-bar select { width: 100%; }.matrix-shell { min-height: 70vh; }.matrix-footer { grid-template-columns: 1fr; }.axis-note { grid-column: auto; }.footer-actions { justify-content: flex-start; } }
</style>
