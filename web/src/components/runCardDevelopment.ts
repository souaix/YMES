export type CardField = {
  fileField: string; valueType: number; source: string; sourceName: string; value: string; direction: number
  start: string; length: string; defaultValue: string; script: string; memo: string
}
export type RunCard = {
  no: string; type: number; main: boolean; fileName: string; printer: string; description: string
  headerStart: string; headerEnd: string; fields: CardField[]; creator: string; createDate: string; dataStamp: number; reviser?: string; reviseDate?: string
}
export const runCardStorageKey = 'ymes.dev.prd114-run-cards.v1'
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
