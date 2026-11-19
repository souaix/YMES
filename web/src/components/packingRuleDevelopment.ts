import type ProductPackingSerialRules from './ProductPackingSerialRules.vue'
import type ProductPackingLabelRules from './ProductPackingLabelRules.vue'

export type Level = { name: string; count: string; quantity: string; unit: string; sop: string }
export type PackingRuleBasis = {
  no: string; version: string; issueState: -1 | 0 | 1 | 2; curVersion: boolean; inline: boolean; levels: Level[]
  serials: InstanceType<typeof ProductPackingSerialRules>['$props']['rules']
  labels: InstanceType<typeof ProductPackingLabelRules>['$props']['rules']
  accessories: { barcode: string; description: string }[]
  creator: string; createDate: string; dataStamp: number; reviser?: string; reviseDate?: string
}

export const packingRuleStorageKey = 'ymes.dev.prd112-packing-rules.v1'
const audit = { creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0 }
const base = { ...audit, inline: true, levels: [{ name: '小箱', count: '', quantity: '20', unit: 'EA', sop: '' }], serials: [], labels: [], accessories: [] }
const seed: PackingRuleBasis[] = [
  { ...base, no: 'DEV-PACK-STD', version: '01', issueState: 2, curVersion: false },
  { ...base, no: 'DEV-PACK-STD', version: '02', issueState: 2, curVersion: true },
  { ...base, no: 'DEV-PACK-EXP', version: '01', issueState: 2, curVersion: true },
  { ...base, no: 'DEV-PACK-DRAFT', version: '01', issueState: 0, curVersion: false },
]

export function loadDevelopmentPackingRules(): PackingRuleBasis[] {
  try {
    const stored = localStorage.getItem(packingRuleStorageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  return structuredClone(seed)
}
