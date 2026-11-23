<script setup lang="ts">
import { computed, ref } from 'vue'

type Factor = { factorNo: string; name: string; value: string }
type QcBinding = { qcItemNo: string; main: boolean; factors: Factor[] }
type ReportCode = { reportNo: string; bindings: QcBinding[] }
type EditorMode = 'add' | 'edit' | 'copy'
type NoticeKind = 'info' | 'success' | 'error'

const props = defineProps<{ developmentPreview: boolean }>()
const emit = defineEmits<{ close: [] }>()

const qcItems = [
  { no: 'VAR-DEV-101', name: '軸心直徑', dataType: 'InLine' },
  { no: 'VAR-DEV-202', name: '壓合高度', dataType: 'Particle' },
  { no: 'VAR-DEV-305', name: '端面平面度', dataType: 'OffLine' },
  { no: 'VAR-DEV-330', name: '鍍層厚度', dataType: 'InLine' },
  { no: 'VAR-OLD-008', name: '舊型孔徑', dataType: 'InLine · IssueState -1' },
]
const factorOptions: Record<string, Array<{ factorNo: string; name: string; values: string[] }>> = {
  'VAR-DEV-101': [
    { factorNo: 'OP', name: '作業站', values: ['OP-20', 'OP-35', 'OP-50'] },
    { factorNo: 'EQUIPMENT', name: '設備', values: ['EQ-020', 'EQ-035'] },
  ],
  'VAR-DEV-202': [
    { factorNo: 'OP', name: '作業站', values: ['OP-20', 'OP-35', 'OP-50'] },
    { factorNo: 'CUSTOMER', name: '客戶', values: ['CUS-DEV-01', 'CUS-DEV-02'] },
  ],
  'VAR-DEV-305': [
    { factorNo: 'PRODUCT', name: '產品', values: ['PRD-AX17', 'PRD-PH08'] },
    { factorNo: 'OP', name: '作業站', values: ['OP-10', 'OP-20'] },
  ],
  'VAR-DEV-330': [
    { factorNo: 'CUSTOMER', name: '客戶', values: ['CUS-DEV-01', 'CUS-DEV-03'] },
    { factorNo: 'LOT_TYPE', name: '批別', values: ['DEV-A', 'DEV-B'] },
  ],
  'VAR-OLD-008': [{ factorNo: 'OP', name: '作業站', values: ['OP-10', 'OP-20'] }],
}

const reportCodes = ref<ReportCode[]>([
  {
    reportNo: 'MCR-VAR-001',
    bindings: [
      { qcItemNo: 'VAR-DEV-101', main: true, factors: [{ factorNo: 'OP', name: '作業站', value: 'OP-20' }, { factorNo: 'EQUIPMENT', name: '設備', value: 'EQ-020' }] },
      { qcItemNo: 'VAR-DEV-202', main: false, factors: [{ factorNo: 'OP', name: '作業站', value: 'OP-20' }, { factorNo: 'EQUIPMENT', name: '設備', value: 'EQ-020' }] },
    ],
  },
  {
    reportNo: 'MCR-VAR-002',
    bindings: [
      { qcItemNo: 'VAR-DEV-305', main: false, factors: [{ factorNo: 'PRODUCT', name: '產品', value: 'PRD-AX17' }] },
    ],
  },
  {
    reportNo: 'MCR-VAR-003',
    bindings: [
      { qcItemNo: 'VAR-OLD-008', main: false, factors: [] },
    ],
  },
])

const searchText = ref('')
const selectedNos = ref<string[]>([])
const activeReportNo = ref('MCR-VAR-001')
const editorMode = ref<EditorMode | null>(null)
const draftReportNo = ref('')
const draftQcItemNos = ref<string[]>([])
const combineQcItems = ref(false)
const draftMainQcItemNo = ref('')
const draftFactors = ref<Factor[]>([])
const pendingDeleteNos = ref<string[]>([])
const notice = ref('清單使用本機 DEV 假資料；變更不會寫入正式資料。')
const noticeKind = ref<NoticeKind>('info')

const filteredCodes = computed(() => {
  const keyword = searchText.value.trim().toUpperCase()
  if (!keyword) return reportCodes.value
  return reportCodes.value.filter((code) => code.reportNo.includes(keyword) || code.bindings.some((binding) => binding.qcItemNo.includes(keyword)))
})
const activeCode = computed(() => reportCodes.value.find((code) => code.reportNo === activeReportNo.value) ?? null)
const draftQcChoices = computed(() => draftQcItemNos.value.map((no) => qcItems.find((item) => item.no === no)).filter((item): item is (typeof qcItems)[number] => Boolean(item)))
const qcItemOptions = computed(() => qcItems)

