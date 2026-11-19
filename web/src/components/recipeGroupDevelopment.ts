export type RecipeState = -1 | 0 | 1 | 2
type Audit = { creator: string; createDate: string; reviser?: string; reviseDate?: string; dataStamp: number }
export type RecipeDetail = Audit & { opNo: string; recipeNo: string; recipeValue: string; sequence: number; description: string }
export type RecipeGroup = Audit & { recipeGroup: string; recipeVersion: number; description: string; issueState: RecipeState; curVersion: 0 | 1; details: RecipeDetail[] }

export const recipeGroupStorageKey = 'ymes.dev.prd105-recipe-groups.v1'
const audit = { creator: 'DEV001', createDate: '2026-11-19T08:00:00', dataStamp: 0 }
const fixtures: RecipeGroup[] = [
  { ...audit, recipeGroup: 'DEV-RG-01', recipeVersion: 1, description: 'DEV 產品配方範例 A', issueState: 2, curVersion: 1, details: [
    { ...audit, opNo: 'DEV-OP-10', recipeNo: 'DEV-RECIPE-001', recipeValue: 'DEV-SET-01', sequence: 1, description: 'DEV 組裝參數' },
  ] },
  { ...audit, recipeGroup: 'DEV-RG-02', recipeVersion: 3, description: 'DEV 產品配方範例 B', issueState: 2, curVersion: 1, details: [] },
  { ...audit, recipeGroup: 'DEV-RG-DRAFT', recipeVersion: 1, description: 'DEV 未核准的參數群組', issueState: 0, curVersion: 0, details: [] },
]

export function loadDevelopmentRecipeGroups(): RecipeGroup[] {
  try {
    const stored = localStorage.getItem(recipeGroupStorageKey)
    const parsed = stored ? JSON.parse(stored) : null
    if (Array.isArray(parsed)) return parsed
  } catch { /* Use fixtures when DEV storage is unavailable. */ }
  return structuredClone(fixtures)
}
