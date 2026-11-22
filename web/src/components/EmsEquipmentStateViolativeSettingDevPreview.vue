<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

interface EquipmentState { code: number; name: string; color: string }
const equipmentStates: EquipmentState[] = [
  { code: 1, name: '生產', color: '#5b9e69' },
  { code: 2, name: '待機', color: '#829eae' },
  { code: 3, name: '異常', color: '#d26056' },
  { code: 4, name: '保養', color: '#cea34e' },
  { code: 5, name: '換線', color: '#6786bf' },
]
const pairKey = (current: number, target: number) => current + '>' + target
const initialPairs = [pairKey(1, 3), pairKey(2, 3), pairKey(3, 1), pairKey(4, 1), pairKey(4, 2)]
const savedPairs = ref(new Set(initialPairs))
const draftPairs = ref(new Set(initialPairs))
const statusMessage = ref('Y 軸代表目前狀態；X 軸代表禁止轉換到的狀態。')
const lastSavedAt = ref('尚未在本頁儲存')

const changedPairCount = computed(() => {
  const allKeys = new Set([...savedPairs.value, ...draftPairs.value])
  return [...allKeys].filter((key) => savedPairs.value.has(key) !== draftPairs.value.has(key)).length
})
const hasChanges = computed(() => changedPairCount.value > 0)

function isForbidden(current: number, target: number): boolean {
  return draftPairs.value.has(pairKey(current, target))
}

function toggleForbidden(current: number, target: number): void {
  const next = new Set(draftPairs.value)
  const key = pairKey(current, target)
  if (next.has(key)) next.delete(key)
  else next.add(key)
  draftPairs.value = next
  statusMessage.value = '互斥矩陣已修改 ' + changedPairCount.value + ' 個格子；按 Confirm 套用本頁 DEV 假資料。'
}

function confirmChanges(): void {
  if (!hasChanges.value) return
  savedPairs.value = new Set(draftPairs.value)
  lastSavedAt.value = new Date().toLocaleString('zh-TW', { hour12: false }) + ' · DEV-OP-01'
  statusMessage.value = 'DEV 假資料已套用；禁止轉換組合共 ' + savedPairs.value.size + ' 組。'
}
</script>

<template>
  <section v-if="dev" class="violative-state-setting" aria-label="設備狀態互斥設定 DEV">
    <header class="page-header">
      <div><div class="eyebrow">EMS · ES104 · DEV fixtures</div><h1>設備狀態互斥設定 <span>VIOLATIVE STATE CHANGE SETTING</span></h1></div>
      <button type="button" class="close-icon" aria-label="關閉" @click="emit('close')">×</button>
    </header>
    <div class="fixture-banner"><strong>DEV 假資料</strong><span>狀態基準、互斥組合及儲存結果都是虛構資料；Confirm 只更新本頁記憶體。</span></div>

    <section class="matrix-shell">
      <div class="matrix-scroll">
        <table aria-label="設備狀態禁止轉換矩陣">
          <thead>
            <tr>
              <th class="axis-cell"><span>目前狀態 ↓</span><span>禁止轉換到 →</span></th>
              <th v-for="target in equipmentStates" :key="'top-' + target.code" class="state-heading" :style="{ backgroundColor: target.color }">
                <strong>{{ target.code }}</strong><span>{{ target.name }}</span>
              </th>
              <th class="axis-cell tail-axis">目前狀態 ↓</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="current in equipmentStates" :key="'row-' + current.code">
              <th class="state-heading row-heading" :style="{ backgroundColor: current.color }"><strong>{{ current.code }}</strong><span>{{ current.name }}</span></th>
              <td v-for="target in equipmentStates" :key="pairKey(current.code, target.code)" class="matrix-slot">
                <button
                  type="button"
                  class="matrix-cell"
                  :class="{ forbidden: isForbidden(current.code, target.code) }"
                  :aria-label="'目前 ' + current.name + '，轉到 ' + target.name + '：' + (isForbidden(current.code, target.code) ? '禁止' : '允許')"
                  :title="'雙擊切換 ' + current.name + ' → ' + target.name"
                  @dblclick.prevent="toggleForbidden(current.code, target.code)"
                  @keydown.enter.prevent="toggleForbidden(current.code, target.code)"
                >
                  <span v-if="isForbidden(current.code, target.code)" aria-hidden="true">×</span>
                  <span v-else class="allowed-mark" aria-hidden="true">·</span>
                </button>
              </td>
              <th class="state-heading row-heading" :style="{ backgroundColor: current.color }"><strong>{{ current.code }}</strong><span>{{ current.name }}</span></th>
            </tr>
          </tbody>
        </table>
      </div>
      <footer class="matrix-footer">
        <div class="axis-note">畫面說明：Y 軸代表機台狀態；X 軸代表禁止轉換到的機台狀態。雙擊內部格子切換互斥標記。</div>
        <div class="save-status" role="status"><span>{{ statusMessage }}</span><span>最後 DEV 套用：{{ lastSavedAt }}</span></div>
        <div class="footer-actions"><button type="button" class="primary-button" :disabled="!hasChanges" @click="confirmChanges">Confirm <span v-if="hasChanges">({{ changedPairCount }})</span></button><button type="button" class="plain-button" @click="emit('close')">Close (X)</button></div>
      </footer>
    </section>
  </section>
