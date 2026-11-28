<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./ContAttribAdjustLogQueryDevPreview.vue')) : null
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" @close="emit('close')" />
  <section v-else class="pending" aria-label="生產批作業站製程特性值調整記錄">
    <p role="status">正式 RT801 唯讀查詢 API 尚未交接；此功能目前僅提供 DEV 假資料預覽。</p>
    <button type="button" @click="emit('close')">Close (X)</button>
  </section>
</template>

<style scoped>
.pending{box-sizing:border-box;min-height:0;padding:12px;background:#f7f5ee;color:var(--ink,#26251f);font:inherit}.pending p{margin:0 0 10px;color:var(--muted,#777366)}.pending button{min-height:26px;padding:3px 10px;border:1px solid #3b3930;border-radius:3px;background:#3b3930;color:#fff;font:inherit;cursor:pointer}
</style>
