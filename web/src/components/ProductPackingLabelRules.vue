<script setup lang="ts">
import { computed, ref } from 'vue'

type ValueSource = {
  dataProperty: 1 | 2
  userDefine: string
  sourceType: string
  searchStart: number | ''
  searchEnd: number | ''
}
type LabelItem = {
  itemName: string
  mode: 1 | 2 | 3
  value: ValueSource
  parts: ValueSource[]
  description: string
  separateSign: string
}
type LabelRule = {
  labelType: string
  printCount: number | ''
  labelPath: string
  description: string
  items: LabelItem[]
  creator: string
  createDate: string
  reviser: string
  reviseDate: string
}

const props = defineProps<{
  rules: LabelRule[]
  canEdit: boolean
  productNo: string
  currentUser: string
  save: (rules: LabelRule[]) => boolean
}>()
const labelTypes = Object.entries({ B: '小箱', C: '大箱', T: '棧板' }).flatMap(([prefix, label]) =>
  [1, 2, 3, 4].map(number => ({ key: prefix + number, label: label + ' Type' + number })))
const sourceOptions = [
  ['D2', '日'], ['M2', '兩碼月'], ['M3', '三碼月'], ['Y4', '四碼年'], ['Y2', '兩碼年'], ['Y1', '一碼年'],
  ['W2', '週別'], ['PT', '產品類別'], ['PN', '產品編號'], ['PU', '產品單位'], ['FN', '產地代號'],
  ['BN', '小箱編號'], ['CN', '大箱編號'], ['TN', '棧板編號'], ['BS', '小箱內序號'], ['CS', '大箱內序號'],
  ['TS', '棧板內序號'], ['1N', '1ofN'], ['EP', '空白'], ['PQ', '實際包裝數'], ['2D', '箱內 2D 條碼文字'],
]
const searchableSources = new Set(['PT', 'PN', 'PU', 'FN'])
const selected = ref('')
const draft = ref<LabelRule | null>(null)
const originalType = ref('')
const detailsOpen = ref('')
const previewOpen = ref(false)
const error = ref('')
const notice = ref('')
const selectedRule = computed(() => props.rules.find(row => row.labelType === selected.value))
const availableTypes = computed(() => labelTypes.filter(type =>
  type.key === originalType.value || !props.rules.some(row => row.labelType === type.key)))

function blankSource(): ValueSource {
  return { dataProperty: 1, userDefine: '', sourceType: '', searchStart: '', searchEnd: '' }
}

function open(row?: LabelRule): void {
  originalType.value = row?.labelType ?? ''
  const labelType = row?.labelType ?? availableTypes.value[0]?.key
  if (!labelType) return
  draft.value = row ? {
    ...row, items: row.items.map(item => ({ ...item, value: { ...item.value }, parts: item.parts.map(part => ({ ...part })) })),
  } : {
    labelType, printCount: 1, labelPath: '', description: '', items: [],
    creator: props.currentUser, createDate: new Date().toISOString(), reviser: '', reviseDate: '',
  }
  previewOpen.value = false
  error.value = ''
  notice.value = ''
}

function addItem(): void {
  if (!draft.value || draft.value.items.length >= 99) return
  draft.value.items.push({ itemName: '', mode: 1, value: blankSource(), parts: [], description: '', separateSign: '' })
}

