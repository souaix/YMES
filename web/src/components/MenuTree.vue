<script setup lang="ts">
import MenuIcon from './MenuIcon.vue'

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

const props = withDefaults(defineProps<{
  items: MenuNode[]
  expandedIds: string[]
  selectedId: string | null
  depth?: number
  ancestorLabels?: string[]
}>(), {
  depth: 1,
  ancestorLabels: () => [],
})

const emit = defineEmits<{
  toggle: [id: string]
  select: [id: string]
}>()

function iconKind(item: MenuNode): string {
  if (props.depth === 1 || !item.children?.length) return item.moduleNo?.toUpperCase() ?? 'folder'
  return 'folder'
}
function menuLabel(item: MenuNode): string {
  return [...props.ancestorLabels, item.label].join(' / ')
}
</script>

<template>
  <div class="menu-tree" :class="`menu-tree-depth-${depth}`">
    <div v-for="item in items" :key="item.id" class="menu-tree-node">
      <button
        type="button"
        class="menu-tree-button"
        :class="{ active: selectedId === item.id, expandable: item.children?.length }"
        :style="{ '--menu-depth': depth - 1 }"
        :aria-label="menuLabel(item)"
        :title="menuLabel(item)"
        :aria-current="selectedId === item.id ? 'page' : undefined"
        :aria-expanded="item.children?.length ? expandedIds.includes(item.id) : undefined"
        @click="item.children?.length ? emit('toggle', item.id) : emit('select', item.id)"
      >
        <MenuIcon :kind="iconKind(item)" />
        <span class="menu-full-label">{{ item.label }}</span>
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
          :ancestor-labels="[...ancestorLabels, item.label]"
          @toggle="emit('toggle', $event)"
          @select="emit('select', $event)"
        />
      </div>
    </div>
  </div>
</template>