function qcLabel(qcItemNo: string): string {
  const item = qcItems.find((candidate) => candidate.no === qcItemNo)
  return item ? `${item.no} · ${item.name}` : qcItemNo
}
function factorNamesFor(items: string[]): Factor[] {
  const seen = new Map<string, Factor>()
  items.forEach((qcItemNo) => {
    factorOptions[qcItemNo]?.forEach((factor) => {
      if (!seen.has(factor.factorNo)) seen.set(factor.factorNo, { factorNo: factor.factorNo, name: factor.name, value: '' })
    })
  })
  return [...seen.values()]
}
function syncFactors(): void {
  const oldValues = new Map(draftFactors.value.map((factor) => [factor.factorNo, factor.value]))
  draftFactors.value = factorNamesFor(draftQcItemNos.value).map((factor) => ({ ...factor, value: oldValues.get(factor.factorNo) ?? '' }))
  if (!draftQcItemNos.value.includes(draftMainQcItemNo.value)) draftMainQcItemNo.value = ''
  if (draftQcItemNos.value.length < 2) {
    combineQcItems.value = false
    draftMainQcItemNo.value = ''
  }
}
function openEditor(mode: EditorMode, source?: ReportCode): void {
  editorMode.value = mode
  draftReportNo.value = mode === 'edit' && source ? source.reportNo : ''
  draftQcItemNos.value = source?.bindings.map((binding) => binding.qcItemNo) ?? []
  combineQcItems.value = Boolean(source?.bindings.some((binding) => binding.main))
  draftMainQcItemNo.value = source?.bindings.find((binding) => binding.main)?.qcItemNo ?? ''
  draftFactors.value = factorNamesFor(draftQcItemNos.value).map((factor) => ({
    ...factor,
    value: source?.bindings.flatMap((binding) => binding.factors).find((saved) => saved.factorNo === factor.factorNo)?.value ?? '',
  }))
  if (source && draftFactors.value.length === 0) draftFactors.value = []
  notice.value = mode === 'edit'
    ? '編輯模式沿用舊畫面的固定 ReportNo／QC Item；選取主 QC Item 並調整 Factor。'
    : mode === 'copy' ? '複製會帶入 QC Item 與 Factor；請輸入新的 ReportNo。' : '選擇 QC Item 後可設定共用 Factor。'
  noticeKind.value = 'info'
}
function closeEditor(): void { editorMode.value = null }
function toggleQcItem(qcItemNo: string, checked: boolean): void {
  draftQcItemNos.value = checked
    ? [...draftQcItemNos.value, qcItemNo]
    : draftQcItemNos.value.filter((item) => item !== qcItemNo)
  syncFactors()
}
function setFactorValue(factorNo: string, value: string): void {
  const factor = draftFactors.value.find((item) => item.factorNo === factorNo)
  if (factor) factor.value = value
}
function factorValueOptions(factorNo: string): string[] {
  return [...new Set(draftQcItemNos.value.flatMap((qcItemNo) => factorOptions[qcItemNo]?.find((factor) => factor.factorNo === factorNo)?.values ?? []))]
}
function saveEditor(): void {
  const mode = editorMode.value
  const reportNo = draftReportNo.value.trim().toUpperCase()
  if (!reportNo) {
    notice.value = '請輸入 ReportNo。'
    noticeKind.value = 'error'
    return
  }
  if (draftQcItemNos.value.length === 0) {
    notice.value = '請至少選擇一個 QC Item。'
    noticeKind.value = 'error'
    return
  }
  if (combineQcItems.value && draftQcItemNos.value.length > 1 && !draftMainQcItemNo.value) {
    notice.value = '合併多個 QC Item 時，請指定 Main QC Item。'
    noticeKind.value = 'error'
    return
  }
  if (editorMode.value !== 'edit' && reportCodes.value.some((code) => code.reportNo === reportNo)) {
    notice.value = `ReportNo ${reportNo} 已存在。`
    noticeKind.value = 'error'
    return
  }

  const factors = draftFactors.value.filter((factor) => factor.value.trim() !== '').map((factor) => ({ ...factor, value: factor.value.trim().toUpperCase() }))
  const bindings = draftQcItemNos.value.map((qcItemNo) => ({
    qcItemNo,
    main: combineQcItems.value && qcItemNo === draftMainQcItemNo.value,
    factors: factors.map((factor) => ({ ...factor })),
  }))
  if (editorMode.value === 'edit') {
    const index = reportCodes.value.findIndex((code) => code.reportNo === reportNo)
    if (index >= 0) reportCodes.value.splice(index, 1, { reportNo, bindings })
  } else {
    reportCodes.value.unshift({ reportNo, bindings })
  }
  activeReportNo.value = reportNo
  selectedNos.value = [reportNo]
  editorMode.value = null
  notice.value = `${reportNo} 已在 DEV 記憶體清單${mode === 'edit' ? '更新' : '新增'}；重新整理頁面會還原樣本資料。`
  noticeKind.value = 'success'
}
function toggleSelected(reportNo: string, checked: boolean): void {
  selectedNos.value = checked
    ? [...new Set([...selectedNos.value, reportNo])]
    : selectedNos.value.filter((item) => item !== reportNo)
  activeReportNo.value = reportNo
}
function activateRow(reportNo: string): void {
  activeReportNo.value = reportNo
  if (!selectedNos.value.includes(reportNo)) selectedNos.value = [reportNo]
}
const selectedCode = computed(() => reportCodes.value.find((code) => code.reportNo === selectedNos.value[0]) ?? null)
function requestDelete(): void {
  pendingDeleteNos.value = selectedNos.value.length > 0 ? [...selectedNos.value] : activeCode.value ? [activeCode.value.reportNo] : []
}
function confirmDelete(): void {
  const removed = new Set(pendingDeleteNos.value)
  reportCodes.value = reportCodes.value.filter((code) => !removed.has(code.reportNo))
  selectedNos.value = []
  activeReportNo.value = reportCodes.value[0]?.reportNo ?? ''
  notice.value = `已從 DEV 記憶體移除 ${removed.size} 組編碼。`
  noticeKind.value = 'success'
  pendingDeleteNos.value = []
}
function modeTitle(): string {
  return editorMode.value === 'edit' ? '編輯月報表編碼' : editorMode.value === 'copy' ? '複製月報表編碼' : '新增月報表編碼'
}
</script>

