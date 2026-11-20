// ET101 proposed service only. No App/Vue integration, DEV storage, fallback
// saved DTO, import success or workflow provider is supplied by this file.
export interface CustomerDto {
  customerNo: string
  customerName: string
  customerSName: string
  telNo: string | null
  faxNo: string | null
  www: string | null
  taxCode: string | null
  address: string | null
  director: string | null
  creator: string | null
  description: string | null
  address2: string | null
  country: string | null
  status: string | null
  currency: string | null
  invTypeNo: string | null
  invAddr: string | null
  invAddr2: string | null
  invCustNo: string | null
  invRemark: string | null
  custType: string | null
  arType: string | null
  freightTerms: string | null
  paymentTerms: string | null
  acctNo: string | null
  deliveryTerm: string | null
  forwarderAssigned: string | null
  brokerAssigned: string | null
  insuranceCoverage: string | null
  reviser: string | null
  customerEName: string | null
  erpNo: string | null
  sapFiskn: string | null
  xxName1: string | null
  xxName2: string | null
  xxRebarcode: string | null
  xxSupplydata: string | null
  createDate: string | null
  reviseDate: string | null
  issueState: number | null
  dataStamp: number | null
}
export interface CustomerWrite {
  customerNo: string
  customerName: string | null
  customerSName: string | null
  customerEName: string | null
  telNo: string | null
  faxNo: string | null
  www: string | null
  taxCode: string | null
  address: string | null
  address2: string | null
  director: string | null
  country: string | null
  description: string | null
  status: string | null
  currency: string | null
  invTypeNo: string | null
  invAddr: string | null
  invAddr2: string | null
  invCustNo: string | null
  invRemark: string | null
  custType: string | null
  arType: string | null
  freightTerms: string | null
  paymentTerms: string | null
  acctNo: string | null
  deliveryTerm: string | null
  forwarderAssigned: string | null
  brokerAssigned: string | null
  insuranceCoverage: string | null
}
export interface CustomerExpected { issueState: number | null; dataStamp: number | null }
export type CustomerEdit = CustomerWrite & CustomerExpected
export interface BrokerOption { brokerNo: string; contactorName: string | null; issueState: number | null }
export interface ForwarderOption { forwarderNo: string; contactorName: string | null; brokerNo: string; issueState: number | null }
export type CustomerDelete =
  | { kind: 'deleted'; customerNo: string; deletedAddressRows: number; deletedContactorRows: number; item: null }
  | { kind: 'retired'; customerNo: string; deletedAddressRows: 0; deletedContactorRows: 0; item: CustomerDto }

export const et101Capabilities = Object.freeze({ approve: true, import: false, print: false, copyChildren: false })
const nullableText = ["telNo","faxNo","www","taxCode","address","director","creator","description","address2","country","status","currency","invTypeNo","invAddr","invAddr2","invCustNo","invRemark","custType","arType","freightTerms","paymentTerms","acctNo","deliveryTerm","forwarderAssigned","brokerAssigned","insuranceCoverage","reviser","customerEName","erpNo","sapFiskn","xxName1","xxName2","xxRebarcode","xxSupplydata"] as const
const writableText = ["customerName","customerSName","customerEName","telNo","faxNo","www","taxCode","address","address2","director","country","description","status","currency","invTypeNo","invAddr","invAddr2","invCustNo","invRemark","custType","arType","freightTerms","paymentTerms","acctNo","deliveryTerm","forwarderAssigned","brokerAssigned","insuranceCoverage"] as const
const customerKeys = ['customerNo', 'customerName', 'customerSName', ...nullableText, 'createDate', 'reviseDate', 'issueState', 'dataStamp'] as const
const maxStamp = 99_999_999_999

