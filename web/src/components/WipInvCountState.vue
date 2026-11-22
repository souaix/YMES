<script setup lang="ts">
import { computed, ref } from 'vue'

type CloseType = 'WIP' | 'RAW' | 'FGD' | 'SEMI' | 'MTLWIP' | 'SEMIWIP'
type StartingCase = 'awaiting-close' | 'awaiting-open'
type FailureCase = 'success' | 'close-snapshot-failure' | 'open-parameter-failure'
type CountState = {
  countSerial: string
  state: 0 | 1
  countDate: string
  creator: string
  createDate: string
  openUserNo: string
  openDate: string
  closeOptions: Record<CloseType, 0 | 1>
  snapshotCounts: Record<CloseType, number>
}

const props = defineProps<{ developmentPreview?: boolean }>()
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const closeOptions: { value: CloseType; label: string; source: string }[] = [
  { value: 'WIP', label: 'WIP 生產批', source: '線上生產批' },
  { value: 'RAW', label: 'RAW 原物料庫', source: '原物料庫' },
  { value: 'FGD', label: 'FGD 成品庫', source: '成品庫' },
  { value: 'SEMI', label: 'SEMI 半成品庫', source: '半成品庫' },
  { value: 'MTLWIP', label: 'MTL WIP 原物料', source: '線邊倉原物料' },
  { value: 'SEMIWIP', label: 'SEMI WIP 半成品', source: '線邊倉半成品' },
]
const snapshotCounts: Record<CloseType, number> = { WIP: 1264, RAW: 48, FGD: 36, SEMI: 12, MTLWIP: 87, SEMIWIP: 45 }
const emptyOptions = (): Record<CloseType, boolean> => ({ WIP: false, RAW: false, FGD: false, SEMI: false, MTLWIP: false, SEMIWIP: false })
const options = ref(emptyOptions())
const startingCase = ref<StartingCase>('awaiting-close')
const failureCase = ref<FailureCase>('success')
const unresolvedAdjustments = ref(false)
const executionAllowed = ref(true)
const record = ref<CountState | null>(null)
const serialSequence = ref(2)
const pendingAction = ref<'close' | 'open' | null>(null)
const busy = ref(false)
const error = ref('')
const info = ref('')
const operationLog = ref<{ time: string; action: string; state: string; detail: string }[]>([])
const activeRecord = computed(() => record.value?.state === 0 ? record.value : null)
const canClose = computed(() => activeRecord.value === null)
const canOpen = computed(() => activeRecord.value !== null)
const selectedTypes = computed(() => closeOptions.filter(item => options.value[item.value]).map(item => item.value))
const selectedSnapshotTotal = computed(() => selectedTypes.value.reduce((total, type) => total + snapshotCounts[type], 0))
const activeTypeSummary = computed(() => activeRecord.value ? closeOptions.filter(item => activeRecord.value?.closeOptions[item.value] === 1).map(item => item.value).join(' · ') : '')

function nowText(): string { return new Date().toLocaleString('zh-TW', { hour12: false }) }

function setStartingCase(value: StartingCase): void {
  startingCase.value = value
  error.value = ''
  info.value = ''
  options.value = emptyOptions()
  operationLog.value = []
  unresolvedAdjustments.value = false
  serialSequence.value = value === 'awaiting-close' ? 1 : 2
  if (value === 'awaiting-close') {
    record.value = null
    executionAllowed.value = true
    info.value = 'DEV 初始情境：沒有 State=0 盤點批次，可選盤點範圍後關帳。'
    return
  }
  record.value = {
    countSerial: '261122001', state: 0, countDate: '2026-11-22 08:00:00', creator: 'DEVUSER', createDate: '2026-11-22 08:00:01',
    openUserNo: '', openDate: '', closeOptions: { WIP: 1, RAW: 0, FGD: 1, SEMI: 0, MTLWIP: 0, SEMIWIP: 1 }, snapshotCounts,
  }
  executionAllowed.value = false
  info.value = 'DEV 初始情境：已有 State=0 盤點批次，批次作業目前鎖定，可檢查待調帳後開帳。'
}

function handleStartingChange(event: Event): void {
  const value = (event.target as HTMLSelectElement).value as StartingCase
  setStartingCase(value)
}

