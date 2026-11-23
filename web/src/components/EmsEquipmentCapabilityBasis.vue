<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean }>(), { apiBaseUrl: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview === true)
const Preview = import.meta.env.DEV
  ? defineAsyncComponent(() => import('./EmsEquipmentCapabilityBasisDevPreview.vue'))
  : null
const LivePage = defineAsyncComponent(() => import('./EmsEquipmentCapabilityBasisLive.vue'))
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <component :is="LivePage" v-else :api-base-url="props.apiBaseUrl" @close="emit('close')" />
</template>
