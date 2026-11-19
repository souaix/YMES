export interface ProductIdentity { productNo: string; productVersion: string }

export interface ProductRelationStore<T> {
  version: 2
  relations: Record<string, T>
  unresolvedLegacyRelations: Record<string, T>
}

// An opaque UI/storage key only: neither field is normalized or rewritten.
export function productIdentityKey(productNo: string, productVersion: string): string {
  return JSON.stringify([productNo, productVersion])
}

export function readProductIdentityKey(key: string): ProductIdentity | null {
  try {
    const value: unknown = JSON.parse(key)
    if (Array.isArray(value) && value.length === 2 && value.every(part => typeof part === 'string'))
      return { productNo: value[0] as string, productVersion: value[1] as string }
  } catch { /* Empty/legacy/malformed keys are not current opaque keys. */ }
  return null
}

export function migrateLegacyProductRelations<T>(legacy: Record<string, T>, products: readonly ProductIdentity[]): ProductRelationStore<T> {
  const owners = new Map<string, Set<string>>()
  for (const product of products) {
    const oldKey = product.productNo + '|' + product.productVersion
    const keys = owners.get(oldKey) ?? new Set<string>()
    keys.add(productIdentityKey(product.productNo, product.productVersion))
    owners.set(oldKey, keys)
  }
  const migrated: Array<[string, T]> = []
  const unresolved: Array<[string, T]> = []
  for (const [oldKey, value] of Object.entries(legacy)) {
    const candidates = owners.get(oldKey)
    let key = candidates?.size === 1 ? candidates.values().next().value : undefined
    // A single delimiter is recoverable even for an old nested-BOM product
    // absent from the master list. Multiple possible owners are never guessed.
    if (!candidates) {
      const separator = oldKey.indexOf('|')
      if (separator > 0 && separator === oldKey.lastIndexOf('|') && separator < oldKey.length - 1)
        key = productIdentityKey(oldKey.slice(0, separator), oldKey.slice(separator + 1))
    }
    if (key === undefined) unresolved.push([oldKey, value])
    else migrated.push([key, value])
  }
  return { version: 2, relations: Object.fromEntries(migrated), unresolvedLegacyRelations: Object.fromEntries(unresolved) }
}

export function readProductRelationStore<T>(value: unknown): ProductRelationStore<T> {
  const isRecord = (input: unknown): input is Record<string, unknown> => !!input && typeof input === 'object' && !Array.isArray(input)
  if (!isRecord(value) || value.version !== 2 || !isRecord(value.relations) || !isRecord(value.unresolvedLegacyRelations))
    throw new Error('產品開發關聯儲存格式不正確。')
  for (const key of Object.keys(value.relations)) {
    const identity = readProductIdentityKey(key)
    if (!identity || productIdentityKey(identity.productNo, identity.productVersion) !== key)
      throw new Error('產品開發關聯包含無效的產品鍵。')
  }
  return { version: 2, relations: value.relations as Record<string, T>, unresolvedLegacyRelations: value.unresolvedLegacyRelations as Record<string, T> }
}