function requestAction(action: 'close' | 'open'): void {
  error.value = ''
  info.value = ''
  if (!dev.value || busy.value) return
  if (action === 'close') {
    if (activeRecord.value) { error.value = '已有 State=0 關帳批次，不能再關帳。'; return }
    if (!selectedTypes.value.length) { error.value = '請至少選擇一種 Close Option。'; return }
  } else {
    if (!activeRecord.value) { error.value = '目前沒有可開帳的 State=0 批次。'; return }
    if (unresolvedAdjustments.value) { error.value = '仍有盤點明細 AdjustState=1 尚未 Confirm；不能開帳。'; return }
  }
  pendingAction.value = action
}

function confirmAction(): void {
  const action = pendingAction.value
  if (!action || busy.value) return
  pendingAction.value = null
  busy.value = true
  error.value = ''
  info.value = ''
  const time = nowText()

  if (action === 'close') {
    const selectedForClose = [...selectedTypes.value]
    executionAllowed.value = false
    if (failureCase.value === 'close-snapshot-failure') {
      operationLog.value.unshift({ time, action: 'InvCount Close', state: '失敗', detail: 'AllowExecuteLot 已先設為 0，但後續 State／detail SQL 失敗；目前沒有 State=0 批次可從 Form 開帳。' })
      error.value = '模擬關帳失敗：原 Service 在 Snapshot SQL 前已停用批次過站，失敗後沒有補償回開。需要人工恢復 AllowExecuteLot。'
      busy.value = false
      return
    }
    const serial = `261122${String(serialSequence.value++).padStart(3, '0')}`
    const flags = Object.fromEntries(closeOptions.map(item => [item.value, options.value[item.value] ? 1 : 0])) as Record<CloseType, 0 | 1>
    const captured = Object.fromEntries(closeOptions.map(item => [item.value, options.value[item.value] ? snapshotCounts[item.value] : 0])) as Record<CloseType, number>
    record.value = { countSerial: serial, state: 0, countDate: time, creator: 'DEVUSER', createDate: time, openUserNo: '', openDate: '', closeOptions: flags, snapshotCounts: captured }
    options.value = emptyOptions()
    operationLog.value.unshift({ time, action: 'InvCount Close', state: 'State=0 · AllowExecuteLot=0', detail: `${serial}；${selectedForClose.join('／')}；快照 ${Object.values(captured).reduce((a, b) => a + b, 0)} 筆` })
    info.value = `關帳完成：${serial}。State=0，批次過站鎖定；${activeTypeSummary.value} 已建立盤點快照。`
  } else {
    const current = activeRecord.value
    if (!current) {
      error.value = 'State=0 批次已不存在，請重新讀取狀態。'
      busy.value = false
      return
    }
    current.state = 1
    current.openUserNo = 'DEVUSER'
    current.openDate = time
    if (failureCase.value === 'open-parameter-failure') {
      operationLog.value.unshift({ time, action: 'InvCount Open', state: 'State=1 · AllowExecuteLot 仍為 0', detail: `${current.countSerial} 已更新成 State=1，但系統參數回寫失敗。` })
      error.value = '模擬開帳部分失敗：盤點主檔已 State=1，但 AllowExecuteLot 沒有恢復為 1，過站仍受阻。'
      busy.value = false
      return
    }
    executionAllowed.value = true
    operationLog.value.unshift({ time, action: 'InvCount Open', state: 'State=1 · AllowExecuteLot=1', detail: `${current.countSerial}；由 DEVUSER 開帳。` })
    info.value = `開帳完成：${current.countSerial} 已改為 State=1，批次過站恢復。`
  }
  busy.value = false
}

function cancelAction(): void { pendingAction.value = null }
</script>

