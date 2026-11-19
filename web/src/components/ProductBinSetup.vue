<script setup lang="ts">
import { computed, ref } from 'vue'

type BinDefinition = {
  binNo: string
  binValue: number
  osBin: number
  dGrade: number
  rank: string
  description: string
}

const props = defineProps<{
  productNo: string
  productVersion: string
  canEdit: boolean
  developmentPreview: boolean
  savedBins: BinDefinition[]
  save: (bins: BinDefinition[]) => boolean
}>()
const emit = defineEmits<{ close: [] }>()

// The system catalog supplies the row order. Replace this fixture with the BinOrder-sorted lookup.
const developmentBinNos = ['01', '02', '03', '04', '05', '06', '07', '08']
const rows = ref(props.developmentPreview ? developmentBinNos.map(binNo => {
  const saved = props.savedBins.find(row => row.binNo === binNo)
  return { binNo, binValue: 0, osBin: 0, dGrade: 0, rank: '', description: '', ...saved, selected: Boolean(saved) }
}) : [])
const editable = computed(() => props.developmentPreview && props.canEdit)
const error = ref('')

function confirm(): void {
  if (!editable.value) return
  const selected = rows.value.filter(row => row.selected)
  const invalid = selected.find(row => row.binValue === 0 && row.osBin === 1)
  if (invalid) {
    error.value = `Bin ${invalid.binNo} 的結果為 Good 時，OS Bin 必須為 No。`
    return
  }
  const bins = selected.map(({ binNo, binValue, osBin, dGrade, rank, description }) =>
    ({ binNo, binValue, osBin, dGrade, rank, description }))
  if (!props.save(bins)) {
    error.value = '無法儲存此產品版本的 Bin 定義。'
    return
  }
  emit('close')
}
</script>

<template>
  <section class="bin-dialog" role="dialog" aria-modal="true" aria-labelledby="product-bin-title">
    <header><h2 id="product-bin-title">Bin 定義</h2><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <dl><dt>產品編號</dt><dd>{{ productNo }}</dd><dt>產品版本</dt><dd>{{ productVersion }}</dd></dl>
    <p v-if="!developmentPreview">Bin 資料等待 API 接入。</p>
    <div v-else class="bin-grid">
      <table>
        <thead><tr><th>選取</th><th>BinNo</th><th>Bin 結果</th><th>說明</th><th>OS Bin</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row.binNo">
            <td><input v-model="row.selected" type="checkbox" :disabled="!editable" :aria-label="`選取 Bin ${row.binNo}`" /></td>
            <th scope="row">{{ row.binNo }}</th>
            <td><select v-model="row.binValue" :disabled="!editable" :aria-label="`Bin ${row.binNo} 結果`"><option :value="0">Good</option><option :value="1">Fail</option></select></td>
            <td><input v-model="row.description" type="text" :disabled="!editable" :aria-label="`Bin ${row.binNo} 說明`" /></td>
            <td><select v-model="row.osBin" :disabled="!editable" :aria-label="`Bin ${row.binNo} 的 OS Bin`"><option :value="0">No</option><option :value="1" :disabled="row.binValue === 0">Yes</option></select></td>
          </tr>
        </tbody>
      </table>
    </div>
    <p v-if="error" role="alert" class="bin-error">{{ error }}</p>
    <footer><button type="button" :disabled="!editable" @click="confirm">確認</button><button type="button" @click="emit('close')">關閉</button></footer>
  </section>
</template>

<style scoped>
.bin-dialog{display:grid;gap:12px;width:min(700px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;border:1px solid #c9c7bd;border-radius:6px;box-shadow:0 12px 36px #28241738}
header,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p,dl,dd{margin:0}
dl{display:flex;flex-wrap:wrap;align-items:baseline;gap:6px 12px}dd{margin-right:16px;font-weight:600}
.bin-grid{overflow:auto}table{width:100%;border-collapse:collapse;text-align:left}thead{background:#f4f2eb}th,td{padding:6px;border:1px solid #dedbd1}td:first-child{text-align:center}select,input[type=text]{width:100%;min-height:27px;border:1px solid #b2aea0;background:#fff;padding:3px 5px}input[type=text]{min-width:120px}select:disabled,input:disabled{background:#f4f3ef;color:#655f4d}
button{padding:4px 12px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.55}.bin-error{color:#a32020}
</style>
