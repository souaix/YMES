import { decodeCustomerList } from './et101'

export interface SwrDto {
  swrNo: string; swrType: number; status: number; customerNo: string; opNo: string
  deviceNo: string | null; startTime: string | null; endTime: string | null
  customerLotNo: string | null; moNo: string | null; swrDesc: string
  creator: string | null; createDate: string | null
  attachment: { sourceHaveAttachFile: 'Y' | 'N'; blobLength: number | null; downloadAvailable: boolean }
}
export interface SwrRow { item: SwrDto; lotNo: string | null }
export interface SwrList { items: SwrRow[]; limit: number; hasMore: boolean }
export interface SwrItem { item: SwrDto; details: { swrNo: string; lotNo: string }[] }
export type FilterKey = 'swrType' | 'customerNo' | 'lotNo' | 'deviceNo' | 'opNo' | 'customerLotNo' | 'moNo'
export interface SwrQuery {
  enabled: Record<FilterKey, boolean>; values: Record<FilterKey, string>
  status: number; limit: number; opNos?: readonly string[]
}
export interface LookupOption { key: string; name: string | null; issueState: number | null }
export class SwrReadError extends Error {
  constructor(message: string, readonly status: number | null) { super(message); this.name = 'SwrReadError' }
}
const swrKeys = ['swrNo','swrType','status','customerNo','opNo','deviceNo','startTime','endTime','customerLotNo','moNo','swrDesc','creator','createDate','attachment']
function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error('服務回應格式不符契約。')
  return value as Record<string, unknown>
}
function keys(row: Record<string, unknown>, wanted: string[]): void {
  if (Object.keys(row).length !== wanted.length || wanted.some(key => !Object.hasOwn(row, key))) throw new Error('服務回應缺少欄位或含未知欄位。')
}
function text(value: unknown): string {
  if (typeof value !== 'string') throw new Error('來源文字格式不符契約。')
  for (const c of value) { const n = c.codePointAt(0)!; if (n >= 0xD800 && n <= 0xDFFF) throw new Error('來源含無效Unicode字元。') }
  return value
}
function nullableText(value: unknown): string | null { return value === null ? null : text(value) }
function integer(value: unknown, max: number): number {
  if (typeof value !== 'number' || !Number.isSafeInteger(value) || Math.abs(value) > max) throw new Error('來源數值格式不符契約。')
  return value
}
function wallClock(value: unknown): string | null {
  if (value === null) return null
  const s = text(value), m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:\.\d{1,7})?$/.exec(s)
  if (!m) throw new Error('來源日期必須保留無時區時間。')
  const [y, mon, d, h, min, sec] = m.slice(1).map(Number)
  const leap = y % 4 === 0 && (y % 100 !== 0 || y % 400 === 0)
  const days = [31, leap ? 29 : 28, 31,30,31,30,31,31,30,31,30,31]
  if (y < 1 || mon < 1 || mon > 12 || d < 1 || d > days[mon - 1] || h > 23 || min > 59 || sec > 59) throw new Error('來源日期不合法。')
  return s // Native timestamp-without-time-zone: never Date/UTC/localize/truncate.
}
export function decodeSwr(value: unknown): SwrDto {
  const row = object(value); keys(row, swrKeys)
  for (const k of ['swrNo','customerNo','opNo','swrDesc']) text(row[k])
  for (const k of ['deviceNo','customerLotNo','moNo','creator']) nullableText(row[k])
  integer(row.swrType, 9); integer(row.status, 99)
  wallClock(row.startTime); wallClock(row.endTime); wallClock(row.createDate)
  const a = object(row.attachment); keys(a, ['sourceHaveAttachFile','blobLength','downloadAvailable'])
  if (!['Y','N'].includes(a.sourceHaveAttachFile as string) || a.blobLength !== null && (integer(a.blobLength, Number.MAX_SAFE_INTEGER) < 0)
      || typeof a.downloadAvailable !== 'boolean' || a.downloadAvailable !== (a.blobLength !== null && (a.blobLength as number) > 0))
    throw new Error('附件來源狀態不符契約。')
  return row as unknown as SwrDto
}
export function decodeSwrList(value: unknown): SwrList {
  const e = object(value); keys(e, ['items','limit','hasMore'])
  if (!Array.isArray(e.items) || integer(e.limit, 500) < 1 || e.items.length > (e.limit as number) || typeof e.hasMore !== 'boolean') throw new Error('查詢清單格式不符契約。')
  const items = e.items.map(value => { const r = object(value); keys(r, ['item','lotNo']); return { item: decodeSwr(r.item), lotNo: nullableText(r.lotNo) } })
  return { items, limit: e.limit as number, hasMore: e.hasMore }
}
export function decodeSwrItem(value: unknown, rawKey: string): SwrItem {
  const e = object(value); keys(e, ['item','details']); const item = decodeSwr(e.item)
  if (item.swrNo !== rawKey || !Array.isArray(e.details)) throw new Error('單張回應身份不符原始選取。')
  const details = e.details.map(value => { const r = object(value); keys(r, ['swrNo','lotNo']); if (text(r.swrNo) !== rawKey) throw new Error('明細所屬身份不符。'); return { swrNo: rawKey, lotNo: text(r.lotNo) } })
  return { item, details }
}
function input(value: string, maximum: number, label: string): string {
  text(value)
  if (/^ *$/.test(value) || value.includes('\0') || [...value].length > maximum) throw new SwrReadError(`${label} 已勾選，請輸入長度範圍內的值。`, 400)
  return value // Test blank only; preserve raw case/padding in the request.
}
export function queryParameters(query: SwrQuery): URLSearchParams {
  const fields = ['swrType','customerNo','lotNo','deviceNo','opNo','customerLotNo','moNo']
  keys(object(query.enabled), fields); keys(object(query.values), fields)
  if (fields.some(key => typeof query.enabled[key as FilterKey] !== 'boolean')) throw new SwrReadError('查詢條件開關不合法。', 400)
  if (![0,1,2,99].includes(query.status) || !Number.isInteger(query.limit) || query.limit < 1 || query.limit > 500) throw new SwrReadError('狀態或查詢筆數不合法。', 400)
  const p = new URLSearchParams({ status: String(query.status), limit: String(query.limit) })
  for (const key of Object.keys(query.enabled) as FilterKey[]) {
    p.set(key + 'Enabled', String(query.enabled[key]))
    if (!query.enabled[key]) continue
    const value = query.values[key]
    if (key === 'swrType') { if (!['0','1'].includes(value)) throw new SwrReadError('請選擇SWRType。', 400); p.set('swrType', value) }
    else if (key === 'lotNo' || key === 'opNo') {
      if (key === 'opNo' && query.opNos !== undefined && !Array.isArray(query.opNos)) throw new SwrReadError('OPNo查找選取格式不合法。', 400)
      const values = key === 'opNo' && query.opNos !== undefined ? query.opNos : value.split(/\r?\n/)
      if (values.length < 1 || values.length > 100) throw new SwrReadError('LotNo／OPNo每次最多100筆。', 400)
      for (const v of values) p.append(key === 'lotNo' ? 'lotNos' : 'opNos', input(v, key === 'opNo' ? 20 : 50, key))
    } else p.set(key, input(value, 50, key))
  }
  return p
}
export function createWp109ReadService(apiBaseUrl: string, origin: string, transport: typeof fetch = fetch) {
  if (!apiBaseUrl.trim()) throw new SwrReadError('目前環境尚未設定API位址。', null)
  const base = new URL(apiBaseUrl.replace(/\/+$/, '') + '/', origin)
  if (base.origin !== new URL(origin).origin || !['https:','http:'].includes(base.protocol) || base.username || base.password || base.search || base.hash)
    throw new SwrReadError('唯讀服務必須使用目前網站的API位址。', null)
  const url = (path: string): URL => new URL('api/' + path, base)
  const itemUrl = (suffix: string, rawKey: string): URL => { const u = url('wip/special-work-requests/' + suffix); u.searchParams.set('swrNo', text(rawKey)); return u }
  async function get<T>(u: URL, decoder: (v: unknown) => T, signal: AbortSignal): Promise<T> {
    let response: Response
    try { response = await transport(u.toString(), { method: 'GET', credentials: 'same-origin', redirect: 'error', cache: 'no-store', signal }) }
    catch (e) { if (signal.aborted) throw e; throw new SwrReadError('讀取服務連線未完成，請重新查詢。', null) }
    if (!response.ok) {
      const error = await response.json().catch(() => null) as unknown
      const row = error && typeof error === 'object' ? error as Record<string, unknown> : {}
      const fallback: Record<number,string> = { 400:'查詢條件不合法。',401:'登入已失效，請重新登入。',403:'沒有讀取權限。',404:'資料或附件已不存在。',409:'資料身份不明確，請重新查詢。',503:'服務或來源資料表尚未就緒。' }
      throw new SwrReadError(typeof row.detail === 'string' ? row.detail : typeof row.title === 'string' ? row.title : fallback[response.status] || '讀取未完成。', response.status)
    }
    try { return decoder(await response.json()) } catch { throw new SwrReadError('回應未通過來源格式驗證，請重新查詢。', response.status) }
  }
  return {
    query: (q: SwrQuery, signal: AbortSignal): Promise<SwrList> => { const u = url('wip/special-work-requests'); u.search = queryParameters(q).toString(); return get(u, value => { const r = decodeSwrList(value); if (r.limit !== q.limit) throw new Error('limit不符'); return r }, signal) },
    item: (rawKey: string, signal: AbortSignal): Promise<SwrItem> => get(itemUrl('item', rawKey), value => decodeSwrItem(value, rawKey), signal),
    attachmentUrl: (rawKey: string): string => itemUrl('attachment', rawKey).toString(),
    customers: (keyword: string, signal: AbortSignal): Promise<LookupOption[]> => { const u = url('ent/customer-basis'); u.searchParams.set('keyword', keyword); return get(u, value => decodeCustomerList(value).map(r => ({ key:r.customerNo, name:r.customerName, issueState:r.issueState })), signal) },
    operations: (keyword: string, signal: AbortSignal): Promise<LookupOption[]> => { const u = url('operations'); u.searchParams.set('keyword', keyword); return get(u, value => { if (!Array.isArray(value)) throw new Error('作業站應回原生陣列'); return value.map(v => { const r = object(v); return { key:text(r.operationNo), name:text(r.operationName), issueState:integer(r.issueState, Number.MAX_SAFE_INTEGER) } }) }, signal) },
  }
}
