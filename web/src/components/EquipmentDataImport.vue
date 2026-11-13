<script setup lang="ts">
import { computed, ref } from 'vue'

interface ImportRecord {
  equipmentType: string; equipmentNo: string; equipmentName: string; description: string
  engineerGroupNo: string; vendorNo: string; chamberFlag: boolean; chamberFlagRaw: string; belongEquipmentNo: string
}
interface ImportResult { rowNo: number; equipmentNo: string; equipmentType: string; chamberFlag: string; status: '待匯入' | '成功' | '失敗' | '已取消'; message: string }
interface ImportApiResult { rowNo: number; equipmentNo: string; equipmentType: string; chamberFlag: boolean | string; result: 'pending' | 'success' | 'fail' | 'cancelled'; message: string }
interface ImportJobSnapshot { total: number; completed: number; complete: boolean; cancelled: boolean; results: ImportApiResult[] }
interface EquipmentRecord {
  equipmentNo: string; equipmentType: string; equipmentName: string; engineerGroupNo: string; vendorNo: string
  modelNo: string; assetNo: string; equipmentClass: string; equipmentClassNo: string; description: string
  capacity: number; loadPort: number; autoFlag: number; eaController: string; eqpRecipe: number; qcListNo: string
  maxTime: number | null; fixEqpTime: number | null; varEqpTime: number | null; countEqpUnitQty: number
  countEquipmentUnitQty: number; queueServer: string; requestMsgQueue: string; responseMsgQueue: string
  requestMsgQueueMes: string; responseMsgQueueEap: string; chamberFlag: boolean; belongEquipmentNo: string | null
  issueState: number; dataStamp: number; creator: string; createdAt: string; reviser: string | null; revisedAt: string | null
}
const props = defineProps<{ apiBaseUrl: string; currentUserNo?: string }>()
const emit = defineEmits<{ close: []; updated: [] }>()
const mock = import.meta.env.DEV
const fileInput = ref<HTMLInputElement | null>(null)
const selectedFile = ref<File | null>(null)
const fileName = ref('')
const records = ref<ImportRecord[]>([])
const results = ref<ImportResult[]>([])
const loading = ref(false)
const importing = ref(false)
const cancelled = ref(false)
const importJobId = ref('')
const totalRows = ref(0)
const error = ref('')
const progress = computed(() => results.value.length ? results.value.filter((item) => item.status !== '待匯入').length : 0)
const requiredHeaders = ['EquipmentType', 'EquipmentNo', 'EquipmentName', 'Description', 'EngineerGroupNo', 'VendorNo', 'ChamberFlag', 'BelongEquipmentNo']

function apiUrl(path: string): string {
  const base = props.apiBaseUrl.trim()
  if (!base) throw new Error('目前環境尚未設定 API 位址。')
  return new URL(path.replace(/^\/+/, ''), `${base.replace(/\/+$/, '')}/`).toString()
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(apiUrl(path), { credentials: 'include', ...init })
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; detail?: string; message?: string } | null
    throw new Error(body?.detail || body?.message || body?.title || `API 回應錯誤 (${response.status})`)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}
