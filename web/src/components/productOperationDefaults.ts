import { seedRelations, type ProductRelation, type ProductRow } from './productBasisDevelopment'

export type DefaultSelection = {
  parameter: boolean; description: boolean; qc: boolean; businessRule: boolean; resource: boolean; subOp: boolean
}
export type DefaultMode = 'default' | 'amend'
const template = seedRelations['DEV-PRD-1001|01']
const templateOps = template.parameters?.ops ?? []

export const defaultOperations = templateOps.map(row => ({ opNo: String(row.opNo), opName: String(row.opName) }))

function merge<T>(current: T[], incoming: T[], scoped: (row: T) => boolean, key: (row: T) => string, mode: DefaultMode): T[] {
  const keep = mode === 'default' ? current.filter(row => !scoped(row)) : current
  const found = new Set(keep.map(key))
  return [...keep, ...incoming.filter(row => {
    const id = key(row)
    if (found.has(id)) return false
    found.add(id)
    return true
  })]
}

export function operationTargets(relation: ProductRelation, mode: DefaultMode, allSegment: boolean, selected: string[]): string[] {
  if (!relation.processes.length) return []
  const segments = new Set(relation.processes.map(row => row.psNo))
  const existing = relation.parameters?.ops ?? []
  const segmentDefaults = templateOps.filter(row => segments.has(row.psNo))
  const candidates = mode === 'amend' || allSegment || !existing.length ? segmentDefaults
    : segmentDefaults.filter(row => existing.some(op => row.opNo === op.opNo))
  return [...new Set(candidates.map(row => String(row.opNo)).filter(op => !selected.length || selected.includes(op)))].filter(Boolean)
}

export function applyOperationDefaults(product: ProductRow, relation: ProductRelation, mode: DefaultMode, flags: DefaultSelection, targets: string[]): ProductRelation {
  const targetSet = new Set(targets)
  const scoped = (row: ProductRow) => targetSet.has(String(row.opNo))
  const opKey = (row: ProductRow) => String(row.opNo)
  const identity = { productNo: product.productNo, productVersion: product.productVersion }
  const fromTemplate = (rows: ProductRow[] | undefined): ProductRow[] => (rows ?? []).filter(scoped)
    .map(row => ({ ...structuredClone(row), ...identity }))
  const next = { ...relation }

  if (flags.parameter) {
    const parameters = relation.parameters ?? { ops: [], attributes: [], rules: [], equipments: [] }
    const oldSerials = new Map(parameters.ops.map(row => [opKey(row), String(row.serialNo)]))
    const sourceOps = fromTemplate(templateOps).map(row => ({ ...row, serialNo: oldSerials.get(opKey(row)) || `DEV:${product.productNo}:${product.productVersion}:${row.opNo}` }))
    const sourceSerials = new Map(templateOps.map(row => [String(row.serialNo), opKey(row)]))
    const destinationSerials = new Map(sourceOps.map(row => [opKey(row), String(row.serialNo)]))
    const scopedSerials = new Set([...oldSerials.entries(), ...destinationSerials.entries()].filter(([op]) => targetSet.has(op)).map(([, serial]) => serial))
    const scopedDetail = (row: ProductRow) => scopedSerials.has(String(row.serialNo))
    const details = (rows: ProductRow[] = []) => rows.flatMap(row => {
      const serialNo = destinationSerials.get(sourceSerials.get(String(row.serialNo)) ?? '')
      return serialNo ? [{ ...structuredClone(row), serialNo }] : []
    })
    const keepUnknownOps = (row: ProductRow) => scoped(row) && destinationSerials.has(opKey(row))
    next.parameters = {
      ops: merge(parameters.ops, sourceOps, keepUnknownOps, opKey, mode),
      attributes: merge(parameters.attributes, details(template.parameters?.attributes), scopedDetail, row => JSON.stringify([row.serialNo, row.attribNo, row.ecnNo ?? '', row.equipmentType ?? '']), mode),
      rules: merge(parameters.rules, details(template.parameters?.rules), scopedDetail, row => JSON.stringify([row.serialNo, row.ruleNo]), mode),
      equipments: merge(parameters.equipments, details(template.parameters?.equipments), scopedDetail, row => JSON.stringify([row.serialNo, row.equipmentNo]), mode),
    }
  }
  if (flags.description) next.opDescriptions = merge(relation.opDescriptions ?? [], fromTemplate(template.opDescriptions), scoped, opKey, mode)
  if (flags.resource) next.opAreas = merge(relation.opAreas ?? [], fromTemplate(template.opAreas), scoped, row => JSON.stringify([row.opNo, row.areaNo]), mode)
  if (flags.qc) next.qcItems = merge(relation.qcItems ?? [], fromTemplate(template.qcItems), scoped, row => JSON.stringify([row.opNo, row.qcItemNo]), mode)
  if (flags.subOp) next.subOps = merge(relation.subOps ?? [], fromTemplate(template.subOps), scoped, row => JSON.stringify([row.opNo, row.subOpSequence]), mode)
  if (flags.businessRule) next.businessRules = { operations: merge(relation.businessRules?.operations ?? [],
    structuredClone((template.businessRules?.operations ?? []).filter(scoped)), scoped, opKey, mode) }
  return next
}

export function copyOperationSettings(product: ProductRow, source: ProductRelation, target: ProductRelation, flags: DefaultSelection, copyProcess: boolean): ProductRelation {
  const next = { ...target }
  const identity = { productNo: product.productNo, productVersion: product.productVersion }
  const copyRows = (rows?: ProductRow[]): ProductRow[] => (rows ?? []).map(row => ({ ...structuredClone(row), ...identity }))
  if (copyProcess) next.processes = copyRows(source.processes)
  if (flags.parameter) {
    const parameters = source.parameters
    const serials = new Map<string, string>()
    const ops = copyRows(parameters?.ops).map((row, index) => {
      const serialNo = `DEV:${product.productNo}:${product.productVersion}:${index + 1}`
      serials.set(String(row.serialNo), serialNo)
      return { ...row, serialNo }
    })
    const details = (rows: ProductRow[] = []): ProductRow[] => rows.flatMap(row => {
      const serialNo = serials.get(String(row.serialNo))
      return serialNo ? [{ ...structuredClone(row), serialNo }] : []
    })
    next.parameters = { ops, attributes: details(parameters?.attributes), rules: details(parameters?.rules), equipments: details(parameters?.equipments) }
  }
  if (flags.description) next.opDescriptions = copyRows(source.opDescriptions)
  if (flags.resource) next.opAreas = copyRows(source.opAreas)
  if (flags.qc) next.qcItems = copyRows(source.qcItems)
  if (flags.subOp) next.subOps = copyRows(source.subOps)
  if (flags.businessRule) next.businessRules = { operations: structuredClone(source.businessRules?.operations ?? []) }
  return next
}