<template>
  <section class="count-state">
    <header class="hero"><div><p class="eyebrow">WIP · INVENTORY CONTROL · WP606</p><h1>盤點關／開帳</h1><p class="subtitle">選盤點範圍建立數量快照並鎖定過站；調帳完成後恢復過站</p></div><div class="hero-actions"><span class="dev-pill">DEV 假資料</span><button class="quiet" type="button" @click="emit('close')">關閉</button></div></header>
    <div v-if="!dev" class="unavailable">正式盤點服務尚未接入；此頁不會讀取或改寫資料。</div>
    <template v-else>
      <div class="flow"><b>選盤點範圍</b><span>→</span><b>InvCount Close · State=0</b><span>→</span><b>盤點／調帳</b><span>→</span><b>InvCount Open · State=1</b></div>
      <div class="dev-controls"><label>起始狀態<select :value="startingCase" @change="handleStartingChange"><option value="awaiting-close">沒有 State=0 批次 · 待關帳</option><option value="awaiting-open">已有 State=0 批次 · 待開帳</option></select></label><label>DEV 結果情境<select v-model="failureCase"><option value="success">成功</option><option value="close-snapshot-failure">關帳：Snapshot SQL 失敗</option><option value="open-parameter-failure">開帳：AllowExecuteLot 回寫失敗</option></select></label><label class="adjust-check"><input v-model="unresolvedAdjustments" type="checkbox"> 模擬有 AdjustState=1 未確認調帳</label></div>
      <div v-if="error || info" class="notice" :class="{ bad: !!error }" role="status">{{ error || info }}</div>
      <div v-if="!executionAllowed" class="lock-banner"><span class="lock-icon">Ⅱ</span><div><strong>生產批過站已鎖定</strong><small>AllowExecuteLot=0 · {{ activeRecord ? `盤點 ${activeRecord.countSerial} 等待完成` : '未找到可開帳的 State=0 批次' }}</small></div><span class="lock-state">LOCKED</span></div>
      <div v-else class="open-banner"><span class="open-icon">✓</span><div><strong>生產批過站開放</strong><small>AllowExecuteLot=1 · 目前沒有盤點關帳批次</small></div><span class="open-state">OPEN</span></div>

      <div class="columns">
        <section class="card options-card"><div class="card-heading"><div><h2>Close Option</h2><p>只建立勾選庫存類型的盤點明細快照；至少選一項才能關帳。</p></div><span class="state-pill" :class="{ closed: !!activeRecord }">{{ activeRecord ? '盤點中' : '可建立盤點' }}</span></div><div class="options-list"><label v-for="item in closeOptions" :key="item.value" :class="{ selected: options[item.value], disabled: !!activeRecord }"><input v-model="options[item.value]" type="checkbox" :disabled="!!activeRecord"><span class="option-main"><strong>{{ item.label }}</strong><small>{{ item.source }} · DEV 約 {{ snapshotCounts[item.value] }} 筆</small></span><span v-if="activeRecord" class="selected-state">{{ activeRecord.closeOptions[item.value] ? '已納入' : '未納入' }}</span></label></div><div class="snapshot-summary"><span>本次預計快照</span><strong>{{ selectedTypes.length }} 類</strong><strong>{{ selectedSnapshotTotal.toLocaleString('zh-TW') }} 筆</strong></div><div class="button-row"><button class="quiet" type="button" :disabled="busy || !canClose" @click="requestAction('close')">InvCount Close</button><button class="primary" type="button" :disabled="busy || !canOpen" @click="requestAction('open')">InvCount Open</button></div><p class="hint">Open 前檢查三種 detail（WIP／MTL／PRD）是否存在 AdjustState=1；任一未確認都會阻止開帳。公司端列印按鈕在原 Form 隱藏，清冊由 WP607 處理。</p></section>

        <section class="card state-card"><div class="card-heading"><div><h2>盤點批次狀態</h2><p>來源：LoadInvCountState(State=0)；State 欄原 Grid 隱藏。</p></div><span class="setting">State=0 才列出</span></div><div class="table-wrap"><table><thead><tr><th>CountSerial</th><th>CountDate</th><th>CloseWIP</th><th>CloseRAW</th><th>CloseFGD</th><th>CloseSEMI</th><th>CloseMTLWIP</th><th>CloseSEMIWIP</th><th>Creator</th><th>CreateDate</th></tr></thead><tbody><tr v-if="activeRecord"><td class="serial">{{ activeRecord.countSerial }}</td><td>{{ activeRecord.countDate }}</td><td>{{ activeRecord.closeOptions.WIP ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.closeOptions.RAW ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.closeOptions.FGD ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.closeOptions.SEMI ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.closeOptions.MTLWIP ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.closeOptions.SEMIWIP ? 'Close' : 'Not Close' }}</td><td>{{ activeRecord.creator }}</td><td>{{ activeRecord.createDate }}</td></tr><tr v-else><td colspan="10" class="empty">沒有 State=0 盤點批次</td></tr></tbody></table></div><div v-if="activeRecord" class="snapshot-details"><h3>盤點快照明細筆數（DEV）</h3><div class="snapshot-chips"><span v-for="item in closeOptions" :key="item.value" :class="{ included: activeRecord.closeOptions[item.value] }"><b>{{ item.value }}</b>{{ activeRecord.closeOptions[item.value] ? `${activeRecord.snapshotCounts[item.value].toLocaleString('zh-TW')} 筆` : '未選' }}</span></div></div></section>
      </div>

      <section class="card history"><div class="card-heading"><div><h2>DEV 操作紀錄</h2><p>關帳停用 AllowExecuteLot；開帳先更新 State，再開放過站。</p></div></div><div class="table-wrap"><table><thead><tr><th>時間</th><th>操作</th><th>結果</th><th>細節</th></tr></thead><tbody><tr v-for="(item, index) in operationLog" :key="item.time + item.action + index"><td>{{ item.time }}</td><td>{{ item.action }}</td><td><span class="status" :class="{ failed: item.state.includes('失敗') }">{{ item.state }}</span></td><td>{{ item.detail }}</td></tr><tr v-if="!operationLog.length"><td colspan="4" class="empty">尚無操作紀錄</td></tr></tbody></table></div></section>
      <section v-if="pendingAction" class="confirm-backdrop" role="presentation"><div class="confirm-dialog" role="dialog" aria-modal="true" :aria-label="pendingAction === 'close' ? '確認盤點關帳' : '確認盤點開帳'"><div class="dialog-icon" :class="pendingAction">{{ pendingAction === 'close' ? 'Ⅱ' : '✓' }}</div><h2>{{ pendingAction === 'close' ? '確認盤點關帳？' : '確認盤點開帳？' }}</h2><p v-if="pendingAction === 'close'">將停止生產批過站，建立 {{ selectedTypes.length }} 類盤點快照（DEV 預計 {{ selectedSnapshotTotal.toLocaleString('zh-TW') }} 筆），並新增 State=0 盤點批次。</p><p v-else>將盤點主檔 State 由 0 改為 1，然後把 AllowExecuteLot 設為 1，恢復生產批過站。</p><div class="actions"><button class="quiet" type="button" @click="cancelAction">取消</button><button class="primary" type="button" @click="confirmAction">確認{{ pendingAction === 'close' ? '關帳' : '開帳' }}</button></div></div></section>
      <footer>DEV 只模擬盤點快照、State 與 AllowExecuteLot 狀態；不呼叫 MES Service 或資料庫。</footer>
    </template>
  </section>
