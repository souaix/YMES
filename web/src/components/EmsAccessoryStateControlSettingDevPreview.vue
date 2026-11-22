<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref } from 'vue'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)

const controlDefinitions = [
  { key: 'Initial', label: '初始狀態', purpose: '配件初始建檔' },
  { key: 'Apply', label: '領用／上機', purpose: '配件領用' },
  { key: 'Return', label: '退回', purpose: '配件退回' },
  { key: 'GetFromINV', label: '從庫存領出', purpose: '庫存領出' },
  { key: 'Repair', label: '維修', purpose: '送修處理' },
  { key: 'I_SetupClose', label: 'I_SetupClose', purpose: 'IC 封裝設置結案' },
  { key: 'ReturnToINV', label: '歸還庫存', purpose: '歸還庫存' },
  { key: 'Scrap', label: '報廢', purpose: '報廢處理' },
] as const

type ControlKey = typeof controlDefinitions[number]['key']
interface AccessoryState { code: number; name: string; color: string }

const stateOptions: AccessoryState[] = [
  { code: 1, name: '庫存', color: '#5b9b70' },
  { code: 2, name: '使用中', color: '#4e829e' },
  { code: 3, name: '維修中', color: '#c8953e' },
  { code: 4, name: '報廢', color: '#ae5650' },
  { code: 5, name: '待驗收', color: '#8b75a9' },
]

// 全部為此頁面專用假資料；不讀寫 API、Oracle、PostgreSQL 或 localStorage。
const currentValues = reactive<Record<ControlKey, number>>({
  Initial: 1,
  Apply: 2,
  Return: 1,
  GetFromINV: 2,
  Repair: 3,
  ReturnToINV: 1,
  Scrap: 4,
  I_SetupClose: 1,
})
const appliedValues = ref<Record<ControlKey, number>>({ ...currentValues })
const icFtpkgActive = ref(false)
const pickerKey = ref<ControlKey | null>(null)
const statusMessage = ref('選取狀態後使用 Apply 暫存，或 Confirm 儲存並關閉。')
const confirmCloseTimer = ref<number | null>(null)

const visibleControls = computed(() => controlDefinitions.filter((item) => item.key !== 'I_SetupClose' || icFtpkgActive.value))
const changedCount = computed(() => visibleControls.value.filter((item) => currentValues[item.key] !== appliedValues.value[item.key]).length)
const activeControl = computed(() => controlDefinitions.find((item) => item.key === pickerKey.value) ?? null)

function stateFor(code: number): AccessoryState {
  return stateOptions.find((item) => item.code === code) ?? { code, name: '未知狀態', color: '#98958a' }
}

function openPicker(key: ControlKey) {
  pickerKey.value = key
}

function chooseState(code: number) {
  if (!pickerKey.value) return
  currentValues[pickerKey.value] = code
  pickerKey.value = null
  statusMessage.value = '設定已變更；Apply 或 Confirm 才會寫入本頁假資料。'
}

function save(closeAfterSave: boolean) {
  const invalidControl = visibleControls.value.find((item) => !stateOptions.some((state) => state.code === currentValues[item.key]))
  if (invalidControl) {
    statusMessage.value = `${invalidControl.label} 必須選擇有效的配件狀態。`
    return
  }

  Object.assign(appliedValues.value, currentValues)
  statusMessage.value = closeAfterSave ? 'DEV 假資料已儲存，畫面即將關閉。' : 'DEV 假資料已套用；設定視窗保持開啟。'
  if (closeAfterSave) confirmCloseTimer.value = window.setTimeout(() => emit('close'), 800)
}

onBeforeUnmount(() => {
  if (confirmCloseTimer.value !== null) window.clearTimeout(confirmCloseTimer.value)
})
</script>

