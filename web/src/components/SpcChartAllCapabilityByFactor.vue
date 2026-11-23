<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./SpcChartAllCapabilityByFactorDevPreview.vue')) : null
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <section v-else class="pending" aria-label="SPC 製程能力報表_以要因查詢"><p role="status">正式環境尚未開放此功能。</p><button type="button" @click="emit('close')">Close (X)</button></section>
</template>

<style scoped>
.pending{padding:12px;min-height:0;box-sizing:border-box;background:#f7f5ee;color:#26251f;font:inherit}.pending p{margin:0 0 10px;color:#777366}.pending button{min-height:26px;padding:3px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:white;font:inherit;cursor:pointer}
</style>