export class Et101ServiceError extends Error {
  constructor(message: string, readonly status: number | null, readonly unknownCommit: boolean) { super(message); this.name = 'Et101ServiceError' }
}
const own = (value: object, key: string): boolean => Object.prototype.hasOwnProperty.call(value, key)
function record(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('客戶服務傳回格式不符契約。')
  return value as Record<string, unknown>
}
function exactKeys(value: Record<string, unknown>, keys: readonly string[]): void {
  if (Object.keys(value).length !== keys.length || keys.some(key => !own(value, key))) throw new Error('客戶服務缺少來源欄位或包含未知欄位。')
}
function text(value: unknown, nullable = false): string | null {
  if (value === null && nullable) return null
  if (typeof value !== 'string') throw new Error('客戶來源文字欄位格式不符契約。')
  // for...of preserves complete supplementary scalars; lone UTF16 is invalid.
  for (const character of value) {
    const scalar = character.codePointAt(0)!
    if (scalar >= 0xD800 && scalar <= 0xDFFF) throw new Error('客戶來源文字包含無效字元。')
  }
  return value
}
function state(value: unknown): number | null {
  if (value === null) return null
  if (typeof value !== 'number' || !Number.isInteger(value) || value < -9 || value > 9) throw new Error('原始客戶狀態格式不符來源。')
  return value
}
function stamp(value: unknown): number | null {
  if (value === null) return null
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || Math.abs(value) > maxStamp) throw new Error('客戶版本格式不符來源。')
  return value
}
function date(value: unknown): string | null {
  if (value === null) return null
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z$/.test(value) || !Number.isFinite(Date.parse(value)))
    throw new Error('客戶稽核日期必須為真實UTC日期或NULL。')
  return value // Keep source fraction precision; do not regenerate a timestamp.
}
export function decodeCustomer(value: unknown): CustomerDto {
  const row = record(value); exactKeys(row, customerKeys)
  text(row.customerNo); text(row.customerName); text(row.customerSName)
  for (const key of nullableText) text(row[key], true)
  state(row.issueState); stamp(row.dataStamp); date(row.createDate); date(row.reviseDate)
  return { ...row } as unknown as CustomerDto
}
function items(value: unknown): unknown[] {
  const envelope = record(value); exactKeys(envelope, ['items'])
  if (!Array.isArray(envelope.items)) throw new Error('客戶來源清單格式不符契約。')
  return envelope.items
}
export function decodeCustomerList(value: unknown): CustomerDto[] { return items(value).map(decodeCustomer) }
export function decodeCustomerDelete(value: unknown): CustomerDelete {
  const result = record(value)
  exactKeys(result, ['kind', 'customerNo', 'deletedAddressRows', 'deletedContactorRows', 'item'])
  const customerNo = text(result.customerNo) as string
  const count = (value: unknown): number => {
    if (typeof value !== 'number' || !Number.isSafeInteger(value) || value < 0) throw new Error('刪除列數不符契約。')
    return value
  }
  const deletedAddressRows = count(result.deletedAddressRows), deletedContactorRows = count(result.deletedContactorRows)
  if (result.kind === 'deleted' && result.item === null)
    return { kind: 'deleted', customerNo, deletedAddressRows, deletedContactorRows, item: null }
  if (result.kind === 'retired' && deletedAddressRows === 0 && deletedContactorRows === 0) {
    const item = decodeCustomer(result.item)
    if (item.customerNo !== customerNo || item.issueState !== -1) throw new Error('停用回傳列與操作身份不符。')
    return { kind: 'retired', customerNo, deletedAddressRows: 0, deletedContactorRows: 0, item }
  }
  throw new Error('刪除回傳結果不符已提交的正式契約。')
}
function decodeBrokers(value: unknown): BrokerOption[] {
  return items(value).map(value => {
    const row = record(value); exactKeys(row, ['brokerNo', 'contactorName', 'issueState'])
    return { brokerNo: text(row.brokerNo) as string, contactorName: text(row.contactorName, true), issueState: state(row.issueState) }
  })
}
function decodeForwarders(value: unknown): ForwarderOption[] {
  return items(value).map(value => {
    const row = record(value); exactKeys(row, ['forwarderNo', 'contactorName', 'brokerNo', 'issueState'])
    return { forwarderNo: text(row.forwarderNo) as string, contactorName: text(row.contactorName, true),
      brokerNo: text(row.brokerNo) as string, issueState: state(row.issueState) }
  })
}
export function blankCustomerWrite(): CustomerWrite {
  const body = Object.fromEntries(writableText.map(key => [key, null])) as unknown as CustomerWrite
  return { ...body, customerNo: '', customerName: '', customerSName: '' }
}
export function copyCustomerHeader(row: CustomerDto): CustomerWrite {
  const body = blankCustomerWrite()
  for (const key of writableText) body[key] = row[key]
  body.customerNo = ''
  return body // No hidden fields, audit, tokens or child resources.
}
export function editCustomerBody(row: CustomerDto): CustomerEdit {
  const body = copyCustomerHeader(row)
  return { ...body, customerNo: row.customerNo, issueState: row.issueState, dataStamp: row.dataStamp }
}
function expected(input: CustomerExpected): CustomerExpected {
  const value = record(input); exactKeys(value, ['issueState', 'dataStamp'])
  return { issueState: state(value.issueState), dataStamp: stamp(value.dataStamp) }
}
function writeBody(input: CustomerWrite, editing: boolean): CustomerWrite | CustomerEdit {
  const value = record(input)
  exactKeys(value, ['customerNo', ...writableText, ...(editing ? ['issueState', 'dataStamp'] : [])])
  const body = blankCustomerWrite()
  body.customerNo = text(value.customerNo) as string
  for (const key of writableText) body[key] = text(value[key], true)
  if (!editing) return body
  return { ...body, issueState: state(value.issueState), dataStamp: stamp(value.dataStamp) }
}

