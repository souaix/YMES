<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{
  mode: 'import' | 'export'
  productNo: string
  productVersion: string
  canEdit: boolean
  developmentPreview: boolean
  savedFile?: File
  save: (file: File) => boolean
}>()
const emit = defineEmits<{ close: [] }>()

const expectedName = computed(() => `${props.productNo}-${props.productVersion}`)
const file = ref<File>()
const error = ref('')
const busy = ref(false)

async function pickFile(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement
  const selected = input.files?.[0]
  file.value = undefined
  error.value = ''
  if (!selected) return

  const name = /^(.*)\.(xlsx|xls)$/i.exec(selected.name)
  if (!name || name[1].toUpperCase() !== expectedName.value.toUpperCase()) {
    error.value = `檔名須為 ${expectedName.value}.xlsx 或 ${expectedName.value}.xls。`
    return
  }

  busy.value = true
  try {
    const signature = new Uint8Array(await selected.slice(0, 8).arrayBuffer())
    const expected = name[2].toLowerCase() === 'xlsx'
      ? [0x50, 0x4b, 0x03, 0x04]
      : [0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1]
    if (!expected.every((byte, index) => signature[index] === byte)) {
      error.value = '檔案格式與副檔名不符。'
      return
    }
    file.value = selected
  } catch {
    error.value = '無法讀取選取的檔案。'
  } finally {
    busy.value = false
  }
}

function confirm(): void {
  if (!props.developmentPreview || busy.value) return
  if (props.mode === 'import') {
    if (!props.canEdit || !file.value) return
    if (!props.save(file.value)) {
      error.value = '無法儲存此產品版本的 DEV GBOM 備份。'
      return
    }
    emit('close')
    return
  }

  const saved = props.savedFile
  if (!saved) return
  const url = URL.createObjectURL(saved)
  const link = document.createElement('a')
  link.href = url
  link.download = `${expectedName.value}.${saved.name.split('.').pop()?.toLowerCase()}`
  link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
  emit('close')
}
</script>

<template>
  <section class="gbom-dialog" role="dialog" aria-modal="true" aria-labelledby="gbom-dialog-title">
    <header><h2 id="gbom-dialog-title">{{ mode === 'import' ? 'GBOM 匯入' : 'GBOM 匯出' }}</h2><button type="button" aria-label="關閉" @click="emit('close')">×</button></header>
    <p>產品 {{ productNo }} / {{ productVersion }}</p>
    <p v-if="!developmentPreview">GBOM 檔案處理等待 API 接入。</p>
    <template v-else-if="mode === 'import'">
      <p v-if="!canEdit">產品需為未簽核狀態。</p>
      <template v-else>
        <p>DEV 只驗證檔名與檔案標頭，備份保留至頁面重新整理。</p>
        <label>選取 {{ expectedName }} 的 Excel 檔 <input type="file" accept=".xls,.xlsx" :disabled="busy" @change="pickFile" /></label>
        <p v-if="file">{{ file.name }}（{{ file.size.toLocaleString() }} bytes）</p>
      </template>
    </template>
    <p v-else-if="savedFile">{{ savedFile.name }}（{{ savedFile.size.toLocaleString() }} bytes）</p>
    <p v-else>此產品版本目前沒有可匯出的 DEV GBOM 備份。</p>
    <p v-if="error" role="alert" class="gbom-error">{{ error }}</p>
    <footer>
      <button type="button" :disabled="!developmentPreview || busy || (mode === 'import' ? !canEdit || !file : !savedFile)" @click="confirm">{{ mode === 'import' ? '匯入備份' : '下載備份' }}</button>
      <button type="button" @click="emit('close')">取消</button>
    </footer>
  </section>
</template>

<style scoped>
.gbom-dialog{display:grid;gap:12px;width:min(540px,95vw);max-height:90vh;overflow:auto;padding:16px;background:#fff;border:1px solid #c9c7bd;border-radius:6px;box-shadow:0 12px 36px #28241738}
header,footer{display:flex;align-items:center;gap:8px}header{justify-content:space-between}footer{justify-content:flex-end}h2,p{margin:0}label{display:grid;gap:7px}
button{padding:4px 12px;border:1px solid #aaa797;border-radius:3px;background:#f8f7f2;color:inherit}button:disabled{opacity:.55}.gbom-error{color:#a32020}
</style>