<template>
  <section v-if="dev" class="acc-control-page" aria-label="配件狀態控制設定 DEV">
    <header class="page-heading">
      <div>
        <div class="title-line">
          <h1>配件狀態控制設定</h1>
          <span class="dev-badge">DEV 假資料</span>
        </div>
        <p>設定配件流程使用的狀態代碼；點選「…」從配件狀態基礎選擇。</p>
      </div>
      <label class="module-toggle">
        <input v-model="icFtpkgActive" type="checkbox" />
        <span>模擬啟用 IC_FT_PKG</span>
      </label>
    </header>

    <div class="table-wrap">
      <table>
        <thead>
          <tr><th>流程操作</th><th>ControlNo</th><th>AccessoryState</th><th>StateName</th><th aria-label="選取"></th></tr>
        </thead>
        <tbody>
          <tr v-for="control in visibleControls" :key="control.key">
            <th scope="row">
              <span class="control-label">{{ control.label }}</span>
              <span class="purpose">{{ control.purpose }}</span>
            </th>
            <td><code>{{ control.key }}</code></td>
            <td class="state-code">{{ currentValues[control.key] }}</td>
            <td>
              <span class="state-name">
                <i :style="{ backgroundColor: stateFor(currentValues[control.key]).color }"></i>
                {{ stateFor(currentValues[control.key]).name }}
              </span>
            </td>
            <td><button class="pick-button" type="button" :aria-label="`選擇${control.label}狀態`" @click="openPicker(control.key)">…</button></td>
          </tr>
        </tbody>
      </table>
    </div>

    <footer class="page-footer">
      <p class="status" role="status">{{ statusMessage }}</p>
      <span class="dirty-count">未套用變更：{{ changedCount }}</span>
      <div class="actions">
        <button type="button" class="secondary" @click="emit('close')">Close</button>
        <button type="button" class="secondary" :disabled="changedCount === 0" @click="save(false)">Apply</button>
        <button type="button" class="primary" @click="save(true)">Confirm</button>
      </div>
    </footer>

    <div v-if="activeControl" class="modal-backdrop" @click.self="pickerKey = null">
      <section class="state-picker" role="dialog" aria-modal="true" :aria-label="`選擇${activeControl.label}狀態`">
        <header>
          <div><h2>選擇配件狀態</h2><p>{{ activeControl.label }} · {{ activeControl.key }}</p></div>
          <button type="button" class="close-picker" aria-label="關閉" @click="pickerKey = null">×</button>
        </header>
        <div class="state-options">
          <button
            v-for="state in stateOptions"
            :key="state.code"
            type="button"
            class="state-option"
            :class="{ selected: currentValues[activeControl.key] === state.code }"
            @click="chooseState(state.code)"
          >
            <span class="state-swatch" :style="{ backgroundColor: state.color }"></span>
            <strong>{{ state.code }}</strong>
            <span>{{ state.name }}</span>
            <span v-if="currentValues[activeControl.key] === state.code" class="current-tag">目前設定</span>
          </button>
        </div>
      </section>
    </div>
  </section>
  <section v-else class="not-available" role="status">DEV 預覽只在開發環境提供。</section>
</template>

