// ET202 formal candidate. No storage, mock success, retries or client audit.
export interface OsItemDto {
  osItemNo: string; osItemName: string; planDays: number; processNo: string; returnType: number
  description: string | null; creator: string | null; createDate: string | null
  dataStamp: number | null; reviser: string | null; reviseDate: string | null
}
export interface OsItemWrite {
  osItemNo: string; osItemName: string; planDays: number; processNo: string
  returnType: number; description: string | null
}
export interface OsItemEdit extends OsItemWrite { dataStamp: number | null }
export interface OsItemDelete { osItemNo: string; deletedCount: 1 }
export interface ProcessOption {
  processNo: string; processVersion: string; processClass: number; issueState: number; description: string | null
}
export class Et202Error extends Error {
  constructor(message: string, readonly status: number | null, readonly unknownWrite: boolean) {
    super(message); this.name = 'Et202Error'
  }
}
const own = (row: object, key: string): boolean => Object.prototype.hasOwnProperty.call(row, key)
const dtoKeys = ['osItemNo','osItemName','planDays','processNo','returnType','description','creator','createDate','dataStamp','reviser','reviseDate']
const writeKeys = ['osItemNo','osItemName','planDays','processNo','returnType','description']
function object(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('服務傳回的資料格式不完整。')
  return value as Record<string, unknown>
}
function keys(row: Record<string, unknown>, expected: readonly string[]): void {
  if (Object.keys(row).length !== expected.length || expected.some(k => !own(row, k))) throw new Error('服務傳回的欄位不完整。')
}
export function scalarLength(value: string): number {
  let length = 0
  for (const character of value) {
    const code = character.codePointAt(0)!
    if (code >= 0xD800 && code <= 0xDFFF) throw new Error('文字包含無效字元。')
    length += 1
  }
  return length
}
function text(value: unknown, nullable = false): string | null {
  if (nullable && value === null) return null
  if (typeof value !== 'string') throw new Error('服務文字欄位格式不完整。')
  scalarLength(value); return value
}
function integer(value: unknown): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value)) throw new Error('服務數值欄位格式不完整。')
  return value
}
function stamp(value: unknown): number | null {
  if (value === null) return null
  const result = integer(value)
  if (Math.abs(result) > 99_999_999_999) throw new Error('資料版本超出來源範圍。')
  return result
}
function date(value: unknown): string | null {
  if (value === null) return null
  const match = typeof value === 'string' ? /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d{1,7})?Z$/.exec(value) : null
  if (!match || typeof value !== 'string') throw new Error('稽核日期格式不完整。')
  const parsed = new Date(value), parts = [parsed.getUTCFullYear(), parsed.getUTCMonth()+1, parsed.getUTCDate(), parsed.getUTCHours(), parsed.getUTCMinutes(), parsed.getUTCSeconds()]
  if (!Number.isFinite(parsed.getTime()) || Number(match[1]) < 1 || parts.some((part, index) => part !== Number(match[index+1]))) throw new Error('稽核日期格式不完整。')
  return value
}
export function decodeOsItem(value: unknown): OsItemDto {
  const row = object(value); keys(row, dtoKeys)
  text(row.osItemNo); text(row.osItemName); integer(row.planDays); text(row.processNo); integer(row.returnType)
  for (const k of ['description','creator','reviser']) text(row[k], true)
  stamp(row.dataStamp); date(row.createDate); date(row.reviseDate)
  return { ...row } as unknown as OsItemDto // Preserve raw strings, NULLs and timestamp fractions.
}
export function decodeOsItems(value: unknown): OsItemDto[] {
  const row = object(value); keys(row, ['items'])
  if (!Array.isArray(row.items)) throw new Error('服務清單格式不完整。')
  return row.items.map(decodeOsItem)
}
function input(value: unknown, label: string, maximum: number, required: boolean, multiline = false): string | null {
  if (value === null && !required) return null
  const result = text(value) as string
  const controls = multiline ? /[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F-\u009F]/ : /[\u0000-\u001F\u007F-\u009F]/
  if (controls.test(result) || scalarLength(result) > maximum) throw new Error(`${label}最多 ${maximum} 個字元，且不可包含不合法控制字元。`)
  if (required && result.replace(/^ +| +$/g, '') === '') throw new Error(`${label}為必填。`)
  return result
}
export function validateOsItemWrite(value: OsItemWrite): OsItemWrite {
  const row = object(value); keys(row, writeKeys)
  input(row.osItemNo, '外包項目編號', 2, true); input(row.osItemName, '外包項目名稱', 50, true)
  input(row.processNo, '流程編號', 30, true); input(row.description, '說明', 255, false, true)
  if (integer(row.planDays) < 0 || (row.planDays as number) > 999) throw new Error('預計天數必須為 0 到 999 的整數。')
  if (row.returnType !== 0 && row.returnType !== 1) throw new Error('請選擇是否退回原流程。')
  return { ...value }
}
export function copyOsItem(row: OsItemDto): OsItemWrite {
  return { osItemNo: '', osItemName: row.osItemName, planDays: row.planDays, processNo: row.processNo,
    returnType: row.returnType, description: row.description }
}
export function editOsItem(row: OsItemDto): OsItemWrite { return { ...copyOsItem(row), osItemNo: row.osItemNo } }
function decodeProcesses(value: unknown): ProcessOption[] {
  if (!Array.isArray(value)) throw new Error('流程清單格式不完整。')
  return value.map(value => {
    const row = object(value)
    return { processNo: text(row.processNo) as string, processVersion: text(row.processVersion) as string,
      processClass: integer(row.processClass), issueState: integer(row.issueState), description: text(row.description, true) }
  }).filter(row => row.issueState === 2 && row.processClass === 3) // All versions; no CurrentVersion condition.
}
export function createEt202Service(apiBaseUrl: string, origin: string, transport: typeof fetch = fetch) {
  if (!apiBaseUrl.trim()) throw new Error('目前環境尚未設定 API 位址。')
  const base = new URL(apiBaseUrl.replace(/\/+$/, '') + '/', origin)
  if (!['https:', 'http:'].includes(base.protocol) || base.username || base.password || base.search || base.hash) throw new Error('API 位址格式不正確。')
  const path = (item = false, no?: string): URL => {
    const url = new URL('api/ent/os-items' + (item ? '/item' : ''), base)
    if (no !== undefined) url.searchParams.set('osItemNo', no)
    return url
  }
  async function call<T>(url: URL, method: string, decoder: (value: unknown) => T, body?: unknown, signal?: AbortSignal): Promise<T> {
    const writing = method !== 'GET'; let response: Response
    try {
      response = await transport(url, { method, credentials: 'include', signal,
        ...(body === undefined ? {} : { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }) })
    } catch { throw new Et202Error(writing ? '寫入結果尚未確認，請先重新查詢核對，勿重複送出。' : '無法取得資料，請重新查詢。', null, writing) }
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as Record<string, unknown> | null
      const message = typeof problem?.detail === 'string' ? problem.detail : typeof problem?.title === 'string' ? problem.title : `操作未完成（${response.status}）。`
      throw new Et202Error(message, response.status, writing && response.status >= 500)
    }
    if (response.status !== 200) throw new Et202Error('服務未回傳完整的操作結果，請重新查詢核對。', response.status, writing)
    try { return decoder(await response.json()) }
    catch { throw new Et202Error('服務回應未通過核對，請重新查詢實際結果。', response.status, writing) }
  }
  return {
    list: (keyword: string, signal?: AbortSignal) => {
      input(keyword, '查詢編號', 2, false)
      return call(path(false, keyword), 'GET', decodeOsItems, undefined, signal)
    },
    get: (rawNo: string, signal?: AbortSignal) => {
      input(rawNo, '外包項目編號', 2, true)
      return call(path(true, rawNo), 'GET', decodeOsItem, undefined, signal)
    },
    create: (body: OsItemWrite) => call(path(), 'POST', decodeOsItem, validateOsItemWrite(body)),
    update: (rawNo: string, body: OsItemWrite, dataStamp: number | null) => {
      if (body.osItemNo !== rawNo) throw new Et202Error('修改時必須保留原始編號。', 422, false)
      const expected = stamp(dataStamp)
      return call(path(true, rawNo), 'PUT', value => {
        const saved = decodeOsItem(value)
        if (saved.osItemNo !== rawNo || saved.dataStamp !== (expected === null ? 0 : expected) + 1) throw new Error('儲存回傳身份或版本不符。')
        return saved
      }, { ...validateOsItemWrite(body), dataStamp: expected })
    },
    remove: (rawNo: string, dataStamp: number | null) => call(path(true, rawNo), 'DELETE', value => {
      const row = object(value); keys(row, ['osItemNo', 'deletedCount'])
      if (row.osItemNo !== rawNo || row.deletedCount !== 1) throw new Error('未取得已確認的刪除列數。')
      return { osItemNo: rawNo, deletedCount: 1 } as OsItemDelete
    }, { dataStamp: stamp(dataStamp) }),
    processes: (signal?: AbortSignal) => call(new URL('api/processes?issueState=2', base), 'GET', decodeProcesses, undefined, signal),
  }
}
