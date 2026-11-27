<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ apiBaseUrl?: string; developmentPreview?: boolean }>(), { apiBaseUrl: '', developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./ConfirmLotDevPreview.vue')) : null
const Live = defineAsyncComponent(() => import('./ConfirmLotLive.vue'))
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" @close="emit('close')" />
  <component :is="Live" v-else :api-base-url="props.apiBaseUrl" @close="emit('close')" />
</template>
