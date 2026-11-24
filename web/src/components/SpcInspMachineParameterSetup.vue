<script setup lang="ts">
import { computed } from 'vue'
import SpcInspMachineParameterSetupPage from './SpcInspMachineParameterSetupPage.vue'

const props = withDefaults(defineProps<{ developmentPreview?: boolean; apiBaseUrl?: string }>(), { developmentPreview: false, apiBaseUrl: '' })
const emit = defineEmits<{ close: [] }>()
const dev = computed(() => import.meta.env.DEV && props.developmentPreview)
</script>

<template>
  <SpcInspMachineParameterSetupPage
    class="framework-content"
    :development-preview="dev"
    :api-base-url="props.apiBaseUrl"
    @close="emit('close')"
  />
</template>
