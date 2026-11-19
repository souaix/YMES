export type UploadFileType = {
  type: number
  host: string
  account: string
  port: number
  path: string
  description: string
  state: number
  stamp: number
}

export const uploadFileTypeKey = 'ymes.dev.pdb04-upload-file-types.v1'
export const uploadFileOptions = [{ type: 50, label: 'CP 測試程式' }, { type: 51, label: 'FT 測試程式' }, { type: 90001, label: 'DEV 測試類型' }]

export function loadDevelopmentUploadFileTypes(): UploadFileType[] {
  try {
    const saved = JSON.parse(localStorage.getItem(uploadFileTypeKey) ?? 'null')
    if (Array.isArray(saved)) return saved
  } catch { /* Use DEV fixture settings when storage is unavailable. */ }
  return uploadFileOptions.filter(option => [50, 51].includes(option.type)).map(({ type, label }) => ({
    type, host: 'ftp.example.invalid', account: 'DEV-FILE', port: 21, path: '/MesFileManagement',
    description: label, state: 2, stamp: 0,
  }))
}
