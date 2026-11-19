export type CardField = {
  fileField: string; valueType: number; source: string; sourceName: string; value: string; direction: number
  start: string; length: string; defaultValue: string; script: string; memo: string
}
export type RunCard = {
  no: string; type: number; main: boolean; fileName: string; printer: string; description: string
  headerStart: string; headerEnd: string; fields: CardField[]; creator: string; createDate: string; dataStamp: number; reviser?: string; reviseDate?: string
}
export const runCardStorageKey = 'ymes.dev.prd114-run-cards.v1'
export const runCardMapStorageKey = 'ymes.dev.prd115-run-card-map.v1'

export type RunCardMapping = {
  productNo: string
  runCardNo: string
  creator: string
  createDate: string
  dataStamp: number
  reviser?: string
  reviseDate?: string
}

export function loadDevelopmentRunCardMappings(): RunCardMapping[] {
  try {
    const stored = JSON.parse(localStorage.getItem(runCardMapStorageKey) ?? 'null')
    if (Array.isArray(stored)) return stored
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  return [{ productNo: 'DEV-PRD-1002', runCardNo: 'DEV-RUNCARD-01', creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0 }]
}
export function newCardField(): CardField {
  return { fileField: '', valueType: 0, source: 'UD', sourceName: '', value: '', direction: 0, start: '', length: '', defaultValue: '', script: '', memo: '' }
}

export function loadDevelopmentRunCards(): RunCard[] {
  try {
    const stored = JSON.parse(localStorage.getItem(runCardStorageKey) ?? 'null')
    if (Array.isArray(stored)) return stored
  } catch { /* Use the fixture when DEV storage is unavailable. */ }
  return [{ no: 'DEV-RUNCARD-01', type: 0, main: true, fileName: 'dev-run-card.xlsx', printer: '', description: 'DEV 生產流程卡', headerStart: '', headerEnd: '',
    creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0, fields: [{ ...newCardField(), fileField: 'LotNo', source: 'TB', sourceName: 'TBLWIPLOTBASIS', value: 'LotNo' }] }]
}
