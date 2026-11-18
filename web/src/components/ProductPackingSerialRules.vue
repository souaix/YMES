<script setup lang="ts">
import { computed, ref } from 'vue'

type Part = {
  codeStart: number | ''
  codeEnd: number | ''
  sourceType: string
  searchStart: number | ''
  searchEnd: number | ''
  decimalType: number
  userDefine: string
  description: string
}
type SerialRule = {
  serialType: number
  totalLength: number | ''
  description: string
  parts: Part[]
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}

const props = defineProps<{
  rules: SerialRule[]
  canEdit: boolean
  currentUser: string
  save: (rules: SerialRule[]) => boolean
}>()
const serialTypes = [{ key: 11, name: '棧板' }, { key: 12, name: '大箱' }, { key: 13, name: '小箱' }]
const sources = [
  ['D2', '日'], ['M2', '兩碼月'], ['M3', '三碼月'], ['Y4', '四碼年'], ['Y2', '兩碼年'], ['Y1', '一碼年'],
  ['W2', '週別'], ['PT', '產品類別'], ['PN', '產品編號'], ['UD', '自訂資料'], ['FN', '產地代號'],
  ['MO', '工單編號'], ['NG', '一般流水號'],
]
const fixedWidths: Record<string, number> = { D2: 2, M2: 2, M3: 3, Y4: 4, Y2: 2, Y1: 1, W2: 2 }
const searchableSources = new Set(['PT', 'PN', 'FN', 'MO'])
const maxCounterWidths: Record<number, number> = { 1: 18, 2: 15, 3: 12 }
const selected = ref(0)
const draft = ref<SerialRule | null>(null)
const originalType = ref(0)
const error = ref('')
const notice = ref('')
const availableTypes = computed(() => serialTypes.filter(type =>
  type.key === originalType.value || !props.rules.some(row => row.serialType === type.key)))
const selectedRule = computed(() => props.rules.find(row => row.serialType === selected.value))

function open(row?: SerialRule): void {
  originalType.value = row?.serialType ?? 0
  const serialType = row?.serialType ?? availableTypes.value[0]?.key
  if (!serialType) return
  draft.value = row ? { ...row, parts: row.parts.map(part => ({ ...part })) } : {
    serialType, totalLength: '', description: '', parts: [],
    creator: props.currentUser, createDate: new Date().toISOString(), reviser: '', reviseDate: '',
  }
  error.value = ''
  notice.value = ''
}

function addPart(): void {
  if (!draft.value || draft.value.parts.length >= 99) return
  const nextStart = Math.min(99, Number(draft.value.parts.at(-1)?.codeEnd || 0) + 1)
  draft.value.parts.push({
    codeStart: nextStart, codeEnd: '', sourceType: 'UD', searchStart: '', searchEnd: '', decimalType: 1,
    userDefine: '', description: '',
  })
}

function inRange(value: number | '', minimum: number, maximum: number): value is number {
  return typeof value === 'number' && Number.isInteger(value) && value >= minimum && value <= maximum
}

