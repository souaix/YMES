<script setup lang="ts">
import { computed } from 'vue'

type ExpansionRow = {
  itemNo: number
  opOrder: number
  opNo: string
  opShortName: string
  description: string
  stageNo?: string
  moduleNo?: string
  moduleVersion?: string
  processNo?: string
}

const props = defineProps<{
  productNo: string
  productVersion: string
  processNo: string
  processVersion: string
  psNo: string
  moTypeNo: string
  developmentPreview: boolean
  moduleMode?: boolean
}>()
const emit = defineEmits<{ close: [] }>()

const developmentRoutes: Record<string, { version: string; rows: ExpansionRow[] }> = {
  'DEV-PROC-01': { version: '01', rows: [
    { itemNo: 1, opOrder: 10, opNo: 'DEV-OP-10', opShortName: '點膠', description: '點膠作業', stageNo: 'DEV-STAGE-10' },
    { itemNo: 1, opOrder: 20, opNo: 'DEV-OP-20', opShortName: '組裝', description: '組裝子流程作業', stageNo: 'DEV-STAGE-20', moduleNo: 'DEV-MODULE-20', moduleVersion: '01', processNo: 'DEV-SUBPROC-20' },
  ] },
  'DEV-PROC-02': { version: '03', rows: [
    { itemNo: 1, opOrder: 10, opNo: 'DEV-OP-10', opShortName: '點膠', description: '點膠作業', stageNo: 'DEV-STAGE-10' },
  ] },
  'DEV-PROC-20': { version: '02', rows: [
    { itemNo: 1, opOrder: 10, opNo: 'DEV-OP-20', opShortName: '組裝', description: '主線組裝作業' },
    { itemNo: 1, opOrder: 20, opNo: 'DEV-OP-30', opShortName: '檢驗', description: '網狀流程檢驗節點' },
  ] },
  'DEV-PROC-30': { version: '01', rows: [
    { itemNo: 1, opOrder: 10, opNo: 'DEV-OP-30', opShortName: '檢驗', description: '試產檢驗作業', stageNo: 'DEV-STAGE-30' },
  ] },
}
const mainVersion = computed(() => props.developmentPreview && props.processVersion === '*'
  ? developmentRoutes[props.processNo]?.version ?? '*' : props.processVersion)
const rows = computed(() => {
  const route = developmentRoutes[props.processNo]
  if (!props.developmentPreview || !route || route.version !== mainVersion.value) return []
  return [...route.rows].sort((a, b) => a.itemNo - b.itemNo || a.opOrder - b.opOrder)
})
</script>

<template>
  <section class="expansion-dialog" role="dialog" aria-modal="true" aria-labelledby="process-expansion-title">
    <header><h2 id="process-expansion-title">流程展開</h2><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <p><span v-if="moduleMode">模組 {{ productNo }} / {{ productVersion }} · </span>區段 {{ psNo }} · MOType {{ moTypeNo }}<span v-if="developmentPreview"> · {{ psNo === 'DEV-PS-20' ? 'DEV 網狀流程' : 'DEV 一般流程' }}</span></p>
    <div v-if="rows.length" class="grid">
      <table>
        <thead><tr><template v-if="!moduleMode"><th>ProductNo</th><th>ProductVersion</th><th>MainProcessNo</th><th>MainProcessVersion</th></template><th>ItemNo</th><th>ModuleNo</th><th>ModuleVersion</th><th>ProcessNo</th><th>OPNo</th><th>OPShortName</th><th>Description</th><th v-if="!moduleMode">StageNo</th></tr></thead>
        <tbody><tr v-for="row in rows" :key="`${row.itemNo}:${row.opOrder}:${row.opNo}`">
          <template v-if="!moduleMode"><td>{{ productNo }}</td><td>{{ productVersion }}</td><td>{{ processNo }}</td><td>{{ mainVersion }}</td></template><td>{{ row.itemNo }}</td>
          <td>{{ row.moduleNo ?? '—' }}</td><td>{{ row.moduleVersion ?? '—' }}</td><td>{{ row.processNo ?? processNo }}</td>
          <td>{{ row.opNo }}</td><td>{{ row.opShortName }}</td><td>{{ row.description }}</td><td v-if="!moduleMode">{{ row.stageNo ?? '—' }}</td>
        </tr></tbody>
      </table>
    </div>
    <p v-else>{{ developmentPreview ? '此流程版次沒有 DEV 展開資料。' : '正式流程展開等待 API 接入。' }}</p>
    <footer><button type="button" @click="emit('close')">關閉</button></footer>
  </section>
</template>

<style scoped>
.expansion-dialog{display:grid;gap:12px;width:min(1100px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;border:1px solid #c9c7bd;border-radius:6px;box-shadow:0 12px 36px #28241738}
header,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p{margin:0}
.grid{overflow:auto}table{width:100%;border-collapse:collapse;text-align:left}thead{background:#f4f2eb}th,td{padding:6px;border:1px solid #dedbd1;white-space:nowrap}
button{padding:4px 12px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}
</style>
