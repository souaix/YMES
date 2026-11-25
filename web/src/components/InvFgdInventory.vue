<script setup lang="ts">
import { computed, defineAsyncComponent } from 'vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean; apiBaseUrl: string }>(), { developmentPreview: false })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
const Preview = import.meta.env.DEV ? defineAsyncComponent(() => import('./InvFgdInventoryDevPreview.vue')) : null
const ApiPage = defineAsyncComponent(() => import('./InvFgdInventoryApi.vue'))
</script>

<template>
  <component :is="Preview" v-if="dev && Preview" :development-preview="true" @close="emit('close')" />
  <ApiPage v-else :api-base-url="apiBaseUrl" @close="emit('close')" />
</template>