function parseCsv(text: string): string[][] {
  const rows: string[][] = []; let row: string[] = []; let value = ''; let quoted = false
  const source = text.replace(/^\uFEFF/, '')
  for (let i = 0; i < source.length; i += 1) {
    const char = source[i]
    if (quoted && char === '"' && source[i + 1] === '"') { value += '"'; i += 1 }
    else if (char === '"') quoted = !quoted
    else if (!quoted && char === ',') { row.push(value); value = '' }
    else if (!quoted && (char === '\n' || char === '\r')) {
      if (char === '\r' && source[i + 1] === '\n') i += 1
      row.push(value); value = ''
      if (row.some((cell) => cell.trim())) rows.push(row)
      row = []
    } else value += char
  }
  if (value.length || row.length) { row.push(value); if (row.some((cell) => cell.trim())) rows.push(row) }
  return rows
}
function boolValue(value: string): boolean | null {
  const normalized = value.trim().toLocaleLowerCase()
  if (['1', 'true', 'y', 'yes'].includes(normalized)) return true
  if (['0', 'false', 'n', 'no'].includes(normalized)) return false
  return null
}
function recordFromCells(cells: string[], rowNo: number): ImportRecord {
  const chamber = boolValue(cells[6] ?? '')
  return {
    equipmentType: (cells[0] ?? '').trim(), equipmentNo: (cells[1] ?? '').trim().toUpperCase(),
    equipmentName: (cells[2] ?? '').trim(), description: (cells[3] ?? '').trim(),
    engineerGroupNo: (cells[4] ?? '').trim(), vendorNo: (cells[5] ?? '').trim(),
    chamberFlag: chamber === true, chamberFlagRaw: (cells[6] ?? '').trim(), belongEquipmentNo: (cells[7] ?? '').trim().toUpperCase(),
  }
}
async function chooseFile(event: Event): Promise<void> {
  error.value = ''; results.value = []; records.value = []; totalRows.value = 0; importJobId.value = ''
  const file = (event.target as HTMLInputElement).files?.[0]
  selectedFile.value = file ?? null; fileName.value = file?.name ?? ''
  if (!file) return
  if (mock) {
    if (!file.name.toLocaleLowerCase().endsWith('.csv')) { error.value = 'DEV 預覽目前只支援 CSV；正式 Excel (.xls/.xlsx) 由 API 端解析。'; selectedFile.value = null; return }
    loading.value = true
    try {
      const data = parseCsv(await file.text())
      records.value = data.slice(1).map((cells, index) => recordFromCells(cells, index + 2))
      totalRows.value = records.value.length
      results.value = records.value.map((item, index) => ({ rowNo: index + 2, equipmentNo: item.equipmentNo, equipmentType: item.equipmentType, chamberFlag: item.chamberFlag ? '是' : '否', status: '待匯入', message: '' }))
      if (!records.value.length) error.value = '檔案沒有可匯入資料列。'
    } catch { error.value = 'CSV 無法解析，請確認欄位以逗號分隔，文字欄位的逗號需加雙引號。' }
    finally { loading.value = false }
  }
}
async function downloadTemplate(): Promise<void> {
  if (!mock) {
    try {
      const response = await fetch(apiUrl('api/equipment/import/template'), { credentials: 'include' })
      if (!response.ok) throw new Error(`範本下載失敗 (${response.status})`)
      const blob = await response.blob(); const url = URL.createObjectURL(blob); const anchor = document.createElement('a')
      anchor.href = url; anchor.download = 'EquipmentTemplate.xlsx'; anchor.click(); URL.revokeObjectURL(url)
    } catch (e) { error.value = e instanceof Error ? e.message : '範本下載失敗。' }
    return
  }
  const content = `${requiredHeaders.join(',')}\r\n`
  const blob = new Blob(['\uFEFF', content], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob); const anchor = document.createElement('a')
  anchor.href = url; anchor.download = 'EquipmentTemplate.csv'; anchor.click(); URL.revokeObjectURL(url)
}
function delay(ms: number): Promise<void> { return new Promise((resolve) => window.setTimeout(resolve, ms)) }
function requiredError(item: ImportRecord): string {
  if (!item.equipmentType) return 'EquipmentType 為必填。'
  if (!item.equipmentNo) return 'EquipmentNo 為必填。'
  if (!item.equipmentName) return 'EquipmentName 為必填。'
  if (!item.engineerGroupNo) return 'EngineerGroupNo 為必填。'
  if (!item.vendorNo) return 'VendorNo 為必填。'
  if (boolValue(item.chamberFlagRaw) === null) return 'ChamberFlag 請使用 1/0、true/false 或 Y/N。'
  return ''
}
function buildEquipment(item: ImportRecord, now: string, userNo: string): EquipmentRecord {
  return {
    equipmentNo: item.equipmentNo, equipmentType: item.equipmentType, equipmentName: item.equipmentName,
    engineerGroupNo: item.engineerGroupNo, vendorNo: item.vendorNo, modelNo: '', assetNo: '',
    equipmentClass: item.chamberFlag ? 'CHAMBER' : 'GENERAL', equipmentClassNo: item.chamberFlag ? '2' : '0',
    description: item.description, capacity: -1, loadPort: -1, autoFlag: 0, eaController: 'N/A', eqpRecipe: 0,
    qcListNo: '', maxTime: null, fixEqpTime: 0, varEqpTime: 0, countEqpUnitQty: 1, countEquipmentUnitQty: 1,
    queueServer: '', requestMsgQueue: '', responseMsgQueue: '', requestMsgQueueMes: '', responseMsgQueueEap: '',
    chamberFlag: item.chamberFlag, belongEquipmentNo: item.chamberFlag ? item.belongEquipmentNo : null,
    issueState: 0, dataStamp: 0, creator: userNo, createdAt: now, reviser: null, revisedAt: null,
  }
}
function readMock<T>(key: string): T[] { try { return JSON.parse(localStorage.getItem(key) ?? '[]') as T[] } catch { return [] } }
function writeMock(key: string, rows: EquipmentRecord[]): void { localStorage.setItem(key, JSON.stringify(rows)) }
async function importDev(): Promise<void> {
  const equipmentKey = 'ymes-dev-mock:equipment:v1'; const chambersKey = 'ymes-dev-mock:equipment-chambers:v1'
  const mains = readMock<EquipmentRecord>(equipmentKey); const chambers = readMock<EquipmentRecord>(chambersKey)
  const processOrder = records.value.map((_, index) => index).sort((a, b) => Number(records.value[a].chamberFlag) - Number(records.value[b].chamberFlag))
  for (let step = 0; step < processOrder.length; step += 1) {
    const i = processOrder[step]
    if (cancelled.value) { processOrder.slice(step).forEach((index) => { if (results.value[index].status === '待匯入') results.value[index].status = '已取消' }); break }
    const source = records.value[i]; const result = results.value[i]
    await delay(35)
    const message = requiredError(source)
    if (message) { result.status = '失敗'; result.message = message; continue }
    const alreadyExists = [...mains, ...chambers].some((item) => item.equipmentNo === source.equipmentNo)
    if (alreadyExists || results.value.slice(0, i).some((item) => item.equipmentNo === source.equipmentNo && item.status === '成功')) { result.status = '失敗'; result.message = '設備編號已存在或在檔案內重複。'; continue }
    if (source.chamberFlag && !source.belongEquipmentNo) { result.status = '失敗'; result.message = 'Chamber 的 BelongEquipmentNo 為必填。'; continue }
    if (source.chamberFlag && ![...mains, ...chambers].some((item) => item.equipmentNo === source.belongEquipmentNo && !item.chamberFlag)) { result.status = '失敗'; result.message = '找不到所屬主設備；DEV 匯入要求父設備已存在。'; continue }
    const item = buildEquipment(source, new Date().toISOString(), props.currentUserNo || 'DEV001')
    if (item.chamberFlag) { chambers.push(item); writeMock(chambersKey, chambers) }
    else { mains.push(item); writeMock(equipmentKey, mains) }
    result.status = '成功'; result.message = 'DEV 假資料已寫入瀏覽器。'
  }
  if (results.value.some((item) => item.status === '成功')) emit('updated')
}
async function startImport(): Promise<void> {
  if (!selectedFile.value) { error.value = '請先選擇匯入檔案。'; return }
  error.value = ''; cancelled.value = false; importing.value = true
  try {
    if (mock) {
      if (!records.value.length) throw new Error('請選擇有資料列的 CSV 檔案。')
      await importDev()
    } else {
      const formData = new FormData(); formData.append('file', selectedFile.value)
      const job = await request<{ jobId: string; total: number }>('api/equipment/import', { method: 'POST', body: formData })
      importJobId.value = job.jobId
      totalRows.value = job.total
      if (cancelled.value) await request<void>(`api/equipment/import/${encodeURIComponent(job.jobId)}/cancel`, { method: 'POST' })
      while (true) {
        const snapshot = await request<ImportJobSnapshot>(`api/equipment/import/${encodeURIComponent(job.jobId)}`)
        results.value = snapshot.results.map((item) => ({
          rowNo: item.rowNo, equipmentNo: item.equipmentNo, equipmentType: item.equipmentType,
          chamberFlag: typeof item.chamberFlag === 'boolean' ? (item.chamberFlag ? '是' : '否') : item.chamberFlag,
          status: item.result === 'success' ? '成功' : item.result === 'fail' ? '失敗' : item.result === 'cancelled' ? '已取消' : '待匯入',
          message: item.message,
        }))
        if (snapshot.complete) {
          if (snapshot.results.some((item) => item.result === 'success')) emit('updated')
          break
        }
        await delay(500)
      }
    }
  } catch (e) { if (!cancelled.value) error.value = e instanceof Error ? e.message : '設備資料匯入失敗。' }
  finally { importing.value = false }
}
function cancel(): void {
  cancelled.value = true
  if (!mock && importJobId.value) {
    void request<void>(`api/equipment/import/${encodeURIComponent(importJobId.value)}/cancel`, { method: 'POST' })
      .catch((e) => { error.value = e instanceof Error ? e.message : '取消匯入工作失敗。' })
  }
}
</script>

