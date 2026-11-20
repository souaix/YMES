// Native ET106 wire decoders and raw structural keys. No browser-generated CAS.
export interface Address {
  shipAddr: string; customerNo: string; telNo: string | null; contactorName: string | null
  shipCustomerName: string | null; creator: string | null; createDate: string | null; description: string | null
  flow: string; declarationFlag: string; overseaFlag: string | null; faxNo: string | null; email: string | null
  shipAddr2: string | null; country: string | null; companyName: string | null; takerNo: string | null
  forwarderNo: string | null; broker: string | null; dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
export type AddressWrite = Pick<Address, 'shipAddr' | 'telNo' | 'contactorName' | 'shipCustomerName' | 'description' |
  'flow' | 'declarationFlag' | 'overseaFlag' | 'faxNo' | 'email' | 'shipAddr2' | 'country' | 'companyName' | 'takerNo' | 'forwarderNo' | 'broker'>
export interface DeclarationKey { shipTo: string; flow: string; overseaFlag: string; declarationFlag: string; bondFlag: string; handCarryFlag: string }
export interface Declaration extends DeclarationKey {
  customerNo: string; declarationWay: string; creator: string | null; createDate: string | null
  dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
export interface DeclarationGroup { key: DeclarationKey; aggregateVersion: string; rowCount: number }
export interface DeclarationList { items: Declaration[]; groups: DeclarationGroup[] }
export interface DeclarationSaved { group: DeclarationGroup; items: Declaration[]; affectedRows: number; inserted?: Declaration }
export interface CustomerOption { customerNo: string; customerName: string | null; customerSName: string | null; issueState: number | null }
export interface ReferenceOption { code: string; name: string | null; issueState: number | null }
export type ReferenceKind = 'taker' | 'forwarder' | 'broker'
export const addressFields = ['shipAddr', 'telNo', 'contactorName', 'shipCustomerName', 'description', 'flow', 'declarationFlag',
  'overseaFlag', 'faxNo', 'email', 'shipAddr2', 'country', 'companyName', 'takerNo', 'forwarderNo', 'broker'] as const
export const keyFields = ['shipTo', 'flow', 'overseaFlag', 'declarationFlag', 'bondFlag', 'handCarryFlag'] as const
export const addressLimits: ReadonlyArray<readonly [keyof AddressWrite, number, string]> = [
  ['shipAddr', 255, '出貨地址'], ['telNo', 25, '電話'], ['contactorName', 25, '連絡人'], ['shipCustomerName', 30, '出貨客戶名稱'],
  ['description', 255, '說明'], ['flow', 10, '流程'], ['declarationFlag', 1, '報關'], ['overseaFlag', 1, '海外'],
  ['faxNo', 25, '傳真'], ['email', 255, 'Email'], ['shipAddr2', 500, '出貨地址2'], ['country', 50, '國家'],
  ['companyName', 150, '公司名稱'], ['takerNo', 50, '取貨公司'], ['forwarderNo', 50, '貨運行'], ['broker', 50, '報關行']]
export function object(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('伺服器回應不完整。')
  return value as Record<string, unknown>
}
function text(row: Record<string, unknown>, key: string, nullable = true): string | null {
  const value = row[key]
  if (!Object.hasOwn(row, key) || !(typeof value === 'string' || nullable && value === null)) throw new Error('伺服器文字欄位回應不完整：' + key)
  return value as string | null
}
function required(row: Record<string, unknown>, key: string): string { return text(row, key, false) as string }
function integer(row: Record<string, unknown>, key: string, nullable = true): number | null {
  const value = row[key]
  if (!Object.hasOwn(row, key) || !(typeof value === 'number' && Number.isSafeInteger(value) || nullable && value === null)) throw new Error('伺服器數值欄位回應不完整：' + key)
  return value as number | null
}
function stamp(row: Record<string, unknown>): number | null {
  const value = integer(row, 'dataStamp')
  if (value !== null && Math.abs(value) > 99999999999) throw new Error('伺服器版本超過來源範圍。')
  return value
}
function date(row: Record<string, unknown>, key: string): string | null {
  const value = text(row, key)
  if (value !== null && (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/.test(value) || Number.isNaN(Date.parse(value)))) throw new Error('伺服器日期欄位回應不完整：' + key)
  return value
}
function array(row: Record<string, unknown>, key: string): unknown[] {
  if (!Array.isArray(row[key])) throw new Error('伺服器清單回應不完整：' + key)
  return row[key] as unknown[]
}
export function addressKey(row: Pick<Address, 'customerNo' | 'shipAddr'>): string { return JSON.stringify([row.customerNo, row.shipAddr]) }
export function declarationKey(key: DeclarationKey): DeclarationKey {
  return { shipTo: key.shipTo, flow: key.flow, overseaFlag: key.overseaFlag, declarationFlag: key.declarationFlag, bondFlag: key.bondFlag, handCarryFlag: key.handCarryFlag }
}
export function groupKey(owner: string, key: DeclarationKey): string { return JSON.stringify([owner, ...keyFields.map(field => key[field])]) }
export function sameGroup(owner: string, row: Declaration, key: DeclarationKey): boolean { return row.customerNo === owner && groupKey(owner, row) === groupKey(owner, key) }
export function readAddress(value: unknown): Address {
  const row = object(value)
  return { shipAddr: required(row, 'shipAddr'), customerNo: required(row, 'customerNo'), telNo: text(row, 'telNo'), contactorName: text(row, 'contactorName'),
    shipCustomerName: text(row, 'shipCustomerName'), creator: text(row, 'creator'), createDate: date(row, 'createDate'), description: text(row, 'description'),
    flow: required(row, 'flow'), declarationFlag: required(row, 'declarationFlag'), overseaFlag: text(row, 'overseaFlag'), faxNo: text(row, 'faxNo'), email: text(row, 'email'),
    shipAddr2: text(row, 'shipAddr2'), country: text(row, 'country'), companyName: text(row, 'companyName'), takerNo: text(row, 'takerNo'), forwarderNo: text(row, 'forwarderNo'),
    broker: text(row, 'broker'), dataStamp: stamp(row), reviser: text(row, 'reviser'), reviseDate: date(row, 'reviseDate') }
}
export function readAddresses(value: unknown, owner: string): Address[] {
  const rows = array(object(value), 'items').map(readAddress)
  if (rows.some(row => row.customerNo !== owner) || new Set(rows.map(addressKey)).size !== rows.length) throw new Error('出貨地址原始客戶或主鍵回應不一致。')
  return rows
}
export function readDeclaration(value: unknown): Declaration {
  const row = object(value)
  return { customerNo: required(row, 'customerNo'), ...readKey(row), declarationWay: required(row, 'declarationWay'),
    creator: text(row, 'creator'), createDate: date(row, 'createDate'), dataStamp: stamp(row), reviser: text(row, 'reviser'), reviseDate: date(row, 'reviseDate') }
}
export function readKey(value: unknown): DeclarationKey {
  const row = object(value)
  return { shipTo: required(row, 'shipTo'), flow: required(row, 'flow'), overseaFlag: required(row, 'overseaFlag'),
    declarationFlag: required(row, 'declarationFlag'), bondFlag: required(row, 'bondFlag'), handCarryFlag: required(row, 'handCarryFlag') }
}
function readGroup(value: unknown): DeclarationGroup {
  const row = object(value), key = readKey(row.key), aggregateVersion = required(row, 'aggregateVersion'), rowCount = integer(row, 'rowCount', false)!
  if (!/^et106-decl-v1:[0-9a-f]{64}$/.test(aggregateVersion) || rowCount <= 0) throw new Error('報關群組版本或筆數回應不完整。')
  return { key, aggregateVersion, rowCount }
}
export function readDeclarations(value: unknown, owner: string): DeclarationList {
  const row = object(value), items = array(row, 'items').map(readDeclaration), groups = array(row, 'groups').map(readGroup)
  const counts = new Map<string, number>()
  for (const item of items) {
    if (item.customerNo !== owner) throw new Error('報關資料的原始客戶回應不一致。')
    const key = groupKey(owner, item); counts.set(key, (counts.get(key) ?? 0) + 1)
  }
  if (groups.length !== counts.size || new Set(groups.map(group => groupKey(owner, group.key))).size !== groups.length ||
      groups.some(group => counts.get(groupKey(owner, group.key)) !== group.rowCount)) throw new Error('報關群組與完整資料列回應不一致。')
  return { items, groups }
}
export function readSavedDeclaration(value: unknown, owner: string, creating: boolean): DeclarationSaved {
  const row = object(value), group = readGroup(row.group), items = array(row, 'items').map(readDeclaration), affectedRows = integer(row, 'affectedRows', false)!
  if (items.length !== group.rowCount || items.some(item => !sameGroup(owner, item, group.key)) || affectedRows !== (creating ? 1 : group.rowCount)) throw new Error('報關保存結果的整組範圍或實際筆數不一致。')
  const inserted = creating ? readDeclaration(row.inserted) : undefined
  if (inserted && (!sameGroup(owner, inserted, group.key) || !items.some(item => JSON.stringify(item) === JSON.stringify(inserted)))) throw new Error('新增報關回應未包含原始新增列。')
  return { group, items, affectedRows, ...(inserted ? { inserted } : {}) }
}
export function readDeletedDeclaration(value: unknown, owner: string, expected: DeclarationGroup): number {
  const row = object(value), key = readKey(row.key), items = array(row, 'items'), count = integer(row, 'rowCount', false), affectedRows = integer(row, 'affectedRows', false)
  if (groupKey(owner, key) !== groupKey(owner, expected.key) || count !== 0 || items.length || row.aggregateVersion !== null || !Object.hasOwn(row, 'aggregateVersion') || affectedRows !== expected.rowCount) throw new Error('報關刪除結果的整組範圍或實際筆數不一致。')
  return affectedRows!
}
export function readCustomers(value: unknown): CustomerOption[] {
  const items = array(object(value), 'items').map(value => {
    const row = object(value)
    return { customerNo: required(row, 'customerNo'), customerName: text(row, 'customerName'), customerSName: text(row, 'customerSName'), issueState: integer(row, 'issueState') }
  })
  if (items.some(item => item.issueState !== 2) || new Set(items.map(item => item.customerNo)).size !== items.length) throw new Error('客戶查找回應不是唯一已簽核資料。')
  return items
}
export function readReferences(value: unknown, kind: ReferenceKind): ReferenceOption[] {
  const field = kind === 'taker' ? 'takerNo' : kind === 'forwarder' ? 'forwarderNo' : 'brokerNo'
  const rows = array(object(value), 'items').map(value => { const row = object(value); return { code: required(row, field), name: text(row, 'contactorName'), issueState: integer(row, 'issueState') } })
  if (new Set(rows.map(row => row.code)).size !== rows.length) throw new Error('查找回應含重複識別值。')
  return rows.filter(row => row.issueState === 2)
}
export function addressWrite(row: Address): AddressWrite {
  return { shipAddr: row.shipAddr, telNo: row.telNo, contactorName: row.contactorName, shipCustomerName: row.shipCustomerName,
    description: row.description, flow: row.flow, declarationFlag: row.declarationFlag, overseaFlag: row.overseaFlag, faxNo: row.faxNo,
    email: row.email, shipAddr2: row.shipAddr2, country: row.country, companyName: row.companyName, takerNo: row.takerNo, forwarderNo: row.forwarderNo, broker: row.broker }
}
export function blankAddress(): AddressWrite { return { shipAddr: '', telNo: null, contactorName: null, shipCustomerName: '', description: null, flow: '', declarationFlag: '1', overseaFlag: '1', faxNo: null, email: null, shipAddr2: null, country: null, companyName: null, takerNo: null, forwarderNo: null, broker: null } }
export function codePoints(value: string): number { return Array.from(value).length }