<style scoped>
.acc-control-page { position: relative; display: flex; flex-direction: column; gap: 12px; min-height: 0; height: 100%; padding: 14px 18px; box-sizing: border-box; overflow: auto; background: #f7f5ee; color: #26251f; font: inherit; }
.page-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; }
.title-line { display: flex; align-items: center; gap: 10px; }
h1 { margin: 0; font-size: 20px; }
.page-heading p { margin: 5px 0 0; color: #777366; font-size: 13px; }
.dev-badge { padding: 3px 8px; border-radius: 12px; background: #fff0bd; color: #6d5713; font-size: 11px; font-weight: 700; }
.module-toggle { display: flex; align-items: center; gap: 7px; padding: 7px 10px; border: 1px solid #d5d0c2; border-radius: 4px; background: #fffdf7; color: #555247; font-size: 12px; white-space: nowrap; cursor: pointer; }
.table-wrap { flex: 1; min-height: 0; overflow: auto; border: 1px solid #d8d3c5; border-radius: 5px; background: #fffdf7; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
thead { position: sticky; top: 0; z-index: 1; background: #ece9df; color: #555247; text-align: left; }
th, td { padding: 8px 10px; border-bottom: 1px solid #e7e3d8; }
thead th { height: 24px; font-size: 11px; font-weight: 700; letter-spacing: .02em; }
tbody tr:last-child th, tbody tr:last-child td { border-bottom: 0; }
tbody th { width: 27%; text-align: left; font-weight: 600; }
tbody td:nth-child(2) { width: 24%; }
tbody td:nth-child(3) { width: 15%; }
tbody td:nth-child(4) { width: 25%; }
.control-label { display: block; }
.purpose { display: block; margin-top: 2px; color: #898578; font-size: 11px; font-weight: 400; }
code { padding: 2px 5px; border-radius: 3px; background: #f0eee7; color: #625f55; font-size: 11px; }
.state-code { font-variant-numeric: tabular-nums; }
.state-name { display: inline-flex; align-items: center; gap: 7px; }
.state-name i, .state-swatch { width: 10px; height: 10px; border: 1px solid #0002; border-radius: 50%; }
button { font: inherit; cursor: pointer; }
.pick-button { width: 28px; height: 25px; border: 1px solid #aca696; border-radius: 3px; background: #f8f6ef; color: #35332e; font-weight: 700; }
.pick-button:hover { border-color: #625f55; background: #f0eee7; }
.page-footer { display: flex; align-items: center; gap: 14px; min-height: 38px; }
.status { flex: 1; margin: 0; color: #686457; font-size: 12px; }
.dirty-count { color: #8a6f21; font-size: 11px; white-space: nowrap; }
.actions { display: flex; gap: 7px; }
.actions button { min-width: 72px; min-height: 29px; padding: 4px 11px; border-radius: 4px; }
.secondary { border: 1px solid #b9b4a6; background: #fffdf7; color: #37352f; }
.secondary:disabled { opacity: .5; cursor: default; }
.primary { border: 1px solid #3b3930; background: #3b3930; color: white; }
button:focus-visible, input:focus-visible { outline: 2px solid #d8b622; outline-offset: 2px; }
.modal-backdrop { position: fixed; inset: 0; z-index: 20; display: grid; place-items: center; padding: 20px; background: #18171277; }
.state-picker { width: min(440px, 100%); padding: 16px; border: 1px solid #c9c3b4; border-radius: 7px; background: #fffdf7; box-shadow: 0 16px 48px #0003; }
.state-picker header { display: flex; justify-content: space-between; gap: 16px; margin-bottom: 12px; }
.state-picker h2 { margin: 0; font-size: 17px; }
.state-picker p { margin: 4px 0 0; color: #777366; font-size: 12px; }
.close-picker { width: 28px; height: 28px; border: 0; border-radius: 4px; background: transparent; color: #625f55; font-size: 21px; }
.state-options { display: grid; gap: 6px; }
.state-option { display: flex; align-items: center; gap: 9px; min-height: 39px; padding: 5px 9px; border: 1px solid #e3dfd4; border-radius: 4px; background: #fff; color: #36342e; text-align: left; }
.state-option.selected { border-color: #88742e; background: #fff9df; }
.state-option strong { min-width: 20px; font-variant-numeric: tabular-nums; }
.current-tag { margin-left: auto; color: #7b671d; font-size: 10px; }
.not-available { padding: 12px; background: #f7f5ee; color: #777366; }
@media (max-width: 680px) { .page-heading, .page-footer { align-items: stretch; flex-direction: column; } .module-toggle { align-self: flex-start; } .page-footer { gap: 8px; } .actions { justify-content: flex-end; } .table-wrap { min-height: 280px; } th, td { padding: 7px 6px; } tbody th { width: 34%; } tbody td:nth-child(2) { display: none; } }
</style>
