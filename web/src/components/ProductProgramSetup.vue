<script setup lang="ts">
import { computed, ref, watch } from 'vue'

type Row = Record<string, any>
type ProgramOption = { programNo: string; programType: string; description: string }

const props = defineProps<{
  product: Row
  programOptions: Row[]
  selectedProgramNos: string[]
  developmentPreview: boolean
}>()
const emit = defineEmits<{ save: [programNos: string[]]; close: [] }>()
const readOnly = computed(() => !props.developmentPreview)
const issueState = computed(() => Number(props.product.issueState ?? props.product.IssueState ?? -1))
const canConfirm = computed(() => !readOnly.value && issueState.value === 0)
const productNo = computed(() => String(props.product.productNo ?? props.product.ProductNo ?? ''))
const productVersion = computed(() => String(props.product.productVersion ?? props.product.ProductVersion ?? ''))
const selected = ref<string[]>([])

const programs = computed<ProgramOption[]>(() => (props.programOptions ?? []).map(row => ({
  programNo: String(row.programNo ?? row.ProgramNo ?? ''),
  programType: String(row.programType ?? row.ProgramType ?? ''),
  description: String(row.description ?? row.Description ?? ''),
})).filter(row => row.programNo))

watch(() => props.selectedProgramNos, value => {
  selected.value = [...new Set((value ?? []).map(String))]
}, { immediate: true, deep: true })

function toggleProgram(programNo: string, event: Event): void {
  if (readOnly.value) return
  const checked = (event.target as HTMLInputElement).checked
  selected.value = checked
    ? [...new Set([...selected.value, programNo])]
    : selected.value.filter(value => value !== programNo)
}

function confirm(): void {
  if (!canConfirm.value) return
  emit('save', [...selected.value])
  emit('close')
}
</script>

<template>
  <section class="prd-program-window" role="dialog" aria-modal="true" aria-label="Product Program">
    <header class="prd-program-title">
      <span>Product Program</span><span v-if="developmentPreview" class="dev-badge">DEV</span>
      <button type="button" aria-label="關閉視窗" @click="emit('close')">×</button>
    </header>
    <main class="prd-program-body">
      <div class="prd-program-context">
        <label for="program-product">Product No</label><input id="program-product" :value="productNo" readonly>
        <label for="program-version">Product Version</label><input id="program-version" :value="productVersion" readonly>
      </div>

      <fieldset class="prd-program-group">
        <legend>Programs</legend>
        <div class="prd-program-grid-wrap">
          <table class="prd-program-grid">
            <colgroup><col class="select-col"><col class="no-col"><col class="type-col"><col class="description-col"></colgroup>
            <thead><tr><th>Select</th><th>ProgramNo</th><th>ProgramType</th><th>Description</th></tr></thead>
            <tbody>
              <tr v-for="row in programs" :key="row.programNo">
                <td class="check-cell"><input :aria-label="'Select ' + row.programNo" type="checkbox" :checked="selected.includes(row.programNo)" :disabled="readOnly" @change="toggleProgram(row.programNo, $event)"></td>
                <td>{{ row.programNo }}</td><td>{{ row.programType }}</td><td class="description">{{ row.description }}</td>
              </tr>
              <tr v-if="!programs.length"><td colspan="4" class="empty">沒有可指派的程式資料。</td></tr>
            </tbody>
          </table>
        </div>
      </fieldset>

      <div class="prd-program-status">
        <span v-if="readOnly">Production API 尚未接線；正式資料唯讀。</span>
        <span v-else-if="issueState !== 0">產品狀態不是未核准，Confirm 停用。</span>
        <span v-else>已勾選 {{ selected.length }} 個 Program</span>
      </div>
      <footer class="prd-program-footer">
        <button class="button primary" type="button" :disabled="!canConfirm" @click="confirm">Confirm</button>
        <button class="button" type="button" @click="emit('close')">Close</button>
      </footer>
    </main>
  </section>
</template>

<style scoped>
.prd-program-window{box-sizing:border-box;width:min(684px,98vw);height:min(516px,96vh);min-height:430px;background:#fff;border:1px solid #aeb4ba;box-shadow:0 8px 28px #18253635;display:flex;flex-direction:column;color:#20252a;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif;overflow:hidden}
.prd-program-title{height:30px;min-height:30px;display:flex;align-items:center;padding:0 9px 0 14px;border-bottom:1px solid #d4d8dc;background:#f6f6f6;font-size:14px;font-weight:600}
.prd-program-title button{margin-left:auto;border:0;background:transparent;font-size:21px;line-height:1;color:#4f555b;cursor:pointer}
.dev-badge{margin-left:10px;padding:2px 7px;border-radius:10px;background:#fff0cf;color:#8c5a00;font:10px Arial,sans-serif}
.prd-program-body{position:relative;flex:1;min-height:0}
.prd-program-context{position:absolute;left:16px;top:14px;height:22px;display:flex;align-items:center}
.prd-program-context label{width:96px;text-align:right;margin-right:6px}
.prd-program-context input{box-sizing:border-box;width:202px;height:21px;padding:2px 5px;border:1px solid #aaa;background:#eee;font:13px "Times New Roman",Arial,sans-serif}
.prd-program-context label:nth-of-type(2){width:104px;margin-left:6px}
.prd-program-context input:last-child{width:72px}
.prd-program-group{position:absolute;left:16px;right:16px;top:42px;height:388px;margin:0;padding:14px 15px;border:1px solid #aeb4ba;color:#2d4771}
.prd-program-group legend{padding:0 5px;color:#2d4771}
.prd-program-grid-wrap{position:absolute;left:15px;right:15px;top:24px;bottom:14px;overflow:auto;border:1px solid #b9bec3;color:#20252a}
.prd-program-grid{width:100%;border-collapse:collapse;table-layout:fixed;white-space:nowrap;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif}
.prd-program-grid .select-col{width:70px}.prd-program-grid .no-col{width:100px}.prd-program-grid .type-col{width:100px}.prd-program-grid .description-col{width:150px}
.prd-program-grid th{position:sticky;top:0;z-index:1;height:26px;padding:3px 7px;background:#4b9ce3;color:#fff;border:1px solid #d3d8dc;text-align:left;font-weight:500}
.prd-program-grid td{height:24px;padding:2px 7px;border:1px solid #d3d7da;background:#fff;overflow:hidden;text-overflow:ellipsis}
.prd-program-grid td.check-cell{text-align:center;padding:2px}
.prd-program-grid td.check-cell input{margin:0;accent-color:#467fb0}
.prd-program-grid .description{max-width:150px}
.prd-program-grid .empty{text-align:center;color:#737b82;height:50px}
.prd-program-status{position:absolute;left:17px;right:190px;bottom:20px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#737b82;font-size:12px}
.prd-program-footer{position:absolute;right:14px;bottom:13px;display:flex;gap:6px}
.button{box-sizing:border-box;width:80px;height:32px;padding:4px 9px;border:1px solid #c5c9cd;border-radius:3px;background:linear-gradient(#fff,#f4f4f4);color:#292d31;cursor:pointer;font:13px "Times New Roman",Arial,"Microsoft JhengHei",sans-serif}
.button:hover:not(:disabled){border-color:#eea145;background:#fff7e8}
.button:disabled{opacity:.5;cursor:not-allowed}
.button.primary{border-color:#ec9b20;background:#ffa51f;color:#fff;font-weight:600}
@media(max-width:600px){.prd-program-window{width:98vw}.prd-program-context{left:8px;transform:scale(.82);transform-origin:left center}.prd-program-group{left:8px;right:8px}.prd-program-status{left:8px}}
</style>