<template>
  <section v-if="props.developmentPreview" class="month-code-page" aria-label="SPC 管制圖月報表編碼">
    <header class="page-head">
      <div><p class="eyebrow">SPC / SC302</p><h1>管制圖月報表編碼</h1><p class="subtitle">維護月報表使用的 ReportNo、QC Item、主項與 Factor 條件。</p></div>
      <button class="button" type="button" @click="emit('close')">Close (X)</button>
    </header>

    <div class="notice" :class="noticeKind" role="status">{{ notice }}</div>
    <div class="toolbar">
      <label class="search"><span>查詢 ReportNo／QC Item</span><input v-model="searchText" type="search" placeholder="輸入關鍵字"></label>
      <div class="actions">
        <button class="button primary" type="button" @click="openEditor('add')">ADD</button>
        <button class="button" type="button" :disabled="selectedNos.length !== 1 || !selectedCode" @click="openEditor('edit', selectedCode ?? undefined)">EDIT</button>
        <button class="button" type="button" :disabled="selectedNos.length !== 1 || !selectedCode" @click="openEditor('copy', selectedCode ?? undefined)">Copy</button>
        <button class="button danger" type="button" :disabled="selectedNos.length === 0" @click="requestDelete">DEL</button>
      </div>
    </div>

    <div class="workspace">
      <section class="list-panel panel">
        <div class="panel-heading"><div><h2>Monthly Report Code</h2><p>{{ filteredCodes.length }} 組編碼</p></div><span class="readonly-badge">DEV 假資料</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th class="select-col">選取</th><th>ReportNo</th><th>QC Item</th><th>Main</th><th>Factor 條件</th></tr></thead>
            <tbody>
              <tr v-for="code in filteredCodes" :key="code.reportNo" :class="{ active: activeReportNo === code.reportNo }" @click="activateRow(code.reportNo)">
                <td class="select-col"><input type="checkbox" :checked="selectedNos.includes(code.reportNo)" :aria-label="`選取 ${code.reportNo}`" @click.stop @change="toggleSelected(code.reportNo, ($event.target as HTMLInputElement).checked)"></td>
                <td><button class="link-button" type="button" @click.stop="activateRow(code.reportNo)">{{ code.reportNo }}</button></td>
                <td><div v-for="binding in code.bindings" :key="binding.qcItemNo" class="item-line">{{ qcLabel(binding.qcItemNo) }}</div></td>
                <td><span v-for="binding in code.bindings.filter((item) => item.main)" :key="binding.qcItemNo" class="main-chip">{{ binding.qcItemNo }}</span><span v-if="!code.bindings.some((item) => item.main)" class="muted">—</span></td>
                <td><div v-if="code.bindings.some((binding) => binding.factors.length)" class="factor-summary">{{ [...new Set(code.bindings.flatMap((binding) => binding.factors.map((factor) => `${factor.factorNo}=${factor.value}`)))].join(' · ') }}</div><span v-else class="muted">無（Core 儲存 FactorNo=NA）</span></td>
              </tr>
              <tr v-if="filteredCodes.length === 0"><td colspan="5" class="empty">查無符合的月報表編碼。</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="detail-panel panel">
        <div class="panel-heading"><div><h2>編碼明細</h2><p>{{ activeCode?.reportNo ?? '尚未選取' }}</p></div><span v-if="activeCode" class="readonly-badge">{{ activeCode.bindings.length }} 個 QC Item</span></div>
        <template v-if="activeCode">
          <div v-for="binding in activeCode.bindings" :key="binding.qcItemNo" class="binding-card">
            <div class="binding-title"><div><strong>{{ qcLabel(binding.qcItemNo) }}</strong><span class="subline">{{ qcItems.find((item) => item.no === binding.qcItemNo)?.dataType }}</span></div><span v-if="binding.main" class="main-chip">Main QC Item</span></div>
            <table class="factor-table"><thead><tr><th>FactorNo</th><th>Factor 名稱</th><th>FactorValue</th></tr></thead>
              <tbody><tr v-for="factor in binding.factors" :key="factor.factorNo"><td>{{ factor.factorNo }}</td><td>{{ factor.name }}</td><td>{{ factor.value }}</td></tr><tr v-if="binding.factors.length === 0"><td colspan="3" class="muted">未設定 Factor（對應 NA）</td></tr></tbody>
            </table>
          </div>
        </template>
        <div v-else class="empty">選取清單中的編碼檢視 QC Item 與 Factor 明細。</div>
      </section>
    </div>

    <div v-if="editorMode" class="overlay" role="presentation" @click.self="closeEditor">
      <section class="dialog" role="dialog" aria-modal="true" aria-labelledby="editor-title">
        <header class="dialog-head"><div><p class="eyebrow">SC302 · DEV</p><h2 id="editor-title">{{ modeTitle() }}</h2></div><button class="icon-button" type="button" aria-label="關閉" @click="closeEditor">×</button></header>
        <div class="dialog-body">
          <label class="field"><span>ReportNo <b>*</b></span><input v-model="draftReportNo" :readonly="editorMode === 'edit'" autocomplete="off" @blur="draftReportNo = draftReportNo.toUpperCase()"></label>
          <section class="field-block"><div class="field-heading"><span>QC Item <b>*</b></span><small>限 ChartControl=1；IssueState 2／-1。清單只供 DEV 模擬。</small></div>
            <div class="qc-picker">
              <label v-for="item in qcItemOptions" :key="item.no" class="check-option"><input type="checkbox" :checked="draftQcItemNos.includes(item.no)" :disabled="editorMode === 'edit'" @change="toggleQcItem(item.no, ($event.target as HTMLInputElement).checked)"><span><strong>{{ item.no }}</strong><small>{{ item.name }} · {{ item.dataType }}</small></span></label>
            </div>
            <p v-if="draftQcChoices.length === 0" class="helper">尚未選擇 QC Item。</p>
          </section>
          <label v-if="draftQcItemNos.length > 1" class="combine-toggle"><input v-model="combineQcItems" type="checkbox" @change="syncFactors"> 將多個 QC Item 合併在同一個月報表編碼</label>
          <label v-if="combineQcItems && draftQcItemNos.length > 1" class="field"><span>Main QC Item <b>*</b></span><select v-model="draftMainQcItemNo"><option value="">選擇主 QC Item</option><option v-for="item in draftQcChoices" :key="item.no" :value="item.no">{{ item.no }} · {{ item.name }}</option></select></label>
          <section class="field-block"><div class="field-heading"><span>Factor 條件</span><small>依所選 QC Item 合併 Factor；只保存有值的條件。</small></div>
            <div v-if="draftFactors.length" class="factor-editor"><label v-for="factor in draftFactors" :key="factor.factorNo" class="factor-row"><span><strong>{{ factor.factorNo }}</strong><small>{{ factor.name }}</small></span><input :value="factor.value" :list="`factor-options-${factor.factorNo}`" placeholder="輸入或選取 FactorValue" @input="setFactorValue(factor.factorNo, ($event.target as HTMLInputElement).value)"><datalist :id="`factor-options-${factor.factorNo}`"><option v-for="option in factorValueOptions(factor.factorNo)" :key="option" :value="option"></option></datalist></label></div>
            <p v-else class="helper">選取 QC Item 後載入可用 Factor。</p>
          </section>
        </div>
        <footer class="dialog-actions"><span>舊畫面新增會為每個 QC Item × Factor 寫入明細列。</span><button class="button" type="button" @click="closeEditor">Cancel</button><button class="button primary" type="button" @click="saveEditor">{{ editorMode === 'edit' ? 'Apply' : '確定' }}</button></footer>
      </section>
    </div>

    <div v-if="pendingDeleteNos.length" class="overlay" role="presentation" @click.self="pendingDeleteNos = []">
      <section class="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="delete-title"><p class="eyebrow">SC302 · DEV</p><h2 id="delete-title">刪除月報表編碼？</h2><p>將移除 {{ pendingDeleteNos.length }} 組編碼及其 QC Item／Factor 明細。此動作只影響目前頁面的 DEV 假資料。</p><div class="dialog-actions"><button class="button" type="button" @click="pendingDeleteNos = []">取消</button><button class="button danger" type="button" @click="confirmDelete">確認刪除</button></div></section>
    </div>
  </section>