<template>
  <div class="eqp-import-overlay" @click.self="!importing && emit('close')">
    <section class="eqp-import-window" role="dialog" aria-modal="true" aria-label="設備資料匯入">
      <header><strong>Equipment Data Import</strong><button type="button" aria-label="關閉" :disabled="importing" @click="emit('close')">×</button></header>
      <p v-if="mock" class="eqp-import-dev-note">DEV 假資料｜僅支援 CSV 預覽，匯入結果寫入瀏覽器 localStorage，不會寫入資料庫。正式環境的 .xls/.xlsx 由 API 解析。</p>
      <p class="eqp-import-info">只讀第一個工作表／資料表；資料列依固定欄位順序讀取。WinForms 必填 EquipmentType、EquipmentNo、EquipmentName、EngineerGroupNo、VendorNo、ChamberFlag；Description 與 BelongEquipmentNo 可空。</p>
      <div class="eqp-import-file"><label>檔案路徑 <input :value="fileName" readonly placeholder="請選擇檔案"></label><input ref="fileInput" class="hidden-file" type="file" :accept="mock ? '.csv,text/csv' : '.xls,.xlsx'" @change="chooseFile"><button type="button" :disabled="importing" @click="fileInput?.click()">瀏覽…</button></div>
      <p v-if="error" class="eqp-import-error" role="alert">{{ error }}</p>
      <div class="eqp-import-progress"><progress :value="progress" :max="Math.max(totalRows, 1)"></progress><span>{{ progress }}/{{ totalRows }}</span><button type="button" :disabled="!importing" @click="cancel">Cancel</button></div>
      <div class="eqp-import-result-wrap"><table><thead><tr><th>列</th><th>EquipmentNo</th><th>EquipmentType</th><th>ChamberFlag</th><th>結果</th><th>訊息</th></tr></thead><tbody><tr v-for="item in results" :key="item.rowNo" :class="item.status === '成功' ? 'success' : item.status === '失敗' ? 'failure' : ''"><td>{{ item.rowNo }}</td><td>{{ item.equipmentNo }}</td><td>{{ item.equipmentType }}</td><td>{{ item.chamberFlag }}</td><td>{{ item.status }}</td><td>{{ item.message }}</td></tr><tr v-if="loading"><td colspan="6">檔案讀取中…</td></tr><tr v-if="!loading && !results.length"><td colspan="6" class="empty">選取檔案後顯示逐筆結果</td></tr></tbody></table></div>
      <footer><button type="button" :disabled="importing" @click="downloadTemplate">Template Download{{ mock ? ' (CSV)' : '' }}</button><span class="spacer"></span><button class="primary" type="button" :disabled="importing || !selectedFile" @click="startImport">Import</button><button type="button" :disabled="importing" @click="emit('close')">Close</button></footer>
    </section>
  </div>
