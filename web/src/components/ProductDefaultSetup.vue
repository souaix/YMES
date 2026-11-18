<script setup lang="ts">
import { computed, ref } from 'vue'

type Row = Record<string, any>
type DefaultPlan = {
  updateParameter: boolean
  updateBusinessRule: boolean
  updateDescription: boolean
  updateOPResource: boolean
  updateQCItem: boolean
  updateSubOP: boolean
  updateAPS: boolean
  defaultOP: 1 | 2
}

const props = defineProps<{
  product: Row
  hasProductProcess: boolean
  developmentPreview: boolean
  qcItemAutoChecked: boolean
  apsEnabled: boolean
}>()
const emit = defineEmits<{ apply: [plan: DefaultPlan]; close: [] }>()
const issueState = computed(() => Number(props.product.issueState ?? props.product.IssueState ?? -1))
const readOnly = computed(() => !props.developmentPreview)
const canApply = computed(() => !readOnly.value && props.hasProductProcess && issueState.value === 0)
const defaultOP = ref<1 | 2>(2)
const checked = ref({
  parameter: true,
  businessRule: true,
  description: true,
  opResource: true,
  qcItem: props.qcItemAutoChecked,
  subOp: true,
  // The WinForms Designer checks APS by default even while the option is hidden.
  aps: true,
})
const confirmOpen = ref(false)
const error = ref('')
const itemOptions = [
  { key: 'parameter', label: 'Parameter', x: 90, y: 68 },
  { key: 'businessRule', label: 'BusinessRule', x: 226, y: 68 },
  { key: 'description', label: 'Description', x: 90, y: 108 },
  { key: 'opResource', label: 'OPResource', x: 226, y: 108 },
  { key: 'qcItem', label: 'QCItem', x: 90, y: 148 },
  { key: 'subOp', label: 'SubOP', x: 226, y: 148 },
] as const

function requestApply(): void {
  if (!canApply.value) return
  // APS starts checked in the legacy Designer, including when ExecuteAPS hides the checkbox.
  if (!Object.values(checked.value).some(Boolean)) {
    error.value = 'Please select one at least.'
    return
  }
  error.value = ''
  confirmOpen.value = true
}

function confirmApply(): void {
  if (!canApply.value) return
  emit('apply', {
    updateParameter: checked.value.parameter,
    updateBusinessRule: checked.value.businessRule,
    updateDescription: checked.value.description,
    updateOPResource: checked.value.opResource,
    updateQCItem: checked.value.qcItem,
    updateSubOP: checked.value.subOp,
    updateAPS: checked.value.aps,
    defaultOP: defaultOP.value,
  })
  confirmOpen.value = false
  emit('close')
}
</script>

<template>
  <section class="prd-default-window" role="dialog" aria-modal="true" aria-label="Product Load Default">
    <header class="prd-default-title">
      <span>frmProductLoadDefault</span><span v-if="developmentPreview" class="dev-badge">DEV</span>
      <button type="button" aria-label="關閉視窗" @click="emit('close')">×</button>
    </header>
    <main class="prd-default-body">
      <label class="default-op-label">Default OP</label>
      <fieldset class="default-op-group">
        <legend></legend>
        <label><input v-model="defaultOP" type="radio" :value="2" :disabled="readOnly">Product Process OP</label>
        <label><input v-model="defaultOP" type="radio" :value="1" :disabled="readOnly">All PS OP</label>
      </fieldset>

      <label v-for="item in itemOptions" :key="item.key" class="default-item" :style="{ left: item.x + 'px', top: item.y + 'px' }">
        <input v-model="checked[item.key]" type="checkbox" :disabled="readOnly || issueState !== 0">
        <span>{{ item.label }}</span>
      </label>
      <label v-if="apsEnabled" class="default-item aps-item">
        <input v-model="checked.aps" type="checkbox" :disabled="readOnly || issueState !== 0"><span>APS</span>
      </label>

      <p v-if="readOnly" class="default-status">Production API 尚未接線；正式資料唯讀。</p>
      <p v-else-if="issueState !== 0" class="default-status">產品狀態不是未核准，預設功能停用。</p>
      <p v-else-if="!hasProductProcess" class="default-status">請先為產品設定流程。</p>
      <p v-if="error" class="default-error">{{ error }}</p>

      <footer class="default-footer">
        <button class="button primary" type="button" :disabled="!canApply" @click="requestApply">Apply</button>
        <button class="button" type="button" @click="emit('close')">Close</button>
      </footer>

      <div v-if="confirmOpen" class="default-confirm-backdrop">
        <section class="default-confirm" role="alertdialog" aria-modal="true" aria-label="確認套用預設">
          <p>此操作會變更產品作業站的相關資料，請確認是否繼續執行。</p>
          <footer><button class="button primary" type="button" @click="confirmApply">OK</button><button class="button" type="button" @click="confirmOpen = false">Cancel</button></footer>
        </section>
      </div>
    </main>
  </section>