</template>

<style scoped>
.month-code-page{height:100%;min-height:0;box-sizing:border-box;padding:16px;display:flex;flex-direction:column;gap:12px;background:#f5f4ef;color:#25251f;font:inherit;overflow:auto}.page-head,.toolbar,.panel-heading,.dialog-head,.dialog-actions,.binding-title{display:flex;align-items:center;justify-content:space-between;gap:12px}.page-head h1{margin:1px 0;font-size:23px}.eyebrow{margin:0;color:#7b765f;font-size:11px;font-weight:700;letter-spacing:.09em}.subtitle,.panel-heading p{margin:3px 0 0;color:#777365;font-size:12px}.button{min-height:32px;padding:5px 12px;border:1px solid #aaa795;border-radius:4px;background:#fff;color:#292820;font:inherit;font-size:12px;cursor:pointer}.button:hover:not(:disabled){border-color:#39382f;background:#efeee7}.button:disabled{opacity:.45;cursor:not-allowed}.button.primary{border-color:#31483d;background:#31483d;color:#fff}.button.primary:hover:not(:disabled){background:#25382f}.button.danger{border-color:#a54f3d;color:#8e382b}.button.danger:hover:not(:disabled){background:#fff1ed}.notice{border:1px solid #d5d1bf;border-radius:4px;padding:8px 11px;background:#fbfaf5;color:#5b594b;font-size:12px}.notice.success{border-color:#b9d0bf;background:#f1f8f2;color:#315b3e}.notice.error{border-color:#dfb5aa;background:#fff5f2;color:#8d382b}.toolbar{align-items:flex-end}.search{display:grid;gap:4px;max-width:350px;width:100%;font-size:11px;color:#686454}.search input,.field input,.field select,.factor-row select,.factor-row input{box-sizing:border-box;width:100%;min-height:32px;border:1px solid #cbc8bb;border-radius:3px;padding:5px 8px;background:#fff;color:#292820;font:inherit;font-size:12px}.actions{display:flex;gap:6px;flex-wrap:wrap}.workspace{display:grid;grid-template-columns:minmax(0,1.25fr) minmax(310px,.85fr);gap:12px;min-height:0;flex:1}.panel{border:1px solid #d4d1c5;border-radius:5px;background:#fff;min-width:0;display:flex;flex-direction:column;overflow:hidden}.panel-heading{padding:12px 14px;border-bottom:1px solid #e4e1d8}.panel-heading h2{margin:0;font-size:14px}.panel-heading p{font-size:11px}.readonly-badge{border-radius:12px;background:#f0eee4;color:#696551;padding:4px 8px;font-size:10px;white-space:nowrap}.table-wrap{overflow:auto;min-height:140px}.table-wrap table,.factor-table{width:100%;border-collapse:collapse;font-size:11px}.table-wrap th,.table-wrap td,.factor-table th,.factor-table td{padding:8px 9px;border-bottom:1px solid #eeece5;text-align:left;vertical-align:top}.table-wrap th,.factor-table th{position:sticky;top:0;background:#f8f7f2;color:#686454;font-size:10px;font-weight:700;z-index:1}.table-wrap tbody tr{cursor:pointer}.table-wrap tbody tr:hover,.table-wrap tbody tr.active{background:#f4f6f0}.select-col{width:44px;text-align:center!important}.link-button{border:0;background:none;color:#315d47;font:inherit;font-weight:700;text-decoration:underline;cursor:pointer;padding:0}.item-line{padding:2px 0;white-space:nowrap}.main-chip{display:inline-block;border-radius:3px;background:#eaf2eb;color:#315d40;padding:3px 6px;font-size:10px;white-space:nowrap}.factor-summary{max-width:240px;line-height:1.6;color:#525144}.muted,.empty{color:#969183}.empty{text-align:center;padding:24px!important}.detail-panel{padding-bottom:10px;overflow:auto}.binding-card{margin:10px 12px 0;border:1px solid #e1ded4;border-radius:4px;overflow:hidden}.binding-title{padding:9px 10px;background:#f8f7f2;font-size:11px}.binding-title>div{display:grid;gap:3px}.subline{font-size:10px;color:#827d6f}.factor-table th{position:static}.overlay{position:fixed;inset:0;z-index:1000;background:#16171180;display:flex;align-items:center;justify-content:center;padding:20px}.dialog,.confirm-dialog{width:min(760px,100%);max-height:calc(100vh - 40px);overflow:auto;border:1px solid #d4d0c3;border-radius:7px;background:#fff;color:#25251f;box-shadow:0 18px 55px #0003}.dialog-head{padding:14px 17px;border-bottom:1px solid #e6e2d8}.dialog-head h2,.confirm-dialog h2{margin:3px 0 0;font-size:18px}.icon-button{width:30px;height:30px;border:0;background:transparent;font-size:23px;color:#777;cursor:pointer}.dialog-body{padding:15px 17px;display:grid;gap:14px}.field{display:grid;gap:5px;font-size:11px;color:#605d50}.field b{color:#9b392e}.field-block{display:grid;gap:7px}.field-heading{display:flex;justify-content:space-between;gap:12px;align-items:baseline;font-size:11px;color:#605d50}.field-heading small,.helper{font-size:10px;color:#898474}.helper{margin:0}.qc-picker{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px;max-height:150px;overflow:auto;border:1px solid #e1ded4;border-radius:4px;padding:8px}.check-option{display:flex;gap:7px;align-items:flex-start;padding:6px;border-radius:3px;background:#faf9f5;font-size:11px}.check-option>span{display:grid;gap:3px}.check-option small{font-size:10px;color:#817c6d}.combine-toggle{display:flex;gap:7px;align-items:center;font-size:11px;color:#4b493d}.factor-editor{display:grid;gap:6px}.factor-row{display:grid;grid-template-columns:minmax(150px,1fr) minmax(190px,1.2fr);gap:8px;align-items:center;border:1px solid #ece9e0;padding:7px;border-radius:4px}.factor-row>span{display:grid;gap:2px;font-size:11px}.factor-row small{font-size:10px;color:#827d6f}.dialog-actions{padding:11px 17px;border-top:1px solid #e6e2d8;justify-content:flex-end}.dialog-actions>span{margin-right:auto;font-size:10px;color:#898474}.confirm-dialog{max-width:440px;padding:18px}.confirm-dialog>p:not(.eyebrow){font-size:12px;line-height:1.6;color:#686454}.confirm-dialog .dialog-actions{margin:14px -18px -18px}.confirm-dialog .dialog-actions>span{display:none}@media(max-width:900px){.workspace{grid-template-columns:1fr}.detail-panel{min-height:240px}.month-code-page{height:auto;min-height:100%}}@media(max-width:620px){.page-head,.toolbar{align-items:stretch;flex-direction:column}.actions{display:grid;grid-template-columns:repeat(2,1fr)}.qc-picker{grid-template-columns:1fr}.field-heading{align-items:flex-start;flex-direction:column}.factor-row{grid-template-columns:1fr}.dialog-actions{flex-wrap:wrap}.dialog-actions>span{width:100%}}
</style>
