<script setup lang="ts">
withDefaults(defineProps<{ kind?: string; size?: number }>(), {
  kind: 'folder',
  size: 18,
})
</script>

<template>
  <svg class="menu-icon" :width="size" :height="size" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
    <template v-if="kind === 'home'">
      <rect x="3.5" y="3.5" width="7" height="7" rx="1.5" />
      <rect x="13.5" y="3.5" width="7" height="7" rx="1.5" />
      <rect x="3.5" y="13.5" width="7" height="7" rx="1.5" />
      <rect x="13.5" y="13.5" width="7" height="7" rx="1.5" />
    </template>
    <template v-else-if="kind === 'logout'">
      <path d="M10 5H5.5A1.5 1.5 0 0 0 4 6.5v11A1.5 1.5 0 0 0 5.5 19H10" />
      <path d="M14 8l4 4-4 4M8 12h10" />
    </template>
    <template v-else-if="kind === 'EQP'">
      <path d="M4 20h16M6 16V8h12v8M9 8V5h6v3M10 11h4v5h-4zM3 8h2M19 8h2" />
    </template>
    <template v-else-if="kind === 'OP'">
      <circle cx="6" cy="6" r="2" /><circle cx="18" cy="18" r="2" /><circle cx="18" cy="6" r="2" />
      <path d="M8 6h3a3 3 0 0 1 3 3v6a3 3 0 0 0 3 3M14 9a3 3 0 0 1 3-3" />
    </template>
    <template v-else-if="kind === 'MTL'">
      <path d="m12 3 8 4.5v9L12 21l-8-4.5v-9L12 3Z" /><path d="m4.5 7.8 7.5 4.3 7.5-4.3M12 12v9M8 5.3l8 4.5" />
    </template>
    <template v-else-if="kind === 'PRS'">
      <circle cx="5" cy="12" r="2" /><circle cx="19" cy="6" r="2" /><circle cx="19" cy="18" r="2" />
      <path d="M7 12h3a3 3 0 0 0 3-3V9a3 3 0 0 1 3-3M7 12h3a3 3 0 0 1 3 3v0a3 3 0 0 0 3 3" />
    </template>
    <template v-else-if="kind === 'PRD'">
      <path d="m12 3 8 4.5v9L12 21l-8-4.5v-9L12 3Z" /><path d="m4.5 7.8 7.5 4.3 7.5-4.3M12 12v9" />
    </template>
    <template v-else>
      <path d="M3.5 6.5A1.5 1.5 0 0 1 5 5h5l2 2h7a1.5 1.5 0 0 1 1.5 1.5v9A1.5 1.5 0 0 1 19 19H5a1.5 1.5 0 0 1-1.5-1.5v-11Z" />
    </template>
  </svg>
</template>
