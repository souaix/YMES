<script setup lang="ts">
import { ref } from 'vue'
import { parseWorksheetsXlsx } from './materialImportWorkbook'

type ModuleImportRow = { moduleNo: string; moduleVersion: string; moduleName: string; moduleType: string; psNo: string; description: string }
const props = defineProps<{ importModules: (rows: ModuleImportRow[]) => string[] }>()
const emit = defineEmits<{ close: [] }>()
const modules = ref<ModuleImportRow[]>([])
const processes = ref<string[]>([])
const results = ref<string[]>([])
const busy = ref(false)
const error = ref('')

function sections(rows: string[][], expected: string[]): number[] {
  let last = -1
  return expected.map(marker => {
    last = rows.findIndex((row, index) => index > last && row[0]?.toUpperCase() === marker)
    if (last < 0) throw new Error(`工作表缺少 ${marker}，或區段順序錯誤。`)
    return last
  })
}

async function choose(event: Event): Promise<void> {
  modules.value = []; processes.value = []; results.value = []; error.value = ''
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  busy.value = true
  try {
    const [process, module, op, parameter] = await parseWorksheetsXlsx(file, 5)
    const [start, end] = sections(module, ['[MODULE BASIS]', '[MODULE PROCESS]', '[MODULE PROPERTY]', '[RECIPE GROUP]'])
    sections(op, ['[OP DESCRIPTION]', '[SUB OP]'])
    sections(parameter, ['[ATTRIB]', '[EQP RECIPE]'])
    const data: ModuleImportRow[] = []
    for (const [code, version, moduleName, type, segment, description = ''] of module.slice(start + 2, end)) {
      if (!code) continue
      if (!version || !moduleName || !type || !segment) throw new Error(`模組 ${code} 缺少主檔必填欄位。`)
      data.push({ moduleNo: code.toUpperCase(), moduleVersion: version.toUpperCase(), moduleName, moduleType: type.toUpperCase(), psNo: segment.toUpperCase(), description })
    }
    processes.value = [...new Set(process.slice(1).filter(row => row[1] && row[2]).map(row => `${row[1]} / ${row[2]}`))]
    modules.value = data
    if (!data.length) error.value = '工作簿沒有可匯入的模組主檔。'
  } catch (cause) { error.value = cause instanceof Error ? cause.message : '無法讀取模組工作簿。' }
  finally { busy.value = false }
}
</script>

<template>
  <section class="import-dialog" role="dialog" aria-modal="true" aria-labelledby="module-import-title">
    <header><h2 id="module-import-title">製程模組匯入</h2><button @click="emit('close')">關閉</button></header>
    <p>DEV 使用 XLSX 測試資料，預覽流程並新增模組主檔。</p>
    <label>工作簿 <input type="file" accept=".xlsx" :disabled="busy" @change="choose" /></label>
    <p v-if="busy" role="status">正在讀取工作簿…</p><p v-if="error" role="alert">{{ error }}</p>
    <p v-if="processes.length">流程預覽 {{ processes.join('、') }}</p>
    <table v-if="modules.length"><thead><tr><th>模組</th><th>版次</th><th>結果</th></tr></thead><tbody>
      <tr v-for="(row, index) in modules" :key="index"><td>{{ row.moduleNo }}</td><td>{{ row.moduleVersion }}</td><td>{{ results[index] || '待匯入' }}</td></tr>
    </tbody></table>
    <footer><button :disabled="busy || !modules.length || !!results.length" @click="results = props.importModules(modules)">匯入 DEV 主檔</button><button @click="emit('close')">關閉</button></footer>
  </section>
</template>

<style scoped>
.import-dialog{width:min(660px,95vw);max-height:90vh;overflow:auto;display:grid;gap:12px;padding:16px;background:white;border:1px solid #b8b3a2}header,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p{margin:0}table{width:100%;border-collapse:collapse;text-align:left}th,td{padding:5px;border:1px solid #dedbd1}th{background:#f4f2eb}
</style>
