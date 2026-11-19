export type Piece = { itemName: string; pieceSource: string; pieceData: string; direction: 0 | 1 | 2; startIndex: number; pieceLength: number; dataScript: string; memo: string }
export type Label = { labelNo: string; labelType: string; labelSource: number; formatFileName: string; printerNo: string; description: string; pieces: Piece[] }
export type LabelRow = Label & { creator: string; createDate: string; reviser?: string; reviseDate?: string; dataStamp: number }

export const labelStorageKey = 'ymes.dev.prd107-label-data.v1'

export function blankPiece(): Piece { return { itemName: '', pieceSource: 'UD', pieceData: '', direction: 0, startIndex: 0, pieceLength: 1, dataScript: '', memo: '' } }
export function blank(): Label { return { labelNo: '', labelType: 'SN', labelSource: 1, formatFileName: '', printerNo: '', description: '', pieces: [] } }

export function loadDevelopmentLabels(): LabelRow[] {
  try {
    const stored = localStorage.getItem(labelStorageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  const audit = { creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0 }
  const lot = { ...blankPiece(), itemName: 'LotNo', pieceSource: 'LB', pieceData: 'LotNo' }
  return [
    { ...blank(), ...audit, labelNo: 'DEV-PRD-LABEL-01', formatFileName: 'dev-product.btw', description: '產品批號範例', pieces: [lot, { ...blankPiece(), itemName: 'Title', pieceData: 'DEV 生產標籤' }] },
    { ...blank(), ...audit, labelNo: 'DEV-PRD-POST-01', labelSource: 2, formatFileName: 'dev-post.btw', description: '過帳標籤範例', pieces: [{ ...lot }] },
  ]
}
