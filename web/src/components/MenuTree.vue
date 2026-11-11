<script setup lang="ts">
interface MenuNode {
  id: string
  label: string
  shortLabel: string
  moduleNo?: string
  menuNo?: string
  functionNo?: string
  tableName?: string
  children?: MenuNode[]
}

withDefaults(defineProps<{
  items: MenuNode[]
  expandedIds: string[]
  selectedId: string | null
  depth?: number
}>(), {
  depth: 1,
})

const emit = defineEmits<{
  toggle: [id: string]
  select: [id: string]
}>()
</script>

<template>
  <div class="menu-tree" :class="`menu-tree-depth-${depth}`">
    <div v-for="item in items" :key="item.id" class="menu-tree-node">
      <button
        type="button"
        class="menu-tree-button"
        :class="{ active: selectedId === item.id, expandable: item.children?.length }"
        :style="{ '--menu-depth': depth - 1 }"
        :aria-current="selectedId === item.id ? 'page' : undefined"
        :aria-expanded="item.children?.length ? expandedIds.includes(item.id) : undefined"
        @click="item.children?.length ? emit('toggle', item.id) : emit('select', item.id)"
      >
        <span class="menu-full-label">{{ item.label }}</span>
        <span class="menu-short-label">{{ item.shortLabel }}</span>
        <span v-if="item.children?.length" class="menu-tree-chevron" aria-hidden="true">
          {{ expandedIds.includes(item.id) ? '⌄' : '›' }}
        </span>
      </button>
      <div
        v-if="item.children?.length && depth < 3 && expandedIds.includes(item.id)"
        class="menu-tree-children"
        role="group"
        :aria-label="item.label"
      >
        <MenuTree
          :items="item.children"
          :expanded-ids="expandedIds"
          :selected-id="selectedId"
          :depth="depth + 1"
          @toggle="emit('toggle', $event)"
          @select="emit('select', $event)"
        />
      </div>
    </div>
  </div>
</template>
