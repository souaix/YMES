<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ currentUserNo?: string; developmentPreview?: boolean }>(), { currentUserNo: '' })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./WipGoldenMapFileManageDevPreview.vue')) : null
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :current-user-no="props.currentUserNo" :development-preview="true" @close="emit('close')" />
  <section v-else class="golden-map-pending" aria-label="Golden MAP 檔案管理">
    <p>Golden MAP 檔案管理正式服務尚未接入。</p>
    <button type="button" @click="emit('close')">Close (X)</button>
  </section>
</template>

<style scoped>
.golden-map-pending { min-height: 0; padding: 12px; box-sizing: border-box; background: #f7f5ee; color: var(--ink, #26251f); font: inherit; }
.golden-map-pending p { margin: 0 0 10px; color: var(--muted, #777366); }
.golden-map-pending button { min-height: 26px; padding: 3px 10px; border: 1px solid #3b3930; border-radius: 3px; background: #3b3930; color: #fff; font: inherit; cursor: pointer; }
</style>