export function createEt101Service(apiBaseUrl: string, origin: string, transport: typeof fetch = fetch) {
  if (!apiBaseUrl.trim()) throw new Error('目前環境尚未設定API位址。')
  const base = new URL(apiBaseUrl.replace(/\/+$/, '') + '/', origin)
  const path = (suffix = '', customerNo?: string): string => {
    const url = new URL('api/ent/customer-basis' + suffix, base)
    if (customerNo !== undefined) url.searchParams.set('customerNo', customerNo)
    return url.toString()
  }
  async function call<T>(url: string, method: string, decoder: (value: unknown) => T, body?: unknown): Promise<T> {
    const writing = method !== 'GET'
    let response: Response
    try {
      response = await transport(url, { method, credentials: 'include',
        ...(body === undefined ? {} : { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }) })
    } catch {
      throw new Et101ServiceError('無法確認伺服器結果，請先重新查詢；不要重送保存。', null, writing)
    }
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as unknown
      const p = problem && typeof problem === 'object' ? problem as Record<string, unknown> : {}
      const detail = typeof p.detail === 'string' ? p.detail : typeof p.title === 'string' ? p.title : '客戶服務未完成操作。'
      throw new Et101ServiceError(detail, response.status, writing && response.status >= 500)
    }
    try { return decoder(await response.json()) }
    catch { throw new Et101ServiceError('伺服器回應未通過來源驗證，請先核對已提交結果。', response.status, writing) }
  }
  return {
    async list(keyword?: string): Promise<CustomerDto[]> {
      const url = new URL(path()); if (keyword !== undefined) url.searchParams.set('keyword', keyword)
      return call(url.toString(), 'GET', decodeCustomerList)
    },
    get: (customerNo: string) => call(path('/item', customerNo), 'GET', decodeCustomer),
    create: (body: CustomerWrite) => call(path(), 'POST', decodeCustomer, writeBody(body, false)),
    update: (rawCustomerNo: string, body: CustomerEdit) => {
      if (body.customerNo !== rawCustomerNo) throw new Et101ServiceError('修改身份必須保留原始識別碼。', 400, false)
      return call(path('/item', rawCustomerNo), 'PUT', decodeCustomer, writeBody(body, true))
    },
    remove: (rawCustomerNo: string, tokens: CustomerExpected) =>
      call(path('/item', rawCustomerNo), 'DELETE', decodeCustomerDelete, expected(tokens)),
    approve: (rawCustomerNo: string, tokens: CustomerExpected) => {
      const body = expected(tokens)
      return call(path('/approve', rawCustomerNo), 'POST', value => {
        const saved = decodeCustomer(value)
        if (saved.customerNo !== rawCustomerNo || saved.issueState !== 2 || saved.dataStamp !== (body.dataStamp ?? 0) + 1)
          throw new Error('核准回傳身份或版本不符契約。')
        return saved
      }, body)
    },
    brokers: () => call(path('/options/brokers'), 'GET', decodeBrokers),
    forwarders: () => call(path('/options/forwarders'), 'GET', decodeForwarders),
  }
}

