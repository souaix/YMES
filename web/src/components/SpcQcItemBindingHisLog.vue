<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ apiBaseUrl: string; developmentPreview?: boolean }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./SpcQcItemBindingHisLogDevPreview.vue')) : null
const ApiPage = defineAsyncComponent(() => import('./SpcQcItemBindingHisLogApi.vue'))
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <component :is="ApiPage" v-else :api-base-url="props.apiBaseUrl" @close="emit('close')" />
</template>
