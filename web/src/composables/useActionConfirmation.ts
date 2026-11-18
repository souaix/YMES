import { onBeforeUnmount, ref } from 'vue'

export function useActionConfirmation() {
  const confirmationMessage = ref<string | null>(null)
  let settle: ((accepted: boolean) => void) | undefined
  function resolveConfirmation(accepted: boolean): void {
    confirmationMessage.value = null
    const resolve = settle
    settle = undefined
    resolve?.(accepted)
  }
  function requestConfirmation(message: string): Promise<boolean> {
    resolveConfirmation(false)
    confirmationMessage.value = message
    return new Promise(resolve => { settle = resolve })
  }
  onBeforeUnmount(() => resolveConfirmation(false))
  return { confirmationMessage, requestConfirmation, resolveConfirmation }
}