</template>

<style scoped>
.eqp-import-overlay{position:fixed;inset:0;z-index:34;display:grid;place-items:center;padding:12px;background:#0005;font:13px 'Segoe UI','Microsoft JhengHei',sans-serif;color:#333}.eqp-import-window{display:flex;width:min(940px,98vw);height:min(700px,94vh);flex-direction:column;gap:10px;padding:14px;border:1px solid #aaa;background:#fff;box-shadow:0 8px 28px #0003}.eqp-import-window>header{display:flex;align-items:center;justify-content:space-between;font-size:16px}.eqp-import-window>header button{width:30px;height:28px;border:1px solid #ccc;background:#fafafa;font-size:20px}.eqp-import-dev-note{margin:0;padding:7px 9px;border:1px solid #d8c577;background:#fff6c8;color:#66551b}.eqp-import-info{margin:0;padding:8px;border:1px solid #ddd;background:#fafafa;line-height:1.55}.eqp-import-file{display:flex;gap:8px;align-items:center}.eqp-import-file label{display:flex;flex:1;align-items:center;gap:10px}.eqp-import-file label input{flex:1;height:32px;border:1px solid #aaa;padding:5px 7px}.eqp-import-file button,.eqp-import-progress button,.eqp-import-window footer button{height:34px;padding:4px 11px;border:1px solid #c7c7c7;background:#fafafa;font:inherit}.hidden-file{display:none}.eqp-import-error{margin:0;padding:7px;border:1px solid #d4a3a3;background:#fff0f0;color:#7a2424}.eqp-import-progress{display:flex;align-items:center;gap:8px}.eqp-import-progress progress{flex:1;height:22px;accent-color:#ffa62b}.eqp-import-progress span{min-width:60px;text-align:right;color:#555}.eqp-import-result-wrap{flex:1;min-height:140px;overflow:auto;border:1px solid #aaa}.eqp-import-result-wrap table{width:100%;border-collapse:collapse}.eqp-import-result-wrap th{position:sticky;top:0;background:#e7c600;color:#26251f;text-align:left}.eqp-import-result-wrap th,.eqp-import-result-wrap td{padding:6px 8px;border:1px solid #ccc}.eqp-import-result-wrap tbody tr.success{background:#eff9ef}.eqp-import-result-wrap tbody tr.failure{background:#fff0f0}.eqp-import-result-wrap .empty{text-align:center;color:#777;padding:32px}.eqp-import-window>footer{display:flex;gap:8px}.eqp-import-window footer .spacer{flex:1}.eqp-import-window footer .primary{min-width:100px;border-color:#f0a126;background:#ffa62b;color:#fff}.eqp-import-window button:disabled{color:#aaa}@media(max-width:620px){.eqp-import-file{flex-wrap:wrap}.eqp-import-file label{flex-basis:100%}.eqp-import-result-wrap table{min-width:650px}}
</style>