</template>

<style scoped>
.count-state{--ink:#1b2a3c;--muted:#708094;--line:#dce4eb;color:var(--ink);height:100%;min-height:0;overflow:auto;padding:22px 26px 30px;background:#f7f9fb;font:13px/1.45 "Segoe UI","Microsoft JhengHei",sans-serif}.hero{display:flex;align-items:center;justify-content:space-between;margin-bottom:15px}.eyebrow{margin:0;color:#73869a;font-size:10px;font-weight:750;letter-spacing:.13em}.hero h1{margin:2px 0 0;font-size:23px}.subtitle{margin:0;color:var(--muted);font-size:12px}.hero-actions{display:flex;align-items:center;gap:9px}.dev-pill,.setting,.state-pill,.status{display:inline-block;padding:4px 8px;border-radius:999px;background:#e8f4ed;color:#22724a;font-size:10px;font-weight:700}.setting{background:#edf2f8;color:#385a78}.state-pill.closed{background:#fff1dc;color:#8a5a1f}.quiet,.primary{height:34px;padding:0 12px;border:1px solid var(--line);border-radius:5px;background:#fff;color:#334d66;cursor:pointer;font:inherit}.primary{border-color:#173b5d;background:#173b5d;color:white;font-weight:700}.primary:disabled,.quiet:disabled{opacity:.42;cursor:not-allowed}.flow{display:flex;align-items:center;gap:10px;padding:10px 14px;margin-bottom:12px;border:1px solid #ccdae7;border-radius:6px;background:#eff5fa;color:#48637c;font-size:11px}.flow b{color:#234663}.dev-controls{display:flex;align-items:end;gap:13px;flex-wrap:wrap;padding:9px 12px;margin-bottom:12px;border:1px solid var(--line);border-radius:6px;background:white}.dev-controls label{display:flex;flex-direction:column;gap:3px;color:#65788c;font-size:9px}.dev-controls select{height:29px;min-width:215px;border:1px solid #cdd8e2;border-radius:4px;padding:0 7px;background:white;color:#354e65;font:inherit}.dev-controls .adjust-check{flex-direction:row;align-items:center;padding-bottom:6px;font-size:10px}.adjust-check input{accent-color:#1d6289}.notice{margin:0 0 12px;padding:9px 12px;border:1px solid #bddcc9;border-radius:5px;background:#eef8f1;color:#286440}.notice.bad{border-color:#ebc5c1;background:#fff3f1;color:#9e3831}.lock-banner,.open-banner{display:flex;align-items:center;gap:12px;padding:11px 14px;margin-bottom:12px;border:1px solid #e6c4bd;border-radius:7px;background:#fff2ef;color:#8c3f33}.open-banner{border-color:#bbd9c6;background:#eef8f1;color:#286440}.lock-banner strong,.open-banner strong{display:block;font-size:13px}.lock-banner small,.open-banner small{display:block;margin-top:2px;color:#7d8996;font-size:10px}.lock-icon,.open-icon{display:grid;place-items:center;width:27px;height:27px;border-radius:50%;background:#f8dfd9;font-weight:800}.open-icon{background:#d9efe0}.lock-state,.open-state{margin-left:auto;font-size:10px;font-weight:800;letter-spacing:.1em}.columns{display:grid;grid-template-columns:minmax(300px, .8fr) minmax(500px, 1.5fr);gap:12px}.card{margin-bottom:12px;border:1px solid var(--line);border-radius:7px;background:white;box-shadow:0 2px 5px #10283a08;overflow:hidden}.card-heading{display:flex;align-items:center;justify-content:space-between;padding:12px 14px}.card-heading h2{margin:0;font-size:14px}.card-heading p{margin:2px 0 0;color:var(--muted);font-size:10px}.options-list{padding:0 12px}.options-list label{display:flex;align-items:center;gap:8px;padding:9px 8px;border-top:1px solid #edf1f4;cursor:pointer}.options-list label.selected{background:#f1f7fb}.options-list label.disabled{cursor:default}.options-list input{accent-color:#1d6289}.option-main{display:flex;flex:1;align-items:center;justify-content:space-between;gap:8px}.option-main strong{font-size:11px}.option-main small{color:#8492a0;font-size:9px}.selected-state{color:#60768a;font-size:9px}.snapshot-summary{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:11px 14px;margin-top:7px;border-top:1px solid #e6ebf0;background:#f7f9fb;color:#697d91;font-size:10px}.snapshot-summary strong{color:#2c536e;font-size:11px}.button-row{display:flex;justify-content:flex-end;gap:8px;padding:11px 14px}.hint{margin:0;padding:0 14px 13px;color:#7a8998;font-size:9px}.table-wrap{overflow:auto;border-top:1px solid #e6ebf0}table{width:100%;border-collapse:collapse;white-space:nowrap;font-size:10px}th{position:sticky;top:0;background:#f1f5f8;color:#607388;font-size:9px;font-weight:700;text-align:left}th,td{padding:7px 8px;border-bottom:1px solid #ebeff3}.serial{color:#60768b;font-family:Consolas,monospace}.empty{text-align:center;color:#8a98a7;padding:17px}.snapshot-details{padding:10px 13px}.snapshot-details h3{margin:0 0 7px;color:#667a8d;font-size:10px}.snapshot-chips{display:flex;gap:6px;flex-wrap:wrap}.snapshot-chips span{padding:5px 7px;border:1px solid var(--line);border-radius:4px;color:#8491a0;font-size:9px}.snapshot-chips span.included{border-color:#c8dbe8;background:#eff6fa;color:#365b75}.snapshot-chips b{margin-right:4px}.history .card-heading{padding-bottom:9px}.status{border-radius:4px;background:#edf2f8;color:#536d86;font-size:9px}.status.failed{background:#fff0ed;color:#a03c31}.confirm-backdrop{position:fixed;inset:0;z-index:40;display:grid;place-items:center;padding:16px;background:#1a2a3990}.confirm-dialog{width:min(440px,100%);padding:22px;border:1px solid #d4dde5;border-radius:9px;background:white;box-shadow:0 16px 45px #10233535}.dialog-icon{display:grid;place-items:center;width:36px;height:36px;border-radius:50%;background:#fff0df;color:#895a25;font-weight:800}.dialog-icon.open{background:#e5f3e9;color:#28734a}.confirm-dialog h2{margin:12px 0 4px;font-size:17px}.confirm-dialog p{margin:0;color:#687b8e;font-size:11px}.actions{display:flex;justify-content:flex-end;gap:8px;padding-top:18px}footer{padding:2px 2px 8px;color:#8391a0;font-size:10px}.unavailable{padding:16px;border:1px solid #e8d2ac;border-radius:6px;background:#fff8ea;color:#7a5622}@media(max-width:1050px){.count-state{padding:14px}.flow{align-items:flex-start;flex-wrap:wrap}.columns{grid-template-columns:1fr}.option-main{align-items:flex-start;flex-direction:column;gap:2px}}
</style>
