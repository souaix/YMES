<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./PmPlanCalendarDevPreview.vue')) : null
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <section v-else class="office-pending-page" aria-label="保養日曆設定">
    <p role="status">正式環境尚未開放此功能。</p>
    <button type="button" class="button" @click="emit('close')">Close (X)</button>
  </section>
</template>

<style scoped>
.office-pending-page { padding: 12px; min-height: 0; box-sizing: border-box; background: #f7f5ee; color: var(--ink, #26251f); font: inherit; }
.office-pending-page p { margin: 0 0 10px; color: var(--muted, #777366); }
.office-pending-page .button { min-height: 26px; padding: 3px 10px; border: 1px solid #3b3930; border-radius: 3px; background: #3b3930; color: white; font: inherit; cursor: pointer; }
</style>
