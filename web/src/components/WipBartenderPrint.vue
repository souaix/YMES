<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./WipBartenderPrintDevPreview.vue')) : null

function closePage(): void { emit('close') }
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <section v-else class="bartender-pending" aria-label="Bartender print">
    <p role="status">正式標籤資料與列印服務尚未接入。</p>
    <button type="button" class="button" @click="closePage">Close (X)</button>
  </section>
</template>

<style scoped>
.bartender-pending { padding: 12px; min-height: 0; box-sizing: border-box; background: #f7f5ee; color: var(--ink, #26251f); font: inherit; }
.bartender-pending p { margin: 0 0 10px; color: var(--muted, #777366); }
.bartender-pending .button { min-height: 26px; padding: 3px 10px; border: 1px solid #3b3930; border-radius: 3px; background: #3b3930; color: white; font: inherit; cursor: pointer; }
.bartender-pending .button:focus-visible { outline: 2px solid var(--yellow, #e7c600); outline-offset: 2px; }
</style>