</template>

<style scoped>
.prd-default-window{box-sizing:border-box;width:min(401px,98vw);height:min(287px,96vh);min-height:265px;background:#fff;border:1px solid #aeb4ba;box-shadow:0 8px 28px #18253635;display:flex;flex-direction:column;color:#20252a;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif;overflow:hidden}
.prd-default-title{height:30px;min-height:30px;display:flex;align-items:center;padding:0 9px 0 14px;border-bottom:1px solid #d4d8dc;background:#f6f6f6;font-size:14px;font-weight:600}
.prd-default-title button{margin-left:auto;border:0;background:transparent;font-size:21px;line-height:1;color:#4f555b;cursor:pointer}
.dev-badge{margin-left:10px;padding:2px 7px;border-radius:10px;background:#fff0cf;color:#8c5a00;font:10px Arial,sans-serif}
.prd-default-body{position:relative;flex:1;min-height:0}
.default-op-label{position:absolute;left:10px;top:35px}
.default-op-group{box-sizing:border-box;position:absolute;left:83px;top:12px;width:268px;height:50px;margin:0;padding:8px 6px 4px;border:1px solid #b9bec3;display:flex;align-items:center;gap:10px;white-space:nowrap}
.default-op-group legend{padding:0}
.default-op-group label{display:flex;align-items:center;gap:4px}
.default-op-group input,.default-item input{accent-color:#467fb0;margin:0}
.default-item{position:absolute;width:118px;height:24px;display:flex;align-items:center;gap:5px;white-space:nowrap}
.default-item input{width:13px;height:13px}
.default-item input:disabled{opacity:.55}
.aps-item{left:90px;top:188px}
.default-status{position:absolute;left:10px;right:10px;bottom:48px;margin:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#777;font-size:11px}
.default-error{position:absolute;left:10px;right:10px;bottom:45px;margin:0;color:#a32323;font-size:12px}
.default-footer{position:absolute;right:12px;bottom:12px;display:flex;gap:6px}
.button{box-sizing:border-box;width:80px;height:32px;padding:4px 9px;border:1px solid #c5c9cd;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#292d31;cursor:pointer;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif}
.button:hover:not(:disabled){border-color:#eea145;background:#fff7e8}
.button:disabled{opacity:.5;cursor:not-allowed}
.button.primary{border-color:#ec9b20;background:#ffa51f;color:#fff;font-weight:600}
.default-confirm-backdrop{position:absolute;inset:0;z-index:3;display:grid;place-items:center;background:#151b2280}
.default-confirm{box-sizing:border-box;width:min(330px,92%);padding:16px;background:#fff;border:1px solid #aab0b5;box-shadow:0 5px 22px #0003}
.default-confirm p{margin:0 0 18px;line-height:1.6}
.default-confirm footer{display:flex;justify-content:flex-end;gap:8px}
@media(max-width:410px){.prd-default-window{width:98vw}.default-op-group{left:78px;right:8px;width:auto}.default-op-group label{font-size:11px;gap:2px}.default-item{width:104px}}
</style>