function checkSource(value: ValueSource, property = value.dataProperty): string {
  if (property === 1) {
    return !value.userDefine.trim() || /[\r\n'"<>]/.test(value.userDefine) ? '自訂資料必填且不可含換行、引號或角括號。' : ''
  }
  if (!sourceOptions.some(([code]) => code === value.sourceType)) return '請選擇系統來源。'
  if (!searchableSources.has(value.sourceType)) return ''
  const bounds = [value.searchStart, value.searchEnd]
  if (bounds.some(number => number !== '' && (!Number.isInteger(number) || number < 1 || number > 99))
    || (value.searchEnd !== '' && value.searchEnd < (value.searchStart || 1))) return '擷取位置限 1–99，結束位置不得小於開始位置。'
  return ''
}

function normalizeSource(value: ValueSource, dataProperty = value.dataProperty): ValueSource {
  if (dataProperty === 1) return { ...blankSource(), userDefine: value.userDefine.trim() }
  return {
    ...value, dataProperty, userDefine: '',
    searchStart: searchableSources.has(value.sourceType) ? value.searchStart : '',
    searchEnd: searchableSources.has(value.sourceType) ? value.searchEnd : '',
  }
}

function save(closeAfter: boolean): void {
  const row = draft.value
  if (!props.canEdit || !row) return
  error.value = ''
  notice.value = ''
  if (row.printCount === '' || !Number.isInteger(row.printCount) || row.printCount < 1 || row.printCount > 99999 || !row.labelPath.trim()) {
    error.value = '請輸入有效的 LabelPath，PrintCount 限 1–99999。'
    return
  }
  const names = new Set<string>()
  for (const item of row.items) {
    const name = item.itemName.trim().toUpperCase()
    if (!name || name === 'SEQUENCENO' || names.has(name)) {
      error.value = 'ItemName 必填且不得重複或使用保留名稱 SequenceNo。'
      return
    }
    names.add(name)
    const invalid = item.mode === 3
      ? (!item.parts.length ? '請新增至少一個組合來源。' : item.parts.map(part => checkSource(part)).find(Boolean))
      : checkSource(item.value, item.mode)
    if (invalid) {
      error.value = item.itemName + '：' + invalid
      return
    }
  }
  const saved: LabelRule = {
    ...row, labelPath: row.labelPath.trim(),
    items: row.items.map(item => ({
      ...item, itemName: item.itemName.trim(),
      value: item.mode === 3 ? blankSource() : normalizeSource(item.value, item.mode),
      parts: item.mode === 3 ? item.parts.map(part => normalizeSource(part)) : [],
      separateSign: item.mode === 2 && item.value.sourceType === '2D' ? item.separateSign : '',
    })),
    reviser: originalType.value ? props.currentUser : '',
    reviseDate: originalType.value ? new Date().toISOString() : '',
  }
  if (!props.save([...props.rules.filter(item => item.labelType !== row.labelType), saved])) {
    error.value = '標籤規則假資料儲存失敗。'
    return
  }
  selected.value = row.labelType
  if (closeAfter) draft.value = null
  else open(saved)
  notice.value = '標籤規則假資料已儲存。'
}

function remove(): void {
  if (!props.canEdit || !selectedRule.value || !window.confirm('確定刪除此標籤規則？')) return
  if (props.save(props.rules.filter(row => row.labelType !== selected.value))) {
    selected.value = ''
    detailsOpen.value = ''
    draft.value = null
    previewOpen.value = false
    error.value = ''
    notice.value = '已刪除標籤規則。'
  } else error.value = '標籤規則假資料儲存失敗。'
}

function previewValue(value: ValueSource): string {
  if (value.dataProperty === 1) return value.userDefine
  if (value.sourceType === 'EP') return ' '
  if (value.sourceType !== 'PN') return '[' + value.sourceType + ']'
  return props.productNo.slice(value.searchStart === '' ? 0 : value.searchStart - 1, value.searchEnd === '' ? undefined : value.searchEnd)
}
</script>

<template>
  <section class="pack-rule-panel">
    <h3>Label Rule</h3>
    <div class="pack-rule-actions">
      <button type="button" :disabled="!canEdit || rules.length >= labelTypes.length" @click="open()">Add</button>
      <button type="button" :disabled="!canEdit || !selectedRule" @click="open(selectedRule)">Edit</button>
      <button type="button" :disabled="!canEdit || !selectedRule" @click="remove">Delete</button>
      <button type="button" :disabled="!selectedRule || !!draft" @click="previewOpen = !previewOpen">{{ previewOpen ? '關閉預覽' : 'Preview' }}</button>
    </div>
    <div class="pack-rule-scroll"><table>
      <thead><tr><th>選取</th><th>LabelType</th><th>PrintCount</th><th>LabelPath</th><th>Description</th><th>Reviser</th></tr></thead>
      <tbody>
        <tr v-for="rule in rules" :key="rule.labelType">
          <td><input v-model="selected" type="radio" :value="rule.labelType" :aria-label="'LabelType ' + rule.labelType" @change="detailsOpen = ''"></td>
          <td>{{ rule.labelType }} {{ labelTypes.find(type => type.key === rule.labelType)?.label }}</td>
          <td>{{ rule.printCount }}</td><td>{{ rule.labelPath }}</td><td>{{ rule.description }}</td><td>{{ rule.reviser }}</td>
        </tr>
        <tr v-if="!rules.length"><td colspan="6">尚未設定標籤規則。</td></tr>
      </tbody>
    </table></div>
    <template v-if="selectedRule && !draft">
      <ul class="pack-rule-items">
        <li v-for="item in selectedRule.items" :key="item.itemName">
          {{ item.itemName }}　{{ item.description }}
          <button v-if="item.mode === 3" type="button" @click="detailsOpen = detailsOpen === item.itemName ? '' : item.itemName">Multi View</button>
          <ol v-if="detailsOpen === item.itemName"><li v-for="(part, index) in item.parts" :key="index">{{ part.dataProperty === 1 ? part.userDefine : part.sourceType }}</li></ol>
        </li>
      </ul>
      <div v-if="previewOpen" class="pack-rule-form">
        <h4>標籤欄位預覽</h4>
        <p>顯示 DEV 欄位值。方括號標示需要正式批次或包裝資料的來源。</p>
        <dl class="pack-rule-fields">
          <div v-for="item in selectedRule.items" :key="item.itemName"><dt>{{ item.itemName }}</dt>
            <dd>{{ item.mode === 3 ? item.parts.map(previewValue).join('') : previewValue(item.value) }}</dd>
          </div>
        </dl>
      </div>
    </template>
    <form v-if="draft" class="pack-rule-form" @submit.prevent="save(true)">
      <div class="pack-rule-fields">
        <label>LabelType <select v-model="draft.labelType" :disabled="!!originalType"><option v-for="type in availableTypes" :key="type.key" :value="type.key">{{ type.key }} {{ type.label }}</option></select></label>
        <label>PrintCount <input v-model.number="draft.printCount" type="number" min="1" max="99999"></label>
        <label>LabelPath <input v-model="draft.labelPath" maxlength="100"></label>
        <label>Description <input v-model="draft.description" maxlength="255"></label>
      </div>
      <button type="button" @click="addItem">Add Detail</button>
      <fieldset v-for="(item, index) in draft.items" :key="index">
        <legend>Item {{ index + 1 }}</legend>
        <div class="pack-rule-fields">
          <label>ItemName <input v-model="item.itemName" maxlength="50"></label>
          <label>DataProperty <select v-model.number="item.mode"><option :value="1">自訂</option><option :value="2">系統</option><option :value="3">組合</option></select></label>
          <label>Description <input v-model="item.description" maxlength="255"></label>
          <button type="button" @click="draft.items.splice(index, 1)">Remove Item</button>
        </div>
        <button v-if="item.mode === 3" type="button" :disabled="item.parts.length >= 99" @click="item.parts.push(blankSource())">Add Multi Value</button>
        <div v-for="(part, partIndex) in (item.mode === 3 ? item.parts : [item.value])" :key="partIndex" class="pack-rule-fields">
          <label v-if="item.mode === 3">Multi {{ partIndex + 1 }} <select v-model.number="part.dataProperty"><option :value="1">自訂</option><option :value="2">系統</option></select></label>
          <label v-if="item.mode === 1 || (item.mode === 3 && part.dataProperty === 1)">UserDefine <input v-model="part.userDefine" maxlength="50"></label>
          <template v-else>
            <label>SourceType <select v-model="part.sourceType"><option value="">請選擇來源</option><option v-for="[code, label] in sourceOptions" :key="code" :value="code">{{ code }} {{ label }}</option></select></label>
            <template v-if="searchableSources.has(part.sourceType)">
              <label>SearchStart <input v-model.number="part.searchStart" type="number" min="1" max="99"></label>
              <label>SearchEnd <input v-model.number="part.searchEnd" type="number" min="1" max="99"></label>
            </template>
            <label v-if="item.mode === 2 && part.sourceType === '2D'">SeparateSign <input v-model="item.separateSign" maxlength="1"></label>
          </template>
          <button v-if="item.mode === 3" type="button" @click="item.parts.splice(partIndex, 1)">Remove Value</button>
        </div>
      </fieldset>
      <div class="pack-rule-actions"><button type="button" @click="save(false)">Apply</button><button type="submit">Confirm</button><button type="button" @click="draft = null; error = ''">Close</button></div>
    </form>
    <p v-if="error" class="pack-rule-error" role="alert">{{ error }}</p>
    <p v-else-if="notice" role="status">{{ notice }}</p>
  </section>
</template>
