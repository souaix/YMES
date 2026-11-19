type Row = Record<string, any>

export type ProductTypeRow = {
  productType: string
  productTypeCode: string
  description: string
  issueState: -1 | 0 | 1 | 2
  creator: string
  createDate: string
  reviser?: string
  reviseDate?: string
  dataStamp: number
  processes?: Row[]
  properties?: Row[]
  parameters?: { ops: Row[]; attributes: Row[]; rules: Row[]; equipments: Row[] }
}

export const productTypeStorageKey = 'ymes.dev.prd101-product-types.v1'
const developmentProductTypes: ProductTypeRow[] = [
  { productType: 'DEV-FINISHED', productTypeCode: 'FG', description: '成品類別', issueState: 2, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 1 },
  { productType: 'DEV-SEMIFINISHED', productTypeCode: 'WIP', description: '半成品類別', issueState: 2, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 1 },
  { productType: 'DEV-RAW', productTypeCode: 'RM', description: '原料類別', issueState: 2, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 1 },
  { productType: 'DEV-NEW-TYPE', productTypeCode: 'N1', description: '待核准類別', issueState: 0, creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0,
    processes: [{ moTypeNo: '1', psNo: 'DEV-PS-10', psOrder: 10, processNo: 'DEV-PROC-01', processVersion: '01', defaultProcess: 1 }] },
]

export function loadDevelopmentProductTypes(): ProductTypeRow[] {
  try {
    const stored = localStorage.getItem(productTypeStorageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch {
    // A new DEV session can still use the fixture when storage is unavailable.
  }
  return structuredClone(developmentProductTypes)
}