function validate(row: SerialRule): string {
  if (!inRange(row.totalLength, 1, 99)) return 'TotalLength 必須是 1–99 的整數。'
  if (row.parts.length < 2 || row.parts.filter(part => part.sourceType === 'NG').length !== 1) {
    return '明細必須包含一段一般流水號（NG）及至少一段其他內容。'
  }
  const ordered = [...row.parts].sort((a, b) => Number(a.codeStart) - Number(b.codeStart))
  let offset = 1
  for (const part of ordered) {
    if (part.codeStart !== offset || !inRange(part.codeEnd, offset, row.totalLength)) {
      return '明細位置必須從 1 開始，依序銜接且不能重疊。'
    }
    const width = part.codeEnd - offset + 1
    if (fixedWidths[part.sourceType] && width !== fixedWidths[part.sourceType]) return part.sourceType + ' 的長度不符合來源格式。'
    if (part.sourceType === 'UD' && (part.userDefine.trim().length !== width || /[\r\n'"<>]/.test(part.userDefine))) {
      return '自訂資料的長度必須等於明細長度，且不可含換行、引號或角括號。'
    }
    if (part.sourceType === 'NG' && width > (maxCounterWidths[part.decimalType] ?? 0)) return '流水號長度超出選定進位制上限。'
    if (searchableSources.has(part.sourceType) && (
      !inRange(part.searchStart, 1, 99) || !inRange(part.searchEnd, part.searchStart, 99) || part.searchEnd - part.searchStart + 1 !== width
    )) return part.sourceType + ' 的擷取起訖長度必須等於明細長度。'
    offset = part.codeEnd + 1
  }
  return offset === row.totalLength + 1 ? '' : '明細必須完整覆蓋 TotalLength。'
}

function save(closeAfter: boolean): void {
  const row = draft.value
  if (!props.canEdit || !row) return
  error.value = validate(row)
  notice.value = ''
  if (error.value) return
  const saved = {
    ...row,
    parts: row.parts.map(part => ({
      ...part,
      userDefine: part.sourceType === 'UD' ? part.userDefine.trim() : '',
      searchStart: searchableSources.has(part.sourceType) ? part.searchStart : '' as const,
      searchEnd: searchableSources.has(part.sourceType) ? part.searchEnd : '' as const,
      decimalType: part.sourceType === 'NG' ? part.decimalType : 0,
    })),
    reviser: originalType.value ? props.currentUser : '',
    reviseDate: originalType.value ? new Date().toISOString() : '',
  }
  if (!props.save([...props.rules.filter(item => item.serialType !== row.serialType), saved])) {
    error.value = '包裝序號規則假資料儲存失敗。'
    return
  }
  selected.value = row.serialType
  originalType.value = row.serialType
  notice.value = '包裝序號規則假資料已儲存。'
  if (closeAfter) draft.value = null
  else draft.value = { ...saved, parts: saved.parts.map(part => ({ ...part })) }
}

function remove(): void {
  if (!props.canEdit || !selectedRule.value || !window.confirm('確定刪除此包裝序號規則？')) return
  if (props.save(props.rules.filter(row => row.serialType !== selected.value))) {
    selected.value = 0
    draft.value = null
    error.value = ''
    notice.value = '已刪除包裝序號規則。'
  } else error.value = '包裝序號規則假資料儲存失敗。'
}
</script>

<template>
  <section class="pack-rule-panel">
    <h3>Serial Rule</h3>
    <div class="pack-rule-actions">
      <button type="button" :disabled="!canEdit || rules.length >= serialTypes.length" @click="open()">Add</button>
      <button type="button" :disabled="!canEdit || !selectedRule" @click="open(selectedRule)">Edit</button>
      <button type="button" :disabled="!canEdit || !selectedRule" @click="remove">Delete</button>
    </div>
    <div class="pack-rule-scroll"><table>
      <thead><tr><th>選取</th><th>SerialType</th><th>TotalLength</th><th>Description</th><th>Creator</th><th>Reviser</th></tr></thead>
      <tbody>
        <tr v-for="rule in rules" :key="rule.serialType">
          <td><input v-model="selected" type="radio" :value="rule.serialType" :aria-label="'SerialType ' + rule.serialType"></td>
          <td>{{ rule.serialType }} {{ serialTypes.find(type => type.key === rule.serialType)?.name }}</td>
          <td>{{ rule.totalLength }}</td><td>{{ rule.description }}</td><td>{{ rule.creator }}</td><td>{{ rule.reviser }}</td>
        </tr>
        <tr v-if="!rules.length"><td colspan="6">尚未設定包裝序號規則。</td></tr>
      </tbody>
    </table></div>
    <ul v-if="selectedRule && !draft" class="pack-rule-items">
      <li v-for="(part, index) in selectedRule.parts" :key="index">{{ part.codeStart }}–{{ part.codeEnd }}　{{ part.sourceType }}　{{ part.userDefine || part.description }}</li>
    </ul>
    <form v-if="draft" class="pack-rule-form" @submit.prevent="save(true)">
      <div class="pack-rule-fields">
        <label>SerialType <select v-model.number="draft.serialType" :disabled="!!originalType"><option v-for="type in availableTypes" :key="type.key" :value="type.key">{{ type.key }} {{ type.name }}</option></select></label>
        <label>TotalLength <input v-model.number="draft.totalLength" type="number" min="1" max="99"></label>
        <label>Description <input v-model="draft.description" maxlength="255"></label>
      </div>
      <button type="button" @click="addPart">Add Detail</button>
      <div class="pack-rule-scroll"><table>
        <thead><tr><th>CodeStart</th><th>CodeEnd</th><th>SourceType</th><th>來源設定</th><th>Description</th><th></th></tr></thead>
        <tbody><tr v-for="(part, index) in draft.parts" :key="index">
          <td><input v-model.number="part.codeStart" :aria-label="'CodeStart ' + (index + 1)" type="number" min="1" max="99"></td>
          <td><input v-model.number="part.codeEnd" :aria-label="'CodeEnd ' + (index + 1)" type="number" min="1" max="99"></td>
          <td><select v-model="part.sourceType" :aria-label="'SourceType ' + (index + 1)"><option v-for="[code, name] in sources" :key="code" :value="code">{{ code }} {{ name }}</option></select></td>
          <td>
            <label v-if="part.sourceType === 'UD'">UserDefine <input v-model="part.userDefine" maxlength="50"></label>
            <label v-else-if="part.sourceType === 'NG'">DecimalType <select v-model.number="part.decimalType"><option :value="1">10</option><option :value="2">16</option><option :value="3">36</option></select></label>
            <template v-else-if="searchableSources.has(part.sourceType)">
              <label>SearchStart <input v-model.number="part.searchStart" type="number" min="1" max="99"></label>
              <label>SearchEnd <input v-model.number="part.searchEnd" type="number" min="1" max="99"></label>
            </template>
          </td>
          <td><input v-model="part.description" :aria-label="'Description ' + (index + 1)" maxlength="255"></td>
          <td><button type="button" @click="draft.parts.splice(index, 1)">Remove</button></td>
        </tr></tbody>
      </table></div>
      <div class="pack-rule-actions"><button type="button" @click="save(false)">Apply</button><button type="submit">Confirm</button><button type="button" @click="draft = null; error = ''">Close</button></div>
    </form>
    <p v-if="error" class="pack-rule-error" role="alert">{{ error }}</p>
    <p v-else-if="notice" role="status">{{ notice }}</p>
  </section>
</template>
