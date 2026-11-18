<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps<{ message: string | null }>()
const emit = defineEmits<{ resolve: [accepted: boolean] }>()
const cancelButton = ref<HTMLButtonElement | null>(null)
let previousFocus: HTMLElement | null = null
watch(() => props.message, async message => {
  if (message !== null) {
    previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
    await nextTick()
    cancelButton.value?.focus()
  } else previousFocus?.focus()
})
function trapTab(event: KeyboardEvent): void {
  const dialog = event.currentTarget as HTMLElement
  const buttons = Array.from(dialog.querySelectorAll<HTMLButtonElement>('button'))
  const target = event.shiftKey ? buttons[0] : buttons[buttons.length - 1]
  if (document.activeElement === target) {
    event.preventDefault()
    ;(event.shiftKey ? buttons[buttons.length - 1] : buttons[0])?.focus()
  }
}
onBeforeUnmount(() => previousFocus?.focus())
</script>

<template>
  <Teleport to="body">
    <div v-if="message !== null" class="action-confirmation-shade" @click.self="emit('resolve', false)">
      <section class="action-confirmation" role="alertdialog" aria-modal="true" aria-label="確認操作"
        @keydown.esc.stop.prevent="emit('resolve', false)" @keydown.tab="trapTab">
        <h2>確認操作</h2>
        <p>{{ message }}</p>
        <footer>
          <button ref="cancelButton" type="button" @click="emit('resolve', false)">取消</button>
          <button type="button" class="confirm" @click="emit('resolve', true)">確定</button>
        </footer>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.action-confirmation-shade{position:fixed;inset:0;z-index:2000;display:grid;place-items:center;padding:20px;background:#19181666}
.action-confirmation{box-sizing:border-box;width:min(440px,100%);padding:20px;border:1px solid #c8c4bb;background:#f8f7f3;box-shadow:0 8px 28px #0003;color:#30302e;font:14px/1.5 "Segoe UI","Microsoft JhengHei",sans-serif}
h2{margin:0 0 12px;font-size:18px}p{margin:0 0 20px;overflow-wrap:anywhere}footer{display:flex;justify-content:flex-end;gap:10px}button{min-height:34px;padding:5px 18px;border:1px solid #bdb9b0;border-radius:3px;background:#fff;font:inherit;color:inherit;cursor:pointer}.confirm{background:#f5b13d}button:focus-visible{outline:2px solid #8b761c;outline-offset:2px}
</style>