</template>

<style scoped>
.violative-state-setting { min-height: 100%; padding: 12px; box-sizing: border-box; background: #f7f6f1; color: #292821; font: 13px Arial, sans-serif; }
.page-header { display: flex; align-items: center; justify-content: space-between; max-width: 1240px; margin: 0 auto 9px; }.eyebrow { color: #777366; font-size: 11px; }.page-header h1 { margin: 3px 0 0; font-size: 21px; }.page-header h1 span { margin-left: 7px; color: #777366; font-size: 12px; font-weight: 500; }
.close-icon { width: 32px; height: 32px; border: 1px solid #aaa69a; border-radius: 3px; background: #f8f7f2; color: #333; font-size: 21px; cursor: pointer; }
.fixture-banner { display: flex; align-items: center; gap: 9px; max-width: 1240px; margin: 0 auto 9px; padding: 7px 10px; border: 1px solid #e4d69d; border-radius: 3px; background: #fff8dc; color: #62551c; font-size: 12px; }.fixture-banner strong { padding: 2px 6px; border-radius: 8px; background: #efda83; font-size: 11px; }
.matrix-shell { display: flex; flex-direction: column; max-width: 1240px; min-height: min(72vh, 640px); margin: 0 auto; border: 1px solid #aaa69a; background: #f9f8f4; }.matrix-scroll { flex: 1; overflow: auto; }.matrix-scroll table { width: 100%; height: 100%; min-width: 740px; border-collapse: collapse; table-layout: fixed; }.matrix-scroll th,.matrix-scroll td { border: 1px solid #c3c0b5; }.matrix-scroll th { font-weight: 500; }.axis-cell { width: 140px; padding: 7px; background: #eceae3; color: #56544c; text-align: center; font-size: 11px; }.axis-cell span { display: block; }.tail-axis { width: 95px; }.state-heading { width: 150px; height: 78px; padding: 5px; color: #161611; text-align: center; text-shadow: 0 1px rgb(255 255 255 / 35%); }.state-heading strong,.state-heading span { display: block; }.state-heading strong { font-size: 16px; }.state-heading span { margin-top: 4px; font-size: 12px; }.row-heading { width: 140px; height: 75px; }.matrix-slot { height: 75px; padding: 0; background: #fff; }.matrix-cell { display: grid; width: 100%; height: 100%; place-items: center; border: 0; background: transparent; color: #b7211b; font: 700 43px Arial, sans-serif; cursor: default; }.matrix-cell:hover { background: #fff6e1; }.matrix-cell.forbidden { background: #fff; }.matrix-cell .allowed-mark { color: #d8d6cf; font-size: 22px; font-weight: 400; }
.matrix-footer { display: grid; grid-template-columns: 1fr auto; gap: 6px 12px; padding: 7px 9px; border-top: 1px solid #c3c0b5; background: #f1f0eb; }.axis-note { grid-column: 1 / -1; align-self: center; color: #57554c; font-size: 11px; }.save-status { display: grid; gap: 3px; align-content: center; color: #69665c; font-size: 11px; }.footer-actions { display: flex; justify-content: flex-end; gap: 6px; }.primary-button,.plain-button { min-height: 30px; padding: 4px 12px; border: 1px solid #77746b; border-radius: 3px; background: #f8f7f2; color: #292821; font: inherit; cursor: pointer; }.primary-button { border-color: #49463d; background: #49463d; color: white; }.primary-button:hover:not(:disabled) { background: #302f29; }.primary-button:disabled { border-color: #c5c2b8; background: #e8e6df; color: #96938a; cursor: default; }.plain-button:hover { background: #fff2bf; }
button:focus-visible { outline: 2px solid #d1b400; outline-offset: 2px; }
@media (max-width: 760px) { .violative-state-setting { padding: 8px; }.page-header h1 span { display: block; margin: 2px 0; }.matrix-shell { min-height: 70vh; }.matrix-footer { grid-template-columns: 1fr; }.axis-note { grid-column: auto; }.footer-actions { justify-content: flex-start; } }
</style>
