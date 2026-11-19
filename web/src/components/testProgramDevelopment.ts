export type SoftwareBin = { plan: string; software: string; hardware: string; fail: boolean }
export type HardwareBin = { no: string; fail: boolean; description: string; os?: boolean; color?: string }
export type TestProgram = {
  no: string; version: string; description: string; manageSoftware: boolean; state: number; current: boolean
  hardwareBins: HardwareBin[]; softwareBins: SoftwareBin[]; files: string[]; creator: string; reviser?: string; stamp: number; programType?: string
}

export const developmentCpProgramTypes = ['DEV-CP', 'DEV-WAFER']

export function developmentProgramKey(cp: boolean): string {
  return cp ? 'ymes.dev.pdp02-cp-programs.v1' : 'ymes.dev.prd202-ft-programs.v1'
}

export function newDevelopmentProgram(cp: boolean): TestProgram {
  return { no: '', version: '01', description: '', manageSoftware: false, state: 0, current: false,
    hardwareBins: [], softwareBins: [], files: [], creator: '', stamp: 0, programType: cp ? developmentCpProgramTypes[0] : undefined }
}

export function loadDevelopmentPrograms(cp: boolean): TestProgram[] {
  try {
    const stored = JSON.parse(localStorage.getItem(developmentProgramKey(cp)) ?? 'null')
    if (Array.isArray(stored)) return stored
  } catch { /* Use DEV fixtures when local storage is unavailable. */ }
  if (cp) return [
    { ...newDevelopmentProgram(cp), no: 'DEV-CP-01', description: 'DEV 核准晶圓測試程式', state: 2, current: true, creator: 'DEV001',
      hardwareBins: [{ no: 'BIN01', fail: false, os: false, description: '', color: '#008000' }] },
    { ...newDevelopmentProgram(cp), no: 'DEV-CP-02', description: 'DEV 待核准晶圓測試程式', creator: 'DEV001' },
  ]
  return [
    { ...newDevelopmentProgram(cp), no: 'DEV-FT-01', description: 'DEV 核准測試程式', state: 2, current: true, creator: 'DEV001' },
    { ...newDevelopmentProgram(cp), no: 'DEV-FT-02', description: 'DEV 待核准測試程式', manageSoftware: true, creator: 'DEV001' },
  ]
}
